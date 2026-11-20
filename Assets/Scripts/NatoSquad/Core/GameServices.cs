using OGSoftware.Platform;
using UnityEngine;

namespace OGSoftware.Platform
{
    /// <summary>This game's Play Games App ID, read by OGPlatformBuild when the Android project is generated.</summary>
    public static class OGPlatformProject
    {
        /// <summary>Play Console → Play Games Services → Configuration → "Project ID" (a long number).</summary>
        public const string AndroidAppId = "TODO_PLAY_GAMES_APP_ID";
    }
}

namespace NatoSquad
{
    /// <summary>
    /// NATO Squad on Game Center / Google Play Games: achievements, leaderboards, and the save (gold, jets, upgrades,
    /// modules, mission stars) kept in the cloud so it comes back on a new device. Shared code: Assets/OGPlatform (same
    /// in every OG Software game). Store setup, with names and descriptions: GAME_SERVICES.md.
    /// </summary>
    public static class GameServices
    {
        const string Ios = "com.ogsoftware.natosquad.";

        public const string FirstClear = "first_clear", Ops4 = "ops_4", OpsAll = "ops_all", Perfect = "perfect",
            AllStars = "all_stars", Kills500 = "kills_500", Kills5000 = "kills_5000", Sorties50 = "sorties_50",
            FullSquad = "full_squad", Legendary = "legendary", Mythic = "mythic", Crates25 = "crates_25",
            Millionaire = "millionaire";

        public const string LbKills = "total_kills", LbGold = "lifetime_gold";

        /// <summary>Play Console IDs (they look like CgkI...EAIQAQ). Leave TODO until they exist; nothing is sent for those.</summary>
        static string Android(string key)
        {
            switch (key)
            {
                case FirstClear: return "TODO";
                case Ops4: return "TODO";
                case OpsAll: return "TODO";
                case Perfect: return "TODO";
                case AllStars: return "TODO";
                case Kills500: return "TODO";
                case Kills5000: return "TODO";
                case Sorties50: return "TODO";
                case FullSquad: return "TODO";
                case Legendary: return "TODO";
                case Mythic: return "TODO";
                case Crates25: return "TODO";
                case Millionaire: return "TODO";
                case LbKills: return "TODO";
                case LbGold: return "TODO";
            }
            return "TODO";
        }

        static void A(string key, int steps = 1) => OGPlatform.DefineAchievement(key, Ios + key, Android(key), steps);
        static void L(string key) => OGPlatform.DefineLeaderboard(key, Ios + key, Android(key));

        static SaveData pendingRestore;

        public static void Init()
        {
            A(FirstClear); A(Ops4, 4); A(OpsAll, Missions.Count); A(Perfect); A(AllStars, Missions.Count * 3);
            A(Kills500, 500); A(Kills5000, 5000); A(Sorties50, 50); A(FullSquad); A(Legendary); A(Mythic);
            A(Crates25, 25); A(Millionaire);
            L(LbKills); L(LbGold);

            OGPlatform.SignInChanged += () => { if (OGPlatform.IsSignedIn) Evaluate(); };
            OGCloudSave.Register(new CloudData());
            OGPlatform.Init();
            Evaluate();
        }

        /// <summary>Called after every save.</summary>
        public static void OnSaved()
        {
            if (GameManager.I == null || GameManager.I.Save == null) return;
            Evaluate();
            OGCloudSave.MarkDirty();
        }

        /// <summary>Works out every achievement and leaderboard from the save (safe to repeat).</summary>
        static void Evaluate()
        {
            var save = GameManager.I != null ? GameManager.I.Save : null;
            if (save == null) return;
            int cleared = 0, perfect = 0;
            for (int i = 0; i < Missions.Count; i++)
            {
                if (save.Cleared(i)) cleared++;
                if (save.Stars(i) >= 3) perfect++;
            }
            if (cleared > 0) OGPlatform.Unlock(FirstClear);
            OGPlatform.SetProgress(Ops4, cleared);
            OGPlatform.SetProgress(OpsAll, cleared);
            if (perfect > 0) OGPlatform.Unlock(Perfect);
            OGPlatform.SetProgress(AllStars, Mathf.Min(save.TotalStars, Missions.Count * 3));
            OGPlatform.SetProgress(Kills500, save.totalKills);
            OGPlatform.SetProgress(Kills5000, save.totalKills);
            OGPlatform.SetProgress(Sorties50, save.sorties);
            if (save.ownedJets.Count >= Jets.All.Length) OGPlatform.Unlock(FullSquad);
            foreach (var m in save.modules)
            {
                if (m.rarity >= (int)Rarity.Legendary) OGPlatform.Unlock(Legendary);
                if (m.rarity >= (int)Rarity.Mythic) OGPlatform.Unlock(Mythic);
            }
            OGPlatform.SetProgress(Crates25, save.cratesOpened);
            if (save.lifetimeGold >= 1000000) OGPlatform.Unlock(Millionaire);

            if (save.totalKills > 0) OGPlatform.SubmitScore(LbKills, save.totalKills);
            if (save.lifetimeGold > 0) OGPlatform.SubmitScore(LbGold, save.lifetimeGold);
        }

        /// <summary>A cloud save that arrived during a sortie waits until the main menu.</summary>
        public static void ApplyPendingRestore()
        {
            if (pendingRestore == null || GameManager.I == null) return;
            var data = pendingRestore;
            pendingRestore = null;
            // a purchase made on this device stays (Double Gold Pay and already-granted orders)
            var local = GameManager.I.Save;
            if (local != null)
            {
                data.doubleGold |= local.doubleGold;
                foreach (var o in local.processedOrders) if (!data.processedOrders.Contains(o)) data.processedOrders.Add(o);
            }
            GameManager.I.Save = data;
            SaveSystem.Write(data);
            Evaluate();
        }

        class CloudData : IOGCloudData
        {
            public string Export() => JsonUtility.ToJson(GameManager.I.Save);

            public long Progress(string exported)
            {
                var d = JsonUtility.FromJson<SaveData>(exported);
                return d != null ? d.revision : 0;
            }

            public bool LocalIsEmpty
            {
                get
                {
                    var s = GameManager.I != null ? GameManager.I.Save : null;
                    return s == null || (s.revision == 0 && s.sorties == 0);
                }
            }

            public void Import(string exported)
            {
                var data = SaveSystem.FromJson(exported);
                if (data == null) throw new System.Exception("not a NATO Squad save");
                pendingRestore = data;
                var gm = GameManager.I;
                if (gm.Current is MainMenuScreen)
                {
                    ApplyPendingRestore();
                    gm.Show<MainMenuScreen>();
                    gm.Toast("Progress restored from " + OGPlatform.ServiceName);
                }
            }
        }
    }
}
