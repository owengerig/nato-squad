using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;

namespace NatoSquad
{
    /// <summary>Product IDs. Create products with exactly these IDs in App Store Connect and Google Play Console.</summary>
    public static class IAPIds
    {
        public const string GoldSmall = "com.ogsoftware.natosquad.gold_small";
        public const string GoldMedium = "com.ogsoftware.natosquad.gold_medium";
        public const string GoldLarge = "com.ogsoftware.natosquad.gold_large";
        public const string GoldHuge = "com.ogsoftware.natosquad.gold_huge";
        public const string CommandCrate = "com.ogsoftware.natosquad.command_crate";
        public const string StarterPack = "com.ogsoftware.natosquad.starter_pack";
        public const string DoubleGold = "com.ogsoftware.natosquad.double_gold";
    }

    public struct IAPProduct
    {
        public string id, title, desc, fallbackPrice;
        public ProductType type;
        public long gold;
        public IAPProduct(string id, ProductType type, string title, string desc, long gold, string fallbackPrice)
        { this.id = id; this.type = type; this.title = title; this.desc = desc; this.gold = gold; this.fallbackPrice = fallbackPrice; }
    }

    /// <summary>
    /// Real-money store using Unity IAP 5 (StoreController). Flow: subscribe to events → Connect → FetchProducts →
    /// PurchaseProduct → OnPurchasePending (grant + save) → ConfirmPurchase. In the editor Unity IAP uses its fake store.
    /// </summary>
    public class IAPManager : MonoBehaviour
    {
        public static readonly IAPProduct[] Catalog =
        {
            new IAPProduct(IAPIds.GoldSmall, ProductType.Consumable, "Pouch of Gold", "5,000 gold", 5000, "$0.99"),
            new IAPProduct(IAPIds.GoldMedium, ProductType.Consumable, "Crate of Gold", "30,000 gold", 30000, "$4.99"),
            new IAPProduct(IAPIds.GoldLarge, ProductType.Consumable, "Pallet of Gold", "70,000 gold", 70000, "$9.99"),
            new IAPProduct(IAPIds.GoldHuge, ProductType.Consumable, "War Chest", "160,000 gold", 160000, "$19.99"),
            new IAPProduct(IAPIds.CommandCrate, ProductType.Consumable, "Command Crate", "3 modules, Rare or better", 0, "$2.99"),
            new IAPProduct(IAPIds.StarterPack, ProductType.Consumable, "Starter Pack", "20,000 gold + 1 Epic module + 20 Seeker Missiles", 20000, "$2.99"),
            new IAPProduct(IAPIds.DoubleGold, ProductType.NonConsumable, "Double Gold Pay", "Permanently double all gold earned in missions", 0, "$4.99"),
        };

        public bool Connected { get; private set; }
        public bool ProductsReady { get; private set; }
        public string Status { get; private set; } = "Connecting to store…";
        public event Action Changed;
        public event Action<string, List<ModuleItem>> Granted; // product id, modules granted (for crate reveal)

        GameManager game;
        StoreController store;
        readonly HashSet<string> purchasing = new HashSet<string>();

        public void Init(GameManager g)
        {
            game = g;
            Connect();
        }

        async void Connect()
        {
            try
            {
                store = UnityIAPServices.StoreController();

                // all events before Connect(): pending purchases from an earlier session can arrive right away
                store.OnPurchasePending += OnPurchasePending;
                store.OnPurchaseConfirmed += OnPurchaseConfirmed;
                store.OnPurchaseFailed += OnPurchaseFailed;
                store.OnPurchaseDeferred += OnPurchaseDeferred;
                store.OnStoreConnected += OnStoreConnected;
                store.OnStoreDisconnected += OnStoreDisconnected;
                store.OnProductsFetched += OnProductsFetched;
                store.OnProductsFetchFailed += OnProductsFetchFailed;
                store.OnPurchasesFetched += OnPurchasesFetched;
                store.OnPurchasesFetchFailed += OnPurchasesFetchFailed;

                await store.Connect();
            }
            catch (Exception e)
            {
                Status = "Store unavailable";
                Debug.LogWarning("[NATO Squad IAP] " + e.Message);
                Changed?.Invoke();
            }
        }

        void OnStoreConnected()
        {
            Connected = true;
            Status = "Loading products…";
            var defs = Catalog.Select(p => new ProductDefinition(p.id, p.type)).ToList();
            store.FetchProducts(defs);
            Changed?.Invoke();
        }

        void OnStoreDisconnected(StoreConnectionFailureDescription f)
        {
            Connected = false;
            Status = "Store unavailable";
            Debug.LogWarning("[NATO Squad IAP] disconnected: " + f.Message);
            Changed?.Invoke();
        }

        void OnProductsFetched(List<Product> products)
        {
            ProductsReady = true;
            Status = "";
            store.FetchPurchases(); // restores Double Gold Pay on a new install / device
            Changed?.Invoke();
        }

        void OnProductsFetchFailed(ProductFetchFailed f)
        {
            Status = "Couldn't load products";
            Debug.LogWarning("[NATO Squad IAP] product fetch failed: " + f.FailureReason);
            Changed?.Invoke();
        }

        void OnPurchasesFetched(Orders orders)
        {
            foreach (var o in orders.ConfirmedOrders)
                foreach (var item in o.CartOrdered.Items())
                    if (item.Product != null && item.Product.definition.id == IAPIds.DoubleGold && !game.Save.doubleGold)
                    {
                        game.Save.doubleGold = true;
                        game.SaveGame();
                    }
            Changed?.Invoke();
        }

        void OnPurchasesFetchFailed(PurchasesFetchFailureDescription f)
        {
            Debug.LogWarning("[NATO Squad IAP] purchases fetch failed: " + f.Message);
        }

        // ------------------------------------------------------------------ public API
        public string Price(string id)
        {
            var def = Catalog.First(p => p.id == id);
            if (store == null || !ProductsReady) return def.fallbackPrice;
            var p = store.GetProductById(id);
            if (p == null || p.metadata == null || string.IsNullOrEmpty(p.metadata.localizedPriceString)) return def.fallbackPrice;
            return p.metadata.localizedPriceString;
        }

        public bool Available(string id)
        {
            if (store == null || !ProductsReady) return false;
            var p = store.GetProductById(id);
            return p != null && p.availableToPurchase;
        }

        public bool Busy(string id) => purchasing.Contains(id);

        public void Buy(string id)
        {
            if (!Available(id)) { game.Toast(Connected ? "This item isn't available right now" : "Store unavailable. Check your connection.", Pal.Red); return; }
            if (id == IAPIds.DoubleGold && game.Save.doubleGold) { game.Toast("You already own Double Gold Pay"); return; }
            purchasing.Add(id);
            Changed?.Invoke();
            store.PurchaseProduct(id);
        }

        public void Restore()
        {
            if (store == null || !Connected) { game.Toast("Store unavailable", Pal.Red); return; }
            store.RestoreTransactions((ok, err) =>
            {
                game.Toast(ok ? "Purchases restored" : "Restore failed" + (string.IsNullOrEmpty(err) ? "" : ": " + err), ok ? Pal.Green : Pal.Red);
                store.FetchPurchases();
            });
        }

        // ------------------------------------------------------------------ purchase flow
        void OnPurchasePending(PendingOrder order)
        {
            var product = order.CartOrdered.Items().FirstOrDefault()?.Product;
            string id = product != null ? product.definition.id : "";
            string tx = order.Info != null ? order.Info.TransactionID : null;
            purchasing.Remove(id);

            bool already = !string.IsNullOrEmpty(tx) && game.Save.processedOrders.Contains(tx);
            List<ModuleItem> modules = null;
            if (!already)
            {
                modules = Grant(id);
                if (!string.IsNullOrEmpty(tx))
                {
                    game.Save.processedOrders.Add(tx);
                    if (game.Save.processedOrders.Count > 200) game.Save.processedOrders.RemoveAt(0);
                }
            }
            // save before confirming: an unconfirmed purchase is re-delivered, a confirmed but unsaved one is lost
            if (!game.SaveGame())
            {
                game.Toast("Couldn't save. The purchase will be delivered next launch.", Pal.Red);
                Changed?.Invoke();
                return;
            }
            store.ConfirmPurchase(order);
            if (!already)
            {
                game.Audio.Play(Sfx.Buy);
                Granted?.Invoke(id, modules);
            }
            Changed?.Invoke();
        }

        List<ModuleItem> Grant(string id)
        {
            var s = game.Save;
            var def = Catalog.FirstOrDefault(p => p.id == id);
            switch (id)
            {
                case IAPIds.GoldSmall:
                case IAPIds.GoldMedium:
                case IAPIds.GoldLarge:
                case IAPIds.GoldHuge:
                    s.AddGold(def.gold);
                    game.Toast("+" + UI.Gold(def.gold) + " gold", Pal.Gold);
                    return null;
                case IAPIds.CommandCrate:
                    return Crates.Open(Crates.All[2], s);
                case IAPIds.StarterPack:
                {
                    s.AddGold(def.gold);
                    int seeker = 1;
                    s.SetAmmo(seeker, s.Ammo(seeker) + 20);
                    var m = Modules.Random(s, (int)Rarity.Epic);
                    return new List<ModuleItem> { m };
                }
                case IAPIds.DoubleGold:
                    s.doubleGold = true;
                    game.Toast("Double Gold Pay unlocked!", Pal.Gold);
                    return null;
            }
            Debug.LogWarning("[NATO Squad IAP] unknown product " + id);
            return null;
        }

        void OnPurchaseConfirmed(Order order)
        {
            if (order is FailedOrder failed)
                Debug.LogWarning("[NATO Squad IAP] confirm failed: " + failed.FailureReason + " " + failed.Details);
        }

        void OnPurchaseFailed(FailedOrder failed)
        {
            var product = failed.CartOrdered.Items().FirstOrDefault()?.Product;
            if (product != null) purchasing.Remove(product.definition.id);
            else purchasing.Clear();
            if (failed.FailureReason != PurchaseFailureReason.UserCancelled)
                game.Toast("Purchase failed", Pal.Red);
            Debug.Log("[NATO Squad IAP] purchase failed: " + failed.FailureReason + " " + failed.Details);
            Changed?.Invoke();
        }

        void OnPurchaseDeferred(DeferredOrder deferred)
        {
            purchasing.Clear();
            game.Toast("Purchase is waiting for approval");
            Changed?.Invoke();
        }
    }
}
