using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace NatoSquad
{
    /// <summary>One upgrade module owned by the player (see Modules.cs).</summary>
    [Serializable]
    public class ModuleItem
    {
        public int id;
        public int slot;      // ModuleSlot
        public int variant;   // index into Modules.Variants[slot]
        public int rarity;    // Rarity
        public int level = 1;
        public bool equipped;
    }

    /// <summary>Everything that is saved between sessions (JSON in persistentDataPath).</summary>
    [Serializable]
    public class SaveData
    {
        public int version = 1;
        public long gold = 1500;
        public long lifetimeGold;
        public int selectedJet;
        public List<int> ownedJets = new List<int> { 0 };
        public List<int> missionStars = new List<int>();   // best stars per mission (0 = not cleared)
        public List<int> trackLevels = new List<int>();    // permanent upgrade tracks
        public List<ModuleItem> modules = new List<ModuleItem>();
        public int nextModuleId = 1;
        public List<int> specialAmmo = new List<int>();    // ammo owned per special weapon
        public int equippedSpecial = -1;
        public bool doubleGold;                            // IAP non-consumable
        public List<string> processedOrders = new List<string>(); // IAP transaction ids already granted
        public int totalKills;
        public int sorties;
        public int cratesOpened;
        public int cratesSinceLegendary;                   // pity counter
        public string dealDay = "";
        public bool dealBought;
        public bool tutorialSeen;

        public int Stars(int mission) => mission >= 0 && mission < missionStars.Count ? missionStars[mission] : 0;
        public bool Cleared(int mission) => Stars(mission) > 0;
        public bool Unlocked(int mission) => mission == 0 || Cleared(mission - 1);
        public int TotalStars { get { int s = 0; foreach (var v in missionStars) s += v; return s; } }

        public int Track(int t) => t >= 0 && t < trackLevels.Count ? trackLevels[t] : 0;
        public void SetTrack(int t, int lv) { while (trackLevels.Count <= t) trackLevels.Add(0); trackLevels[t] = lv; }

        public int Ammo(int s) => s >= 0 && s < specialAmmo.Count ? specialAmmo[s] : 0;
        public void SetAmmo(int s, int v) { while (specialAmmo.Count <= s) specialAmmo.Add(0); specialAmmo[s] = Mathf.Max(0, v); }

        public void SetStars(int mission, int stars)
        {
            while (missionStars.Count <= mission) missionStars.Add(0);
            if (stars > missionStars[mission]) missionStars[mission] = stars;
        }

        public ModuleItem Equipped(int slot)
        {
            foreach (var m in modules) if (m.equipped && m.slot == slot) return m;
            return null;
        }

        public ModuleItem AddModule(int slot, int variant, int rarity)
        {
            var m = new ModuleItem { id = nextModuleId++, slot = slot, variant = variant, rarity = rarity, level = 1 };
            modules.Add(m);
            // auto-equip into an empty slot so new players see the effect right away
            if (Equipped(slot) == null) m.equipped = true;
            return m;
        }

        public void AddGold(long amount)
        {
            gold = Math.Max(0, gold + amount);
            if (amount > 0) lifetimeGold += amount;
        }
    }

    [Serializable]
    public class GameSettings
    {
        public float soundVolume = 0.8f;
        public float musicVolume = 0.55f;
        public bool vibration = true;
        public float sensitivity = 1.25f;   // touch drag → jet movement
        public bool showDamageNumbers = true;
        public bool screenShake = true;
    }

    public static class SaveSystem
    {
        const string FileName = "natosquad_save.json";
        const string SettingsKey = "NatoSquad.Settings";
        static string SavePath => Path.Combine(Application.persistentDataPath, FileName);

        public static SaveData Load()
        {
            try
            {
                if (File.Exists(SavePath))
                {
                    var d = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
                    if (d != null) return Fix(d);
                }
            }
            catch (Exception e) { Debug.LogWarning("[NATO Squad] Couldn't read the save: " + e.Message); }
            return Fix(new SaveData());
        }

        static SaveData Fix(SaveData d)
        {
            if (d.ownedJets == null || d.ownedJets.Count == 0) d.ownedJets = new List<int> { 0 };
            if (d.missionStars == null) d.missionStars = new List<int>();
            if (d.trackLevels == null) d.trackLevels = new List<int>();
            if (d.modules == null) d.modules = new List<ModuleItem>();
            if (d.specialAmmo == null) d.specialAmmo = new List<int>();
            if (d.processedOrders == null) d.processedOrders = new List<string>();
            if (!d.ownedJets.Contains(d.selectedJet)) d.selectedJet = d.ownedJets[0];
            // starter kit: a few cluster bombs so the special button does something on the first sortie
            if (d.specialAmmo.Count == 0) { d.SetAmmo(0, 6); d.equippedSpecial = 0; }
            return d;
        }

        /// <summary>Writes the save atomically. Returns false if it could not be written.</summary>
        public static bool Save(SaveData d)
        {
            try
            {
                string tmp = SavePath + ".tmp";
                File.WriteAllText(tmp, JsonUtility.ToJson(d));
                if (File.Exists(SavePath)) File.Delete(SavePath);
                File.Move(tmp, SavePath);
                return true;
            }
            catch (Exception e) { Debug.LogError("[NATO Squad] Couldn't write the save: " + e.Message); return false; }
        }

        public static void Delete()
        {
            try { if (File.Exists(SavePath)) File.Delete(SavePath); } catch { }
        }

        public static GameSettings LoadSettings()
        {
            string json = PlayerPrefs.GetString(SettingsKey, "");
            if (!string.IsNullOrEmpty(json))
            {
                try { var s = JsonUtility.FromJson<GameSettings>(json); if (s != null) return s; } catch { }
            }
            return new GameSettings();
        }

        public static void SaveSettings(GameSettings s)
        {
            PlayerPrefs.SetString(SettingsKey, JsonUtility.ToJson(s));
            PlayerPrefs.Save();
        }
    }
}
