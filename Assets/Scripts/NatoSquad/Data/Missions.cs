using UnityEngine;

namespace NatoSquad
{
    public enum Terrain { Grassland, Ocean, Mountains, Desert, Storm, Arctic, Canyon, Fortress }

    public enum EnemyKind
    {
        // air
        Fighter, Interceptor, Bomber, Gunship, Drone, AceFighter,
        // ground / sea
        Tank, AAGun, SamLauncher, Truck, Gunboat, Bunker,
    }

    public enum BossKind { RailBattery, Destroyer, PeakFortress, SandCrawler, StratoGunship, Submarine, WraithAce, BlackCitadel }

    public struct MissionDef
    {
        public string code, name, briefing, bossName;
        public Terrain terrain;
        public BossKind boss;
        public float length;        // seconds of waves before the boss
        public int reward;          // gold for clearing (first clear pays double)
        public EnemyKind[] air, ground;
        public Color skyTop, skyBottom, far, ground1, ground2;
        public bool sea;
    }

    public static class Missions
    {
        public static readonly MissionDef[] All =
        {
            new MissionDef {
                code = "OP 01", name = "Border Shield", terrain = Terrain.Grassland, boss = BossKind.RailBattery, bossName = "IRONCLAD RAIL BATTERY",
                briefing = "Iron Dominion armor is massing at the eastern border. Break their air cover and stop the rail battery shelling our airfield.",
                length = 70f, reward = 2200,
                air = new[] { EnemyKind.Fighter, EnemyKind.Drone, EnemyKind.Fighter },
                ground = new[] { EnemyKind.Tank, EnemyKind.Truck, EnemyKind.AAGun },
                skyTop = new Color(0.25f, 0.52f, 0.85f), skyBottom = new Color(0.72f, 0.86f, 0.98f), far = new Color(0.42f, 0.58f, 0.62f),
                ground1 = new Color(0.33f, 0.52f, 0.25f), ground2 = new Color(0.25f, 0.40f, 0.18f) },
            new MissionDef {
                code = "OP 02", name = "Iron Tide", terrain = Terrain.Ocean, boss = BossKind.Destroyer, bossName = "DESTROYER TIDEBREAKER",
                briefing = "A missile destroyer is blockading the strait. Its escorts and gunboats are thick. Sink it.",
                length = 75f, reward = 3200, sea = true,
                air = new[] { EnemyKind.Fighter, EnemyKind.Gunship, EnemyKind.Interceptor },
                ground = new[] { EnemyKind.Gunboat, EnemyKind.Gunboat, EnemyKind.SamLauncher },
                skyTop = new Color(0.20f, 0.45f, 0.78f), skyBottom = new Color(0.80f, 0.88f, 0.95f), far = new Color(0.50f, 0.62f, 0.72f),
                ground1 = new Color(0.10f, 0.33f, 0.55f), ground2 = new Color(0.07f, 0.24f, 0.42f) },
            new MissionDef {
                code = "OP 03", name = "Granite Pass", terrain = Terrain.Mountains, boss = BossKind.PeakFortress, bossName = "PEAK FORTRESS",
                briefing = "Air-defense sites carved into the mountains guard the pass. Fly low, hit the SAMs, and crack the fortress.",
                length = 80f, reward = 4400,
                air = new[] { EnemyKind.Fighter, EnemyKind.Interceptor, EnemyKind.Drone },
                ground = new[] { EnemyKind.SamLauncher, EnemyKind.AAGun, EnemyKind.Bunker },
                skyTop = new Color(0.36f, 0.50f, 0.75f), skyBottom = new Color(0.82f, 0.80f, 0.86f), far = new Color(0.46f, 0.48f, 0.58f),
                ground1 = new Color(0.45f, 0.42f, 0.38f), ground2 = new Color(0.34f, 0.31f, 0.29f) },
            new MissionDef {
                code = "OP 04", name = "Dust Convoy", terrain = Terrain.Desert, boss = BossKind.SandCrawler, bossName = "SAND CRAWLER",
                briefing = "A supply convoy escorted by a mobile land fortress is crossing the dunes. Leave nothing moving.",
                length = 85f, reward = 5800,
                air = new[] { EnemyKind.Gunship, EnemyKind.Fighter, EnemyKind.Bomber },
                ground = new[] { EnemyKind.Tank, EnemyKind.Truck, EnemyKind.AAGun, EnemyKind.Tank },
                skyTop = new Color(0.40f, 0.62f, 0.88f), skyBottom = new Color(0.98f, 0.86f, 0.62f), far = new Color(0.82f, 0.66f, 0.45f),
                ground1 = new Color(0.86f, 0.70f, 0.44f), ground2 = new Color(0.74f, 0.57f, 0.33f) },
            new MissionDef {
                code = "OP 05", name = "Thunderhead", terrain = Terrain.Storm, boss = BossKind.StratoGunship, bossName = "STRATO-GUNSHIP COLOSSUS",
                briefing = "Inside the storm front a flying gunship launches drone swarms. Punch through the clouds and bring it down.",
                length = 85f, reward = 7600,
                air = new[] { EnemyKind.Bomber, EnemyKind.Drone, EnemyKind.Interceptor, EnemyKind.Fighter },
                ground = new EnemyKind[0],
                skyTop = new Color(0.16f, 0.18f, 0.25f), skyBottom = new Color(0.40f, 0.42f, 0.50f), far = new Color(0.28f, 0.30f, 0.38f),
                ground1 = new Color(0.30f, 0.32f, 0.40f), ground2 = new Color(0.22f, 0.24f, 0.31f) },
            new MissionDef {
                code = "OP 06", name = "Polar Night", terrain = Terrain.Arctic, boss = BossKind.Submarine, bossName = "ICEBREAKER SUBMARINE",
                briefing = "A missile submarine is surfacing through the ice. Neutralize the coastal batteries and catch it on the surface.",
                length = 90f, reward = 9800, sea = true,
                air = new[] { EnemyKind.Interceptor, EnemyKind.Gunship, EnemyKind.Fighter },
                ground = new[] { EnemyKind.Gunboat, EnemyKind.SamLauncher, EnemyKind.AAGun },
                skyTop = new Color(0.08f, 0.14f, 0.30f), skyBottom = new Color(0.45f, 0.62f, 0.75f), far = new Color(0.70f, 0.80f, 0.88f),
                ground1 = new Color(0.62f, 0.78f, 0.88f), ground2 = new Color(0.40f, 0.56f, 0.70f) },
            new MissionDef {
                code = "OP 07", name = "Night Canyon", terrain = Terrain.Canyon, boss = BossKind.WraithAce, bossName = "THE WRAITH",
                briefing = "Their top ace hunts in the canyon at night. Stay sharp. He dodges, and he never stops shooting.",
                length = 90f, reward = 12500,
                air = new[] { EnemyKind.AceFighter, EnemyKind.Interceptor, EnemyKind.Drone, EnemyKind.Fighter },
                ground = new[] { EnemyKind.AAGun, EnemyKind.SamLauncher, EnemyKind.Bunker },
                skyTop = new Color(0.04f, 0.05f, 0.14f), skyBottom = new Color(0.22f, 0.14f, 0.30f), far = new Color(0.18f, 0.12f, 0.20f),
                ground1 = new Color(0.40f, 0.20f, 0.16f), ground2 = new Color(0.28f, 0.13f, 0.11f) },
            new MissionDef {
                code = "OP 08", name = "Black Citadel", terrain = Terrain.Fortress, boss = BossKind.BlackCitadel, bossName = "THE BLACK CITADEL",
                briefing = "The Dominion's airborne command fortress. Everything they have left is defending it. End this war.",
                length = 100f, reward = 18000,
                air = new[] { EnemyKind.AceFighter, EnemyKind.Bomber, EnemyKind.Gunship, EnemyKind.Interceptor, EnemyKind.Drone },
                ground = new[] { EnemyKind.Bunker, EnemyKind.SamLauncher, EnemyKind.AAGun, EnemyKind.Tank },
                skyTop = new Color(0.12f, 0.06f, 0.10f), skyBottom = new Color(0.62f, 0.25f, 0.18f), far = new Color(0.25f, 0.14f, 0.16f),
                ground1 = new Color(0.25f, 0.24f, 0.27f), ground2 = new Color(0.17f, 0.16f, 0.19f) },
        };

        public static int Count => All.Length;

        /// <summary>Difficulty multiplier for enemy health and damage. Replays of cleared missions get "veteran" scaling.</summary>
        public static float Difficulty(int mission) => 1f + 0.42f * mission;
    }
}
