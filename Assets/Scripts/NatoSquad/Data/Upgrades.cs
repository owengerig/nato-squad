using System;
using System.Collections.Generic;
using UnityEngine;

namespace NatoSquad
{
    // =====================================================================================================
    //  Stats
    // =====================================================================================================
    public enum Stat
    {
        Damage, FireRate, MaxHull, Regen, Speed, GoldBonus, Crit, CritDamage, SpecialDamage, SpecialCapacity,
        Magnet, DamageReduction, PowerStart, DroneDamage, BossDamage
    }

    public static class StatInfo
    {
        public static readonly string[] Names =
        {
            "Gun Damage", "Fire Rate", "Max Hull", "Hull Repair", "Speed", "Gold Bonus", "Crit Chance", "Crit Damage",
            "Special Damage", "Special Capacity", "Pickup Range", "Damage Reduction", "Starting Gun Power", "Drone Damage", "Boss Damage"
        };
        // flat stats are shown as "+N", the rest as "+N%"
        public static bool IsFlat(Stat s) => s == Stat.SpecialCapacity || s == Stat.PowerStart || s == Stat.DroneDamage;

        public static string Format(Stat s, float v)
        {
            if (IsFlat(s)) return "+" + (s == Stat.DroneDamage ? v.ToString("0.#") : Mathf.FloorToInt(v + 0.0001f).ToString()) + " " + Names[(int)s];
            return "+" + v.ToString("0.#") + "% " + Names[(int)s];
        }
    }

    /// <summary>Summed bonuses from upgrade tracks and modules. Percent stats are stored as percent (e.g. 12 = +12%).</summary>
    public class StatBlock
    {
        public readonly float[] v = new float[Enum.GetValues(typeof(Stat)).Length];
        public float this[Stat s] { get => v[(int)s]; set => v[(int)s] = value; }
        public void Add(Stat s, float amount) { v[(int)s] += amount; }
        public float Mult(Stat s) => 1f + v[(int)s] / 100f;
    }

    // =====================================================================================================
    //  Rarity
    // =====================================================================================================
    public enum Rarity { Common, Uncommon, Rare, Epic, Legendary, Mythic }

    public static class RarityInfo
    {
        public static readonly string[] Names = { "Common", "Uncommon", "Rare", "Epic", "Legendary", "Mythic" };
        public static readonly Color[] Colors =
        {
            new Color(0.70f, 0.73f, 0.77f), new Color(0.35f, 0.85f, 0.40f), new Color(0.30f, 0.62f, 1.00f),
            new Color(0.72f, 0.42f, 1.00f), new Color(1.00f, 0.62f, 0.15f), new Color(1.00f, 0.25f, 0.30f)
        };
        public static readonly float[] Power = { 1f, 1.6f, 2.5f, 4f, 6.5f, 10f };
        public static readonly int[] MaxLevel = { 5, 10, 15, 20, 25, 30 };
        public static readonly int[] LevelCostBase = { 150, 300, 600, 1200, 2400, 4800 };
        public static readonly int[] SalvageValue = { 100, 250, 700, 2000, 6000, 18000 };
        public const int Count = 6;
        public static Color Col(int r) => Colors[Mathf.Clamp(r, 0, Count - 1)];
        public static string Name(int r) => Names[Mathf.Clamp(r, 0, Count - 1)];
    }

    // =====================================================================================================
    //  Modules (rarity-tier gear in six slots)
    // =====================================================================================================
    public enum ModuleSlot { Gun, Engine, Armor, Avionics, Warhead, Drone }

    public struct ModuleVariant
    {
        public string name;
        public Stat primary, secondary;
        public ModuleVariant(string n, Stat p, Stat s) { name = n; primary = p; secondary = s; }
    }

    public static class Modules
    {
        public static readonly string[] SlotNames = { "Gun", "Engine", "Armor", "Avionics", "Warhead", "Drone" };
        public const int SlotCount = 6;

        public static readonly ModuleVariant[][] Variants =
        {
            new[] { // Gun
                new ModuleVariant("Rotary Barrel", Stat.FireRate, Stat.Damage),
                new ModuleVariant("Tungsten Core Rounds", Stat.Damage, Stat.CritDamage),
                new ModuleVariant("Smart Fuze Shells", Stat.Crit, Stat.Damage),
                new ModuleVariant("Twin Feed Chute", Stat.FireRate, Stat.Crit),
            },
            new[] { // Engine
                new ModuleVariant("Afterburner Kit", Stat.Speed, Stat.FireRate),
                new ModuleVariant("Vector Nozzles", Stat.Speed, Stat.DamageReduction),
                new ModuleVariant("Scramjet Intake", Stat.Speed, Stat.GoldBonus),
            },
            new[] { // Armor
                new ModuleVariant("Ceramic Plating", Stat.MaxHull, Stat.DamageReduction),
                new ModuleVariant("Nano-Weave Skin", Stat.Regen, Stat.MaxHull),
                new ModuleVariant("Reactive Armor", Stat.DamageReduction, Stat.Regen),
            },
            new[] { // Avionics
                new ModuleVariant("Targeting Pod", Stat.Crit, Stat.BossDamage),
                new ModuleVariant("Salvage Radar", Stat.Magnet, Stat.GoldBonus),
                new ModuleVariant("EW Jamming Suite", Stat.DamageReduction, Stat.Crit),
                new ModuleVariant("Fire Control Computer", Stat.PowerStart, Stat.Damage),
            },
            new[] { // Warhead
                new ModuleVariant("Tandem Charge", Stat.SpecialDamage, Stat.BossDamage),
                new ModuleVariant("Extended Racks", Stat.SpecialCapacity, Stat.SpecialDamage),
                new ModuleVariant("Thermobaric Fill", Stat.SpecialDamage, Stat.CritDamage),
            },
            new[] { // Drone
                new ModuleVariant("Sentinel Drone", Stat.DroneDamage, Stat.Damage),
                new ModuleVariant("Hunter Drone", Stat.DroneDamage, Stat.Crit),
                new ModuleVariant("Guardian Drone", Stat.DroneDamage, Stat.DamageReduction),
            },
        };

        /// <summary>Value of a stat at Common rarity, level 1, as the module's primary stat.</summary>
        static float Base(Stat s)
        {
            switch (s)
            {
                case Stat.Damage: return 4f;
                case Stat.FireRate: return 3f;
                case Stat.MaxHull: return 5f;
                case Stat.Regen: return 6f;
                case Stat.Speed: return 2.5f;
                case Stat.GoldBonus: return 4f;
                case Stat.Crit: return 1.5f;
                case Stat.CritDamage: return 8f;
                case Stat.SpecialDamage: return 6f;
                case Stat.SpecialCapacity: return 0.6f;
                case Stat.Magnet: return 10f;
                case Stat.DamageReduction: return 1.5f;
                case Stat.PowerStart: return 0.3f;
                case Stat.DroneDamage: return 3f;
                case Stat.BossDamage: return 5f;
            }
            return 1f;
        }

        public static ModuleVariant Variant(ModuleItem m)
        {
            var list = Variants[Mathf.Clamp(m.slot, 0, SlotCount - 1)];
            return list[Mathf.Clamp(m.variant, 0, list.Length - 1)];
        }

        public static string Name(ModuleItem m) => Variant(m).name;

        static float LevelMult(int level) => 1f + 0.12f * (Mathf.Max(1, level) - 1);

        public static float PrimaryValue(ModuleItem m) => Base(Variant(m).primary) * RarityInfo.Power[m.rarity] * LevelMult(m.level);
        public static float SecondaryValue(ModuleItem m) => 0.5f * Base(Variant(m).secondary) * RarityInfo.Power[m.rarity] * LevelMult(m.level);

        public static void Apply(ModuleItem m, StatBlock b)
        {
            var v = Variant(m);
            b.Add(v.primary, PrimaryValue(m));
            b.Add(v.secondary, SecondaryValue(m));
        }

        public static string Describe(ModuleItem m)
        {
            var v = Variant(m);
            return StatInfo.Format(v.primary, PrimaryValue(m)) + "\n" + StatInfo.Format(v.secondary, SecondaryValue(m));
        }

        public static int MaxLevel(ModuleItem m) => RarityInfo.MaxLevel[m.rarity];
        public static int LevelUpCost(ModuleItem m) => Mathf.RoundToInt(RarityInfo.LevelCostBase[m.rarity] * Mathf.Pow(m.level, 1.35f) / 10f) * 10;
        public static int SalvageValue(ModuleItem m)
        {
            // salvage returns the rarity value plus a share of the gold spent on levels
            long spent = 0;
            var tmp = new ModuleItem { rarity = m.rarity };
            for (int l = 1; l < m.level; l++) { tmp.level = l; spent += LevelUpCost(tmp); }
            return RarityInfo.SalvageValue[m.rarity] + (int)(spent * 0.4f);
        }

        public static ModuleItem Random(SaveData save, int rarity, System.Random rng = null)
        {
            rng = rng ?? new System.Random();
            int slot = rng.Next(SlotCount);
            int variant = rng.Next(Variants[slot].Length);
            return save.AddModule(slot, variant, rarity);
        }

        /// <summary>Three unequipped modules of the same slot and rarity fuse into one random module of the next rarity (same slot).</summary>
        public static List<ModuleItem> FuseCandidates(SaveData save, int slot, int rarity)
        {
            var list = new List<ModuleItem>();
            foreach (var m in save.modules)
                if (!m.equipped && m.slot == slot && m.rarity == rarity) list.Add(m);
            list.Sort((a, b) => a.level.CompareTo(b.level));
            return list;
        }
    }

    // =====================================================================================================
    //  Permanent upgrade tracks
    // =====================================================================================================
    public struct TrackDef
    {
        public string name, desc;
        public Stat stat;
        public float perLevel;
        public int maxLevel, baseCost;
        public float growth;
        public TrackDef(string n, string d, Stat s, float per, int max, int cost, float g)
        { name = n; desc = d; stat = s; perLevel = per; maxLevel = max; baseCost = cost; growth = g; }
    }

    public static class Tracks
    {
        public static readonly TrackDef[] All =
        {
            new TrackDef("Cannon Caliber", "Heavier rounds for the main gun.", Stat.Damage, 6f, 15, 400, 1.32f),
            new TrackDef("Feed Rate", "Faster ammunition feed.", Stat.FireRate, 4f, 10, 500, 1.38f),
            new TrackDef("Airframe", "Reinforced structure, more hull.", Stat.MaxHull, 8f, 15, 350, 1.32f),
            new TrackDef("Damage Control", "Faster in-flight hull repair.", Stat.Regen, 10f, 10, 450, 1.35f),
            new TrackDef("Engine Thrust", "Quicker maneuvering.", Stat.Speed, 4f, 8, 600, 1.42f),
            new TrackDef("Payload Bay", "Carry more special-weapon ammo.", Stat.SpecialCapacity, 1f, 5, 1500, 1.7f),
            new TrackDef("Bounty Contracts", "More gold from every kill and mission.", Stat.GoldBonus, 5f, 10, 800, 1.4f),
            new TrackDef("Gun Synchronizer", "Start each sortie with more gun power.", Stat.PowerStart, 1f, 4, 2500, 2.0f),
            new TrackDef("Salvage Magnet", "Collect pickups from farther away.", Stat.Magnet, 15f, 6, 500, 1.45f),
            new TrackDef("Crit Optics", "Higher chance of critical hits.", Stat.Crit, 1.5f, 10, 700, 1.38f),
            new TrackDef("Warhead Tuning", "Special weapons hit harder.", Stat.SpecialDamage, 8f, 10, 650, 1.38f),
            new TrackDef("Composite Armor", "Take less damage from every hit.", Stat.DamageReduction, 2f, 10, 900, 1.4f),
        };

        public static int Cost(int track, int currentLevel)
        {
            var t = All[track];
            return Mathf.RoundToInt(t.baseCost * Mathf.Pow(t.growth, currentLevel) / 10f) * 10;
        }
    }

    // =====================================================================================================
    //  Aircraft
    // =====================================================================================================
    public enum GunPattern { Spread, Stream, ForwardAndDown }

    public struct JetDef
    {
        public string name, role, pilot, callsign, nation, desc;
        public float damage, fireInterval, hull, speed, regen;
        public GunPattern gun;
        public int price;
        public Color body, accent;
    }

    public static class Jets
    {
        // Fictional designs inspired by modern NATO multirole, interceptor and ground-attack jets.
        public static readonly JetDef[] All =
        {
            new JetDef {
                name = "FX-35 Specter", role = "Stealth Multirole", pilot = "Capt. Elena Novak", callsign = "FROST", nation = "Czechia",
                desc = "Balanced stealth fighter. Its cannon fans out into a wide spread as gun power rises.",
                damage = 10f, fireInterval = 0.14f, hull = 100f, speed = 7f, regen = 6f, gun = GunPattern.Spread, price = 0,
                body = new Color(0.46f, 0.52f, 0.58f), accent = new Color(1f, 0.75f, 0.25f) },
            new JetDef {
                name = "TF-4 Tempest", role = "Canard Interceptor", pilot = "Maj. Henrik Lindqvist", callsign = "SAGA", nation = "Norway",
                desc = "Fastest jet in the squad. A tight, very fast forward stream that melts single targets.",
                damage = 8.5f, fireInterval = 0.095f, hull = 85f, speed = 8.6f, regen = 5f, gun = GunPattern.Stream, price = 12000,
                body = new Color(0.62f, 0.66f, 0.72f), accent = new Color(0.3f, 0.85f, 1f) },
            new JetDef {
                name = "AG-10 Bulldog", role = "Armored Ground Attack", pilot = "Lt. Marcus Reyes", callsign = "ANVIL", nation = "United States",
                desc = "Slow and tough. Fires forward and a second stream down at 45° to shred ground targets. Repairs fastest.",
                damage = 11f, fireInterval = 0.16f, hull = 140f, speed = 6.1f, regen = 10f, gun = GunPattern.ForwardAndDown, price = 20000,
                body = new Color(0.42f, 0.48f, 0.40f), accent = new Color(1f, 0.45f, 0.3f) },
        };
    }

    // =====================================================================================================
    //  Special weapons (bought as ammo with gold, one equipped per sortie)
    // =====================================================================================================
    public enum SpecialKind { ClusterBombs, SeekerMissiles, RocketSalvo, Napalm, RailLance, Thermobaric, AegisShield, SwarmDrones }

    public struct SpecialDef
    {
        public string name, desc;
        public SpecialKind kind;
        public int costPerAmmo, baseMax, unlockMission;
        public float cooldown, damage;
    }

    public static class Specials
    {
        public static readonly SpecialDef[] All =
        {
            new SpecialDef { name = "Cluster Bombs", kind = SpecialKind.ClusterBombs, costPerAmmo = 60, baseMax = 20, unlockMission = 0, cooldown = 0.8f, damage = 45f,
                desc = "Drops a stick of bomblets that blanket ground targets." },
            new SpecialDef { name = "Seeker Missiles", kind = SpecialKind.SeekerMissiles, costPerAmmo = 90, baseMax = 16, unlockMission = 0, cooldown = 1.0f, damage = 55f,
                desc = "Four missiles that lock onto the nearest enemies, air or ground." },
            new SpecialDef { name = "Rocket Salvo", kind = SpecialKind.RocketSalvo, costPerAmmo = 70, baseMax = 18, unlockMission = 1, cooldown = 0.9f, damage = 30f,
                desc = "Eight unguided rockets in a forward fan." },
            new SpecialDef { name = "Napalm Canister", kind = SpecialKind.Napalm, costPerAmmo = 120, baseMax = 10, unlockMission = 2, cooldown = 1.4f, damage = 18f,
                desc = "Leaves a burning carpet on the ground that keeps damaging vehicles." },
            new SpecialDef { name = "Rail Lance", kind = SpecialKind.RailLance, costPerAmmo = 180, baseMax = 8, unlockMission = 3, cooldown = 2.0f, damage = 22f,
                desc = "A piercing electromagnetic beam that cuts through everything in front of you." },
            new SpecialDef { name = "Aegis Shield", kind = SpecialKind.AegisShield, costPerAmmo = 160, baseMax = 6, unlockMission = 3, cooldown = 6.0f, damage = 0f,
                desc = "Five seconds of invulnerability. Rams enemies it touches." },
            new SpecialDef { name = "Swarm Drones", kind = SpecialKind.SwarmDrones, costPerAmmo = 220, baseMax = 6, unlockMission = 4, cooldown = 3.0f, damage = 70f,
                desc = "Launches three kamikaze drones that hunt targets." },
            new SpecialDef { name = "Thermobaric Strike", kind = SpecialKind.Thermobaric, costPerAmmo = 450, baseMax = 3, unlockMission = 5, cooldown = 8.0f, damage = 260f,
                desc = "Calls a strike that hits every enemy on screen and clears enemy fire." },
        };

        public static int MaxAmmo(int special, StatBlock stats)
        {
            var d = All[special];
            int extra = Mathf.FloorToInt(stats[Stat.SpecialCapacity] + 0.0001f);
            // capacity adds a share of the base load per point
            return d.baseMax + Mathf.CeilToInt(extra * Mathf.Max(1, d.baseMax / 5f));
        }
    }

    // =====================================================================================================
    //  Loadout = save data → numbers the battle uses
    // =====================================================================================================
    public static class Loadout
    {
        public static StatBlock Compute(SaveData save)
        {
            var b = new StatBlock();
            for (int i = 0; i < Tracks.All.Length; i++)
            {
                int lv = save.Track(i);
                if (lv > 0) b.Add(Tracks.All[i].stat, Tracks.All[i].perLevel * lv);
            }
            foreach (var m in save.modules) if (m.equipped) Modules.Apply(m, b);
            b[Stat.DamageReduction] = Mathf.Min(b[Stat.DamageReduction], 60f);
            b[Stat.Crit] = Mathf.Min(b[Stat.Crit], 75f);
            return b;
        }

        /// <summary>A rough "power rating" to show in the hangar.</summary>
        public static int Rating(SaveData save)
        {
            var b = Compute(save);
            var j = Jets.All[save.selectedJet];
            float dps = j.damage / j.fireInterval * b.Mult(Stat.Damage) * b.Mult(Stat.FireRate) * (1f + b[Stat.Crit] / 100f * (0.5f + b[Stat.CritDamage] / 100f));
            float tough = j.hull * b.Mult(Stat.MaxHull) / (1f - b[Stat.DamageReduction] / 100f);
            return Mathf.RoundToInt(dps * 1.5f + tough + b[Stat.DroneDamage] * 15f);
        }
    }

    // =====================================================================================================
    //  Supply crates
    // =====================================================================================================
    public struct CrateDef
    {
        public string name;
        public int goldCost;          // 0 = real-money only
        public int count;
        public float[] odds;          // per rarity, sums to 100
        public string iapId;
    }

    public static class Crates
    {
        public static readonly CrateDef[] All =
        {
            new CrateDef { name = "Standard Crate", goldCost = 2500, count = 1, odds = new[] { 60f, 28f, 10f, 2f, 0f, 0f } },
            new CrateDef { name = "Elite Crate", goldCost = 9000, count = 1, odds = new[] { 0f, 40f, 38f, 17f, 4.5f, 0.5f } },
            new CrateDef { name = "Command Crate", goldCost = 0, count = 3, odds = new[] { 0f, 0f, 40f, 40f, 17f, 3f }, iapId = IAPIds.CommandCrate },
        };

        public const int PityLimit = 40; // a Legendary is guaranteed after this many crate modules without one

        public static int RollRarity(CrateDef c, SaveData save, System.Random rng)
        {
            if (save.cratesSinceLegendary >= PityLimit && c.odds[4] + c.odds[5] > 0f) { save.cratesSinceLegendary = 0; return (int)Rarity.Legendary; }
            double roll = rng.NextDouble() * 100.0, acc = 0;
            int r = 0;
            for (int i = 0; i < c.odds.Length; i++)
            {
                acc += c.odds[i];
                if (c.odds[i] > 0 && roll < acc) { r = i; break; }
                if (c.odds[i] > 0) r = i;
            }
            if (r >= (int)Rarity.Legendary) save.cratesSinceLegendary = 0; else save.cratesSinceLegendary++;
            return r;
        }

        public static string OddsText(CrateDef c)
        {
            var parts = new List<string>();
            for (int i = 0; i < c.odds.Length; i++) if (c.odds[i] > 0) parts.Add(RarityInfo.Names[i] + " " + c.odds[i].ToString("0.#") + "%");
            return string.Join("  ·  ", parts);
        }

        public static List<ModuleItem> Open(CrateDef c, SaveData save)
        {
            var rng = new System.Random();
            var got = new List<ModuleItem>();
            for (int i = 0; i < c.count; i++) got.Add(Modules.Random(save, RollRarity(c, save, rng), rng));
            save.cratesOpened++;
            return got;
        }
    }

    /// <summary>The daily deal: one Epic-or-better module that changes every UTC day.</summary>
    public static class DailyDeal
    {
        public static string Today => DateTime.UtcNow.ToString("yyyy-MM-dd");

        public static void Describe(out int slot, out int variant, out int rarity, out int price)
        {
            var rng = new System.Random(Today.GetHashCode() ^ 0x5A17);
            slot = rng.Next(Modules.SlotCount);
            variant = rng.Next(Modules.Variants[slot].Length);
            double r = rng.NextDouble();
            rarity = r < 0.70 ? (int)Rarity.Epic : r < 0.96 ? (int)Rarity.Legendary : (int)Rarity.Mythic;
            price = rarity == (int)Rarity.Epic ? 12000 : rarity == (int)Rarity.Legendary ? 30000 : 90000;
        }

        public static bool Bought(SaveData save) => save.dealDay == Today && save.dealBought;
    }
}
