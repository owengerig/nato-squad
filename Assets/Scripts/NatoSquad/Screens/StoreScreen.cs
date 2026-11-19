using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NatoSquad
{
    /// <summary>
    /// Store: gold packs and bundles for real money (Unity IAP), supply crates for gold (with the drop odds shown),
    /// the daily deal, and Double Gold Pay. Restore Purchases is at the bottom (required by Apple).
    /// </summary>
    public class StoreScreen : MenuScreen
    {
        RectTransform listRoot, list;
        Text status;

        public override void Build()
        {
            G.Audio.PlayMusic(GameAudio.Track.Menu);
            TopBar("STORE", () => G.Show<MainMenuScreen>());
            listRoot = Body(24);
            G.Store.Changed += OnStoreChanged;
            G.Store.Granted += OnGranted;
            Rebuild();
        }

        void OnDestroy()
        {
            if (G != null && G.Store != null) { G.Store.Changed -= OnStoreChanged; G.Store.Granted -= OnGranted; }
        }

        void OnStoreChanged() { if (this != null) Rebuild(); }

        void OnGranted(string id, List<ModuleItem> modules)
        {
            if (modules != null && modules.Count > 0) Reveal(modules, id == IAPIds.CommandCrate ? "COMMAND CRATE" : "STARTER PACK");
        }

        public override void OnBack() => G.Show<MainMenuScreen>();

        void Rebuild()
        {
            Vector2 pos = list != null ? list.anchoredPosition : Vector2.zero;
            for (int i = listRoot.childCount - 1; i >= 0; i--) Destroy(listRoot.GetChild(i).gameObject);
            list = UI.Scroll(listRoot, "List", 16, 16);
            UI.Stretch((RectTransform)list.parent);

            var store = G.Store;
            var save = G.Save;

            // ---------------- gold packs (real money)
            Header("GOLD", store.Status);
            var gold = Section(300);
            string[] goldIds = { IAPIds.GoldSmall, IAPIds.GoldMedium, IAPIds.GoldLarge, IAPIds.GoldHuge };
            string[] tags = { "", "POPULAR", "+17% VALUE", "BEST VALUE" };
            for (int i = 0; i < goldIds.Length; i++)
            {
                var p = Find(goldIds[i]);
                Tile(gold, i, 4, p.title, UI.Gold(p.gold) + " gold", tags[i], Art.Coin(), store.Price(p.id), new Color(0.75f, 0.52f, 0.12f),
                     store.Busy(p.id) ? "…" : null, () => store.Buy(p.id));
            }

            // ---------------- crates
            Header("SUPPLY CRATES", "Each crate holds random upgrade modules. Odds are listed on every crate.");
            var crates = Section(360);
            for (int i = 0; i < Crates.All.Length; i++)
            {
                var c = Crates.All[i];
                int idx = i;
                string price; Action act; Color bc;
                if (c.goldCost > 0)
                {
                    price = UI.Gold(c.goldCost) + " gold";
                    bc = save.gold >= c.goldCost ? Pal.Accent : Pal.Disabled;
                    act = () => BuyCrateWithGold(idx);
                }
                else
                {
                    price = store.Price(c.iapId);
                    bc = new Color(0.75f, 0.52f, 0.12f);
                    act = () => store.Buy(c.iapId);
                }
                string sub = (c.count > 1 ? c.count + " modules\n" : "1 module\n") + "<size=20>" + Crates.OddsText(c) + "</size>";
                Tile(crates, i, 3, c.name, sub, i == 2 ? "PREMIUM" : "", Art.SlotIcon(i == 0 ? 2 : i == 1 ? 0 : 5), price, bc, null, act);
            }
            var pity = UI.Row(list, 44, new Color(0, 0, 0, 0));
            var pt = UI.Label(pity, "A Legendary module is guaranteed at least once every " + Crates.PityLimit + " crate modules (" + save.cratesSinceLegendary + "/" + Crates.PityLimit + ").", 24, Pal.TextDim);
            UI.Stretch(pt.rectTransform);

            // ---------------- daily deal + bundles
            Header("DEALS", "The daily deal changes every day at 00:00 UTC.");
            var deals = Section(330);
            DailyDeal.Describe(out int dSlot, out int dVar, out int dRar, out int dPrice);
            bool dealBought = DailyDeal.Bought(save);
            var preview = new ModuleItem { slot = dSlot, variant = dVar, rarity = dRar, level = 1 };
            Tile(deals, 0, 3, "Daily Deal: " + RarityInfo.Name(dRar), Modules.Name(preview) + "\n<size=20>" + Modules.Describe(preview).Replace("\n", "   ") + "</size>",
                 dealBought ? "SOLD OUT" : "TODAY", Art.SlotIcon(dSlot), dealBought ? "Come back tomorrow" : UI.Gold(dPrice) + " gold",
                 dealBought ? Pal.Disabled : Pal.Accent, null, () =>
                 {
                     if (DailyDeal.Bought(save)) return;
                     if (!G.Spend(dPrice)) return;
                     var m = save.AddModule(dSlot, dVar, dRar);
                     save.dealDay = DailyDeal.Today; save.dealBought = true;
                     G.SaveGame();
                     Rebuild();
                     Reveal(new List<ModuleItem> { m }, "DAILY DEAL");
                 }, RarityInfo.Col(dRar));
            var starter = Find(IAPIds.StarterPack);
            Tile(deals, 1, 3, starter.title, starter.desc, "NEW PILOTS", Art.Jet(0), store.Price(starter.id), new Color(0.75f, 0.52f, 0.12f),
                 store.Busy(starter.id) ? "…" : null, () => store.Buy(starter.id));
            var dbl = Find(IAPIds.DoubleGold);
            Tile(deals, 2, 3, dbl.title, dbl.desc, save.doubleGold ? "OWNED" : "PERMANENT", Art.Coin(),
                 save.doubleGold ? "Owned" : store.Price(dbl.id), save.doubleGold ? Pal.Disabled : new Color(0.75f, 0.52f, 0.12f),
                 store.Busy(dbl.id) ? "…" : null, () => { if (!save.doubleGold) store.Buy(dbl.id); });

            // ---------------- restore + legal
            var foot = UI.Row(list, 120, new Color(0, 0, 0, 0));
            var restore = UI.Button(foot, "RESTORE PURCHASES", 30, Pal.PanelLight, () => store.Restore());
            UI.Place((RectTransform)restore.transform, new Vector2(0, 0.5f), new Vector2(420, 90), new Vector2(10, 0), new Vector2(0, 0.5f));
            var privacy = UI.Button(foot, "PRIVACY POLICY", 30, Pal.PanelLight, () => Application.OpenURL(Links.PrivacyPolicy));
            UI.Place((RectTransform)privacy.transform, new Vector2(0, 0.5f), new Vector2(360, 90), new Vector2(450, 0), new Vector2(0, 0.5f));
            var legal = UI.Label(foot, "Purchases are charged to your App Store or Google Play account. Gold and modules have no cash value.", 22, Pal.TextDim, TextAnchor.MiddleLeft);
            UI.Stretch(legal.rectTransform, 840, 0, 10, 0);

            list.anchoredPosition = pos;
        }

        static IAPProduct Find(string id)
        {
            foreach (var p in IAPManager.Catalog) if (p.id == id) return p;
            return default;
        }

        void Header(string title, string sub)
        {
            var row = UI.Row(list, 70, new Color(0, 0, 0, 0));
            var t = UI.Label(row, title, 44, Pal.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            UI.Stretch(t.rectTransform, 10, 0, 0, 0);
            UI.AddShadow(t);
            if (!string.IsNullOrEmpty(sub))
            {
                var s = UI.Label(row, sub, 24, Pal.TextDim, TextAnchor.MiddleRight);
                UI.Stretch(s.rectTransform, 500, 0, 10, 0);
            }
        }

        RectTransform Section(float height) => UI.Row(list, height, new Color(0, 0, 0, 0));

        void Tile(RectTransform section, int i, int of, string title, string sub, string tag, Sprite icon, string price, Color buttonColor, string busy, Action onBuy, Color? accent = null)
        {
            var card = UI.Box(section, "Tile", Pal.Panel);
            card.rectTransform.anchorMin = new Vector2(i / (float)of, 0); card.rectTransform.anchorMax = new Vector2((i + 1) / (float)of, 1);
            card.rectTransform.offsetMin = new Vector2(8, 0); card.rectTransform.offsetMax = new Vector2(-8, 0);
            if (accent.HasValue)
            {
                var edge = UI.Img(card.transform, "Accent", accent.Value);
                edge.rectTransform.anchorMin = new Vector2(0, 1); edge.rectTransform.anchorMax = new Vector2(1, 1);
                edge.rectTransform.sizeDelta = new Vector2(-30, 8); edge.rectTransform.anchoredPosition = new Vector2(0, -6);
            }
            var img = UI.Img(card.transform, "Icon", accent ?? Color.white, icon);
            img.preserveAspect = true;
            UI.Place(img.rectTransform, new Vector2(0, 1), new Vector2(96, 96), new Vector2(24, -26));
            var t = UI.Label(card.transform, title, 34, Pal.Text, TextAnchor.UpperLeft, FontStyle.Bold);
            UI.Stretch(t.rectTransform, 136, 0, 16, 26);
            var s = UI.Label(card.transform, sub, 26, Pal.TextDim, TextAnchor.UpperLeft);
            UI.Stretch(s.rectTransform, 136, 120, 16, 76);
            if (!string.IsNullOrEmpty(tag))
            {
                var tg = UI.Box(card.transform, "Tag", Pal.Red);
                UI.Place(tg.rectTransform, new Vector2(1, 1), new Vector2(210, 40), new Vector2(-12, -12));
                var tl = UI.Label(tg.transform, tag, 22, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                UI.Stretch(tl.rectTransform);
                // keep the title clear of the tag
                t.rectTransform.offsetMax = new Vector2(-230, -26);
            }
            var b = UI.Button(card.transform, busy ?? price, 34, buttonColor, onBuy);
            var brt = (RectTransform)b.transform;
            brt.anchorMin = new Vector2(0, 0); brt.anchorMax = new Vector2(1, 0); brt.pivot = new Vector2(0.5f, 0);
            brt.offsetMin = new Vector2(24, 20); brt.offsetMax = new Vector2(-24, 110);
        }

        void BuyCrateWithGold(int idx)
        {
            var c = Crates.All[idx];
            if (!G.Spend(c.goldCost)) return;
            var got = Crates.Open(c, G.Save);
            G.SaveGame();
            G.Audio.Play(Sfx.Crate);
            Rebuild();
            Reveal(got, c.name.ToUpper());
        }

        /// <summary>Shows the modules just received, best first, with their rarity colors.</summary>
        public static void Reveal(List<ModuleItem> modules, string title)
        {
            var gm = GameManager.I;
            var dim = UI.Img(gm.Overlay, "Reveal", new Color(0, 0, 0, 0.8f));
            dim.raycastTarget = true;
            UI.Stretch(dim.rectTransform, -200, -200, -200, -200);
            var panel = UI.Box(dim.transform, "Panel", Pal.Panel);
            float w = Mathf.Max(700, modules.Count * 420 + 80);
            UI.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(w, 700), Vector2.zero);
            var t = UI.Label(panel.transform, title, 60, Pal.Gold, TextAnchor.UpperCenter, FontStyle.Bold);
            UI.Stretch(t.rectTransform, 0, 0, 0, 30);
            modules.Sort((a, b) => b.rarity.CompareTo(a.rarity));
            for (int i = 0; i < modules.Count; i++)
            {
                var m = modules[i];
                var rc = RarityInfo.Col(m.rarity);
                var card = UI.Box(panel.transform, "Module", new Color(rc.r * 0.25f, rc.g * 0.25f, rc.b * 0.25f, 1f));
                UI.Place(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(390, 440), new Vector2((i - (modules.Count - 1) * 0.5f) * 420, 0));
                var glow = UI.Img(card.transform, "Glow", Col.A(rc, 0.6f), Art.Glow());
                UI.Place(glow.rectTransform, new Vector2(0.5f, 1), new Vector2(260, 260), new Vector2(0, -20), new Vector2(0.5f, 1));
                var icon = UI.Img(card.transform, "Icon", rc, Art.SlotIcon(m.slot));
                UI.Place(icon.rectTransform, new Vector2(0.5f, 1), new Vector2(140, 140), new Vector2(0, -80), new Vector2(0.5f, 1));
                var r = UI.Label(card.transform, RarityInfo.Name(m.rarity).ToUpper(), 32, rc, TextAnchor.UpperCenter, FontStyle.Bold);
                UI.Stretch(r.rectTransform, 10, 0, 10, 250);
                var n = UI.Label(card.transform, Modules.Name(m) + "\n<size=24><color=#9fb0c8>" + Modules.SlotNames[m.slot] + " module</color></size>", 30, Pal.Text, TextAnchor.UpperCenter, FontStyle.Bold);
                UI.Stretch(n.rectTransform, 10, 0, 10, 292);
                var s = UI.Label(card.transform, Modules.Describe(m), 24, Pal.Text, TextAnchor.LowerCenter);
                UI.Stretch(s.rectTransform, 10, 20, 10, 0);
            }
            var ok = UI.Button(panel.transform, "COLLECT", 40, Pal.Accent, () => UnityEngine.Object.Destroy(dim.gameObject));
            UI.Place((RectTransform)ok.transform, new Vector2(0.5f, 0), new Vector2(360, 100), new Vector2(0, 30), new Vector2(0.5f, 0));
            gm.Audio.Play(Sfx.Crate);
        }
    }
}
