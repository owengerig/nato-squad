using System.Collections.Generic;
using UnityEngine;

namespace NatoSquad
{
    /// <summary>
    /// Every sprite in the game, drawn once with Painter and cached. Aircraft and vehicles are original designs
    /// (side view). Player jets face right; enemies are drawn mirrored to face left.
    /// </summary>
    public static class Art
    {
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        static Sprite Get(string key, System.Func<Sprite> make)
        {
            if (cache.TryGetValue(key, out var s) && s != null) return s;
            s = make();
            s.name = key;
            cache[key] = s;
            return s;
        }

        // ---------------------------------------------------------------- palette
        static readonly Color Glass = new Color(0.35f, 0.75f, 0.95f);
        static readonly Color GlassTop = new Color(0.75f, 0.95f, 1f);
        static readonly Color Dark = new Color(0.16f, 0.18f, 0.21f);
        static readonly Color Ink = new Color(0.07f, 0.08f, 0.10f, 0.9f);
        static readonly Color EnemyBody = new Color(0.47f, 0.42f, 0.40f);
        static readonly Color EnemyRed = new Color(0.86f, 0.22f, 0.16f);
        static readonly Color Metal = new Color(0.55f, 0.57f, 0.60f);
        static readonly Color Olive = new Color(0.40f, 0.42f, 0.30f);
        static readonly Color Sand = new Color(0.70f, 0.60f, 0.42f);

        public const float PPU = 80f;

        // ================================================================ player jets
        public static Sprite Jet(int index) => Get("jet" + index, () =>
        {
            var j = Jets.All[Mathf.Clamp(index, 0, Jets.All.Length - 1)];
            var b = j.body; var hi = Col.Shade(b, 1.25f); var lo = Col.Shade(b, 0.72f);
            var p = new Painter(128, 48);
            switch (index)
            {
                default: // FX-35 Specter: stealth multirole, twin canted tails
                    p.Poly(lo, "0.02,0.40 0.20,0.42 0.24,0.36 0.06,0.30");
                    p.Poly(lo, "0.34,0.42 0.62,0.44 0.44,0.26 0.32,0.26");
                    p.Poly(Col.Shade(b, 0.85f), "0.10,0.58 0.26,0.60 0.22,0.93 0.14,0.93");
                    p.Poly(hi, "0.0,0.40 0.04,0.34 0.30,0.32 0.62,0.34 0.84,0.40 1.0,0.48 0.86,0.54 0.70,0.60 0.52,0.62 0.30,0.60 0.08,0.58 0.0,0.54", lo);
                    p.Poly(b, "0.04,0.58 0.20,0.60 0.12,0.97 0.05,0.97");
                    p.Rect(Dark, 0.0f, 0.38f, 0.05f, 0.55f);
                    p.Poly(Dark, "0.54,0.40 0.66,0.40 0.62,0.50 0.52,0.50");
                    p.Poly(hi, "0.36,0.38 0.62,0.40 0.48,0.14 0.38,0.14", b);
                    p.Rect(j.accent, 0.28f, 0.47f, 0.46f, 0.50f);
                    p.Ellipse(GlassTop, 0.71f, 0.60f, 0.11f, 0.10f, Glass);
                    break;
                case 1: // TF-4 Tempest: canard delta interceptor
                    p.Poly(lo, "0.18,0.42 0.62,0.44 0.30,0.28");
                    p.Poly(hi, "0.0,0.40 0.05,0.35 0.40,0.33 0.75,0.38 1.0,0.47 0.80,0.54 0.62,0.58 0.30,0.58 0.06,0.56 0.0,0.52", lo);
                    p.Poly(b, "0.03,0.56 0.24,0.58 0.15,0.98 0.07,0.98");
                    p.Poly(Dark, "0.48,0.30 0.66,0.33 0.63,0.41 0.47,0.39");
                    p.Rect(Dark, 0.0f, 0.39f, 0.04f, 0.53f);
                    p.Poly(hi, "0.16,0.38 0.62,0.40 0.24,0.12", b);
                    p.Poly(b, "0.66,0.50 0.80,0.53 0.70,0.43");
                    p.Rect(j.accent, 0.20f, 0.48f, 0.56f, 0.51f);
                    p.Ellipse(GlassTop, 0.75f, 0.58f, 0.10f, 0.09f, Glass);
                    break;
                case 2: // AG-10 Bulldog: armored straight-wing attack jet, high engine pod
                    p.Poly(Col.Shade(b, 0.8f), "0.06,0.52 0.16,0.52 0.14,0.90 0.07,0.90");
                    p.Poly(hi, "0.0,0.42 0.06,0.38 0.70,0.36 0.92,0.40 1.0,0.46 0.94,0.52 0.80,0.58 0.66,0.60 0.20,0.56 0.02,0.54", lo);
                    p.Ellipse(hi, 0.30f, 0.66f, 0.14f, 0.14f, lo);
                    p.Rect(Dark, 0.15f, 0.58f, 0.18f, 0.74f);
                    p.Poly(b, "0.0,0.52 0.10,0.52 0.08,0.96 0.01,0.96");
                    p.Rect(lo, 0.34f, 0.37f, 0.64f, 0.45f);
                    p.Ellipse(Dark, 0.46f, 0.31f, 0.07f, 0.06f);
                    p.Ellipse(Dark, 0.58f, 0.31f, 0.07f, 0.06f);
                    p.Rect(Dark, 0.95f, 0.42f, 1.0f, 0.45f);
                    p.Poly(j.accent, "0.86,0.44 1.0,0.46 0.94,0.40 0.86,0.40");
                    p.Ellipse(GlassTop, 0.80f, 0.58f, 0.08f, 0.10f, Glass);
                    break;
            }
            p.Outline(Ink);
            return p.ToSprite(PPU, new Vector2(0.5f, 0.45f));
        });

        // ================================================================ enemy aircraft (face left)
        public static Sprite Enemy(EnemyKind k) => Get("enemy" + k, () =>
        {
            Painter p;
            var b = EnemyBody; var hi = Col.Shade(b, 1.25f); var lo = Col.Shade(b, 0.7f);
            switch (k)
            {
                case EnemyKind.Fighter:
                    p = new Painter(112, 44) { Mirror = true };
                    p.Poly(lo, "0.30,0.42 0.60,0.44 0.40,0.24 0.30,0.24");
                    p.Poly(hi, "0.0,0.42 0.05,0.36 0.35,0.34 0.70,0.38 1.0,0.47 0.78,0.55 0.58,0.58 0.30,0.58 0.06,0.56", lo);
                    p.Poly(b, "0.04,0.55 0.18,0.57 0.14,0.95 0.07,0.95");
                    p.Poly(Col.Shade(b, 0.9f), "0.12,0.55 0.26,0.57 0.24,0.88 0.18,0.88");
                    p.Rect(Dark, 0.0f, 0.40f, 0.04f, 0.54f);
                    p.Poly(hi, "0.32,0.38 0.58,0.40 0.42,0.12 0.33,0.12", b);
                    p.Rect(EnemyRed, 0.08f, 0.70f, 0.16f, 0.82f);
                    p.Ellipse(new Color(0.95f, 0.75f, 0.3f), 0.73f, 0.56f, 0.10f, 0.09f, new Color(0.7f, 0.4f, 0.1f));
                    break;
                case EnemyKind.Interceptor:
                    p = new Painter(112, 40) { Mirror = true };
                    p.Poly(lo, "0.12,0.44 0.55,0.46 0.25,0.25");
                    p.Poly(hi, "0.0,0.42 0.06,0.37 0.50,0.35 1.0,0.47 0.62,0.56 0.30,0.58 0.06,0.56", lo);
                    p.Poly(b, "0.03,0.55 0.22,0.57 0.10,0.98 0.04,0.98");
                    p.Poly(hi, "0.10,0.40 0.56,0.42 0.18,0.10", b);
                    p.Rect(Dark, 0.0f, 0.40f, 0.04f, 0.54f);
                    p.Rect(EnemyRed, 0.06f, 0.75f, 0.12f, 0.88f);
                    p.Ellipse(new Color(0.95f, 0.75f, 0.3f), 0.66f, 0.54f, 0.10f, 0.09f, new Color(0.7f, 0.4f, 0.1f));
                    break;
                case EnemyKind.AceFighter:
                    p = new Painter(128, 44) { Mirror = true };
                    var ab = new Color(0.14f, 0.14f, 0.17f);
                    p.Poly(Col.Shade(ab, 0.7f), "0.20,0.42 0.62,0.44 0.32,0.20");
                    p.Poly(Col.Shade(ab, 1.4f), "0.0,0.42 0.05,0.36 0.40,0.33 0.76,0.38 1.0,0.47 0.80,0.55 0.60,0.58 0.30,0.58 0.06,0.56", ab);
                    p.Poly(ab, "0.04,0.55 0.22,0.57 0.16,0.97 0.08,0.97");
                    p.Poly(Col.Shade(ab, 1.3f), "0.18,0.38 0.64,0.40 0.28,0.08", ab);
                    p.Poly(EnemyRed, "0.30,0.47 0.70,0.44 0.72,0.48 0.30,0.51");
                    p.Rect(EnemyRed, 0.07f, 0.72f, 0.14f, 0.90f);
                    p.Ellipse(new Color(1f, 0.3f, 0.25f), 0.74f, 0.56f, 0.10f, 0.09f, new Color(0.5f, 0.05f, 0.05f));
                    break;
                case EnemyKind.Bomber:
                    p = new Painter(192, 80) { Mirror = true };
                    p.Poly(lo, "0.30,0.44 0.62,0.46 0.42,0.24 0.34,0.24");
                    p.Poly(hi, "0.0,0.45 0.04,0.38 0.80,0.36 0.96,0.42 1.0,0.50 0.92,0.58 0.70,0.62 0.10,0.60 0.0,0.56", lo);
                    p.Poly(b, "0.0,0.56 0.12,0.60 0.08,0.96 0.02,0.96");
                    p.Poly(hi, "0.30,0.40 0.62,0.42 0.40,0.06 0.32,0.06", b);
                    p.Ellipse(Dark, 0.45f, 0.24f, 0.07f, 0.07f);
                    p.Ellipse(Dark, 0.38f, 0.12f, 0.07f, 0.07f);
                    p.Rect(EnemyRed, 0.03f, 0.72f, 0.08f, 0.85f);
                    p.Ellipse(new Color(0.95f, 0.75f, 0.3f), 0.90f, 0.53f, 0.05f, 0.08f, new Color(0.7f, 0.4f, 0.1f));
                    for (int i = 0; i < 6; i++) p.Ellipse(Dark, 0.30f + i * 0.08f, 0.52f, 0.012f, 0.03f);
                    break;
                case EnemyKind.Gunship:
                    p = new Painter(128, 64) { Mirror = true };
                    p.Poly(lo, "0.0,0.52 0.35,0.46 0.35,0.62 0.0,0.60");
                    p.Poly(b, "0.0,0.55 0.07,0.55 0.05,0.88 0.0,0.88");
                    p.Ellipse(hi, 0.56f, 0.46f, 0.28f, 0.27f, lo);
                    p.Ellipse(new Color(0.95f, 0.75f, 0.3f), 0.76f, 0.52f, 0.12f, 0.14f, new Color(0.7f, 0.4f, 0.1f));
                    p.Rect(Dark, 0.42f, 0.28f, 0.72f, 0.34f);
                    p.Ellipse(Dark, 0.50f, 0.24f, 0.08f, 0.06f);
                    p.Rect(Dark, 0.38f, 0.04f, 0.82f, 0.08f);
                    p.Rect(Dark, 0.46f, 0.08f, 0.48f, 0.22f);
                    p.Rect(Dark, 0.72f, 0.08f, 0.74f, 0.22f);
                    p.Rect(Dark, 0.53f, 0.70f, 0.58f, 0.82f);
                    p.Rect(EnemyRed, 0.08f, 0.62f, 0.18f, 0.72f);
                    p.Rect(Dark, 0.84f, 0.30f, 1.0f, 0.34f);
                    break;
                case EnemyKind.Drone:
                default:
                    p = new Painter(64, 32) { Mirror = true };
                    p.Poly(lo, "0.30,0.44 0.62,0.44 0.42,0.10");
                    p.Poly(hi, "0.0,0.50 0.12,0.36 0.85,0.38 1.0,0.50 0.85,0.62 0.12,0.62", lo);
                    p.Rect(Dark, 0.0f, 0.30f, 0.04f, 0.70f);
                    p.Ellipse(EnemyRed, 0.82f, 0.48f, 0.07f, 0.12f);
                    p.Poly(b, "0.10,0.60 0.24,0.60 0.16,0.92");
                    break;
            }
            p.Outline(Ink);
            return p.ToSprite(PPU);
        });

        // ================================================================ ground & sea units (face left)
        public static Sprite Ground(EnemyKind k) => Get("ground" + k, () =>
        {
            Painter p;
            switch (k)
            {
                case EnemyKind.Tank:
                default:
                    p = new Painter(112, 56) { Mirror = true };
                    p.Poly(Dark, "0.04,0.04 0.96,0.04 1.0,0.18 0.92,0.30 0.08,0.30 0.0,0.18");
                    for (int i = 0; i < 6; i++) p.Ellipse(Metal, 0.12f + i * 0.152f, 0.16f, 0.055f, 0.10f, Col.Shade(Metal, 0.6f));
                    p.Poly(Col.Shade(Olive, 1.2f), "0.04,0.30 0.96,0.30 0.90,0.50 0.10,0.50", Olive);
                    p.Poly(Col.Shade(Olive, 1.3f), "0.30,0.50 0.66,0.50 0.62,0.72 0.36,0.72", Olive);
                    p.Rect(EnemyRed, 0.40f, 0.58f, 0.46f, 0.64f);
                    break;
                case EnemyKind.AAGun:
                    p = new Painter(96, 48) { Mirror = true };
                    p.Poly(Col.Shade(Metal, 1.1f), "0.08,0.0 0.92,0.0 0.80,0.30 0.20,0.30", Col.Shade(Metal, 0.7f));
                    p.Ellipse(Col.Shade(Olive, 1.3f), 0.5f, 0.36f, 0.22f, 0.24f, Olive);
                    p.Rect(EnemyRed, 0.44f, 0.30f, 0.56f, 0.38f);
                    break;
                case EnemyKind.SamLauncher:
                    p = new Painter(128, 64) { Mirror = true };
                    p.Ellipse(Dark, 0.18f, 0.12f, 0.08f, 0.12f); p.Ellipse(Dark, 0.42f, 0.12f, 0.08f, 0.12f); p.Ellipse(Dark, 0.80f, 0.12f, 0.08f, 0.12f);
                    p.Poly(Col.Shade(Olive, 1.2f), "0.04,0.18 0.96,0.18 0.96,0.36 0.04,0.36", Olive);
                    p.Poly(Col.Shade(Olive, 1.3f), "0.74,0.36 0.98,0.36 0.98,0.62 0.80,0.62", Olive);
                    p.Ellipse(Glass, 0.92f, 0.52f, 0.04f, 0.07f);
                    p.Poly(Col.Shade(Metal, 1.1f), "0.10,0.40 0.62,0.40 0.20,0.92 0.08,0.86", Metal);
                    p.Poly(Color.white, "0.16,0.66 0.24,0.70 0.12,0.94 0.06,0.90");
                    p.Poly(EnemyRed, "0.08,0.88 0.14,0.92 0.12,0.98 0.06,0.96");
                    break;
                case EnemyKind.Truck:
                    p = new Painter(112, 52) { Mirror = true };
                    p.Ellipse(Dark, 0.18f, 0.14f, 0.08f, 0.15f); p.Ellipse(Dark, 0.40f, 0.14f, 0.08f, 0.15f); p.Ellipse(Dark, 0.84f, 0.14f, 0.08f, 0.15f);
                    p.Poly(Col.Shade(Sand, 1.15f), "0.04,0.22 0.66,0.22 0.66,0.86 0.10,0.86 0.04,0.80", Col.Shade(Sand, 0.8f));
                    for (int i = 0; i < 4; i++) p.Rect(Col.Shade(Sand, 0.75f), 0.12f + i * 0.14f, 0.26f, 0.13f + i * 0.14f, 0.84f);
                    p.Poly(Col.Shade(Olive, 1.3f), "0.68,0.22 1.0,0.22 1.0,0.50 0.90,0.70 0.68,0.70", Olive);
                    p.Poly(Glass, "0.86,0.50 0.96,0.50 0.90,0.64 0.82,0.64");
                    break;
                case EnemyKind.Gunboat:
                    p = new Painter(160, 56) { Mirror = true };
                    p.Poly(Col.Shade(Metal, 1.2f), "0.0,0.40 1.0,0.44 0.92,0.06 0.06,0.10", Col.Shade(Metal, 0.6f));
                    p.Rect(EnemyRed, 0.06f, 0.10f, 0.92f, 0.16f);
                    p.Poly(Col.Shade(Metal, 1.35f), "0.30,0.40 0.62,0.40 0.58,0.72 0.36,0.72", Metal);
                    p.Rect(Glass, 0.40f, 0.60f, 0.56f, 0.66f);
                    p.Rect(Dark, 0.46f, 0.72f, 0.48f, 0.98f);
                    p.Ellipse(Col.Shade(Metal, 1.2f), 0.80f, 0.46f, 0.08f, 0.12f, Metal);
                    break;
                case EnemyKind.Bunker:
                    p = new Painter(128, 64) { Mirror = true };
                    p.Poly(new Color(0.66f, 0.66f, 0.62f), "0.04,0.0 0.96,0.0 0.84,0.56 0.16,0.56", new Color(0.42f, 0.42f, 0.40f));
                    p.Rect(Dark, 0.26f, 0.30f, 0.74f, 0.38f);
                    p.Ellipse(Col.Shade(Olive, 1.3f), 0.5f, 0.58f, 0.18f, 0.22f, Olive);
                    p.Rect(EnemyRed, 0.46f, 0.66f, 0.54f, 0.72f);
                    break;
            }
            p.Outline(Ink);
            return p.ToSprite(PPU);
        });

        /// <summary>Gun barrel (pivot at the breech), rotated toward the player by tanks, AA guns and boss turrets.</summary>
        public static Sprite Barrel(bool twin) => Get("barrel" + twin, () =>
        {
            var p = new Painter(48, twin ? 14 : 8);
            if (twin) { p.Rect(Dark, 0f, 0.05f, 1f, 0.38f); p.Rect(Dark, 0f, 0.62f, 1f, 0.95f); }
            else p.Rect(Dark, 0f, 0.1f, 1f, 0.9f);
            p.Rect(Metal, 0f, 0.2f, 0.3f, 0.8f);
            return p.ToSprite(PPU, new Vector2(0f, 0.5f));
        });

        public static Sprite Rotor() => Get("rotor", () =>
        {
            var p = new Painter(128, 8);
            p.Soft(new Color(0.15f, 0.15f, 0.18f, 0.85f), 0.5f, 0.5f, 0.5f, 0.5f, 0.6f);
            return p.ToSprite(PPU);
        });

        // ================================================================ bosses (face left)
        public static Sprite Boss(BossKind k) => Get("boss" + k, () =>
        {
            Painter p;
            var steel = new Color(0.40f, 0.40f, 0.44f); var hiS = Col.Shade(steel, 1.35f); var loS = Col.Shade(steel, 0.65f);
            var glow = new Color(1f, 0.35f, 0.15f);
            switch (k)
            {
                case BossKind.RailBattery:
                default:
                    p = new Painter(512, 160) { Mirror = true };
                    for (int i = 0; i < 12; i++) p.Ellipse(Dark, 0.04f + i * 0.084f, 0.08f, 0.025f, 0.08f);
                    p.Rect(loS, 0.0f, 0.10f, 1.0f, 0.18f);
                    p.Poly(hiS, "0.0,0.18 0.30,0.18 0.30,0.50 0.02,0.50", steel);
                    p.Poly(hiS, "0.34,0.18 0.98,0.18 0.98,0.40 0.34,0.40", steel);
                    p.Poly(Olive, "0.46,0.40 0.86,0.40 0.80,0.58 0.52,0.58");
                    p.Poly(Col.Shade(steel, 1.2f), "0.56,0.52 0.62,0.52 1.0,0.70 1.0,0.76 0.58,0.62", loS);
                    p.Ellipse(glow, 0.64f, 0.56f, 0.03f, 0.08f);
                    p.Rect(Dark, 0.05f, 0.30f, 0.25f, 0.36f);
                    p.Ellipse(Olive, 0.15f, 0.52f, 0.06f, 0.12f);
                    p.Rect(EnemyRed, 0.36f, 0.30f, 0.44f, 0.34f);
                    break;
                case BossKind.Destroyer:
                    p = new Painter(576, 192) { Mirror = true };
                    p.Poly(hiS, "0.0,0.36 1.0,0.42 0.94,0.04 0.08,0.06", loS);
                    p.Rect(EnemyRed, 0.08f, 0.06f, 0.94f, 0.10f);
                    p.Poly(Col.Shade(steel, 1.45f), "0.30,0.36 0.66,0.36 0.62,0.62 0.34,0.62", steel);
                    p.Poly(Col.Shade(steel, 1.5f), "0.40,0.62 0.58,0.62 0.55,0.80 0.43,0.80", steel);
                    p.Rect(Glass, 0.42f, 0.55f, 0.58f, 0.59f);
                    p.Rect(Dark, 0.48f, 0.80f, 0.50f, 1.0f);
                    p.Rect(Dark, 0.44f, 0.92f, 0.54f, 0.94f);
                    p.Ellipse(hiS, 0.78f, 0.42f, 0.05f, 0.10f, steel);
                    p.Ellipse(hiS, 0.18f, 0.42f, 0.05f, 0.10f, steel);
                    for (int i = 0; i < 5; i++) p.Rect(Dark, 0.68f + i * 0.02f, 0.36f, 0.685f + i * 0.02f, 0.40f);
                    p.Rect(loS, 0.24f, 0.36f, 0.28f, 0.50f);
                    break;
                case BossKind.PeakFortress:
                    p = new Painter(384, 320) { Mirror = true };
                    p.Poly(new Color(0.55f, 0.52f, 0.50f), "0.0,0.0 1.0,0.0 0.86,0.50 0.66,0.78 0.50,0.86 0.30,0.70 0.12,0.40", new Color(0.30f, 0.28f, 0.27f));
                    p.Poly(new Color(0.92f, 0.94f, 0.96f), "0.40,0.80 0.50,0.86 0.66,0.78 0.60,0.72 0.52,0.78 0.46,0.74");
                    p.Poly(new Color(0.62f, 0.62f, 0.60f), "0.30,0.28 0.80,0.28 0.76,0.52 0.34,0.52", new Color(0.45f, 0.45f, 0.44f));
                    p.Rect(Dark, 0.40f, 0.36f, 0.70f, 0.42f);
                    p.Ellipse(Olive, 0.55f, 0.60f, 0.08f, 0.07f);
                    p.Ellipse(Olive, 0.22f, 0.30f, 0.06f, 0.05f);
                    p.Ellipse(Olive, 0.84f, 0.36f, 0.06f, 0.05f);
                    p.Ellipse(glow, 0.55f, 0.39f, 0.03f, 0.02f);
                    p.Rect(Dark, 0.53f, 0.64f, 0.57f, 0.74f);
                    break;
                case BossKind.SandCrawler:
                    p = new Painter(448, 224) { Mirror = true };
                    p.Poly(Dark, "0.02,0.02 0.98,0.02 1.0,0.20 0.94,0.30 0.06,0.30 0.0,0.20");
                    for (int i = 0; i < 9; i++) p.Ellipse(Metal, 0.08f + i * 0.105f, 0.15f, 0.04f, 0.09f, Col.Shade(Metal, 0.6f));
                    p.Poly(Col.Shade(Sand, 1.2f), "0.04,0.30 0.96,0.30 0.90,0.56 0.10,0.56", Col.Shade(Sand, 0.7f));
                    p.Poly(Col.Shade(Sand, 1.25f), "0.20,0.56 0.74,0.56 0.68,0.76 0.26,0.76", Sand);
                    p.Poly(Col.Shade(Sand, 1.3f), "0.36,0.76 0.60,0.76 0.56,0.90 0.40,0.90", Sand);
                    p.Rect(Glass, 0.40f, 0.82f, 0.56f, 0.86f);
                    p.Rect(Dark, 0.62f, 0.62f, 1.0f, 0.68f);
                    p.Ellipse(Olive, 0.15f, 0.60f, 0.06f, 0.09f);
                    p.Rect(EnemyRed, 0.10f, 0.40f, 0.90f, 0.43f);
                    break;
                case BossKind.StratoGunship:
                    p = new Painter(448, 224) { Mirror = true };
                    p.Poly(loS, "0.10,0.50 0.70,0.52 0.40,0.10 0.26,0.10");
                    p.Poly(hiS, "0.0,0.50 0.06,0.40 0.80,0.38 0.98,0.48 1.0,0.56 0.90,0.64 0.20,0.66 0.0,0.60", loS);
                    p.Poly(steel, "0.0,0.60 0.14,0.64 0.10,0.96 0.03,0.96");
                    p.Poly(hiS, "0.12,0.46 0.70,0.48 0.30,0.0 0.18,0.0", steel);
                    for (int i = 0; i < 3; i++) p.Ellipse(Dark, 0.30f + i * 0.13f, 0.26f, 0.05f, 0.07f);
                    p.Ellipse(Olive, 0.55f, 0.36f, 0.05f, 0.08f);
                    p.Ellipse(Olive, 0.75f, 0.38f, 0.04f, 0.07f);
                    p.Ellipse(Glass, 0.92f, 0.55f, 0.04f, 0.06f);
                    p.Rect(EnemyRed, 0.04f, 0.74f, 0.09f, 0.84f);
                    p.Ellipse(glow, 0.04f, 0.48f, 0.02f, 0.06f);
                    break;
                case BossKind.Submarine:
                    p = new Painter(512, 160) { Mirror = true };
                    var sub = new Color(0.12f, 0.13f, 0.16f);
                    p.Poly(Col.Shade(sub, 2.0f), "0.0,0.30 0.06,0.18 0.90,0.16 1.0,0.30 0.94,0.46 0.06,0.48", sub);
                    p.Poly(Col.Shade(sub, 2.2f), "0.46,0.46 0.66,0.46 0.62,0.86 0.50,0.86", sub);
                    p.Poly(Col.Shade(sub, 2.0f), "0.48,0.70 0.42,0.72 0.42,0.66 0.48,0.64");
                    for (int i = 0; i < 6; i++) p.Rect(EnemyRed, 0.16f + i * 0.045f, 0.46f, 0.19f + i * 0.045f, 0.50f);
                    p.Poly(new Color(0.85f, 0.93f, 1f), "0.0,0.20 0.18,0.24 0.30,0.14 0.10,0.10", new Color(0.6f, 0.75f, 0.85f));
                    p.Poly(new Color(0.85f, 0.93f, 1f), "0.72,0.18 0.84,0.30 1.0,0.22 0.92,0.12", new Color(0.6f, 0.75f, 0.85f));
                    break;
                case BossKind.WraithAce:
                    p = new Painter(192, 64) { Mirror = true };
                    var wb = new Color(0.10f, 0.10f, 0.12f);
                    p.Poly(Col.Shade(wb, 0.7f), "0.16,0.44 0.64,0.46 0.30,0.18");
                    p.Poly(Col.Shade(wb, 1.8f), "0.0,0.44 0.06,0.36 0.42,0.33 0.78,0.38 1.0,0.48 0.80,0.56 0.60,0.60 0.28,0.60 0.06,0.58", wb);
                    p.Poly(wb, "0.03,0.57 0.20,0.59 0.15,0.98 0.07,0.98");
                    p.Poly(Col.Shade(wb, 1.6f), "0.14,0.40 0.66,0.42 0.24,0.04", wb);
                    p.Poly(EnemyRed, "0.20,0.50 0.80,0.46 0.82,0.50 0.20,0.54");
                    p.Poly(EnemyRed, "0.07,0.80 0.16,0.80 0.15,0.94 0.08,0.94");
                    p.Ellipse(new Color(1f, 0.2f, 0.2f), 0.76f, 0.57f, 0.09f, 0.09f, new Color(0.4f, 0f, 0f));
                    p.Ellipse(glow, 0.02f, 0.47f, 0.02f, 0.07f);
                    break;
                case BossKind.BlackCitadel:
                    p = new Painter(448, 384) { Mirror = true };
                    var cb = new Color(0.14f, 0.13f, 0.16f);
                    p.Poly(Col.Shade(cb, 1.9f), "0.10,0.50 0.30,0.20 0.70,0.20 0.90,0.50 0.70,0.80 0.30,0.80", cb);
                    p.Poly(Col.Shade(cb, 2.4f), "0.20,0.50 0.36,0.30 0.64,0.30 0.80,0.50 0.64,0.70 0.36,0.70", Col.Shade(cb, 1.3f));
                    p.Poly(Col.Shade(cb, 1.5f), "0.0,0.46 0.12,0.42 0.12,0.58 0.0,0.54");
                    p.Poly(Col.Shade(cb, 1.5f), "0.88,0.42 1.0,0.46 1.0,0.54 0.88,0.58");
                    p.Poly(Col.Shade(cb, 1.6f), "0.40,0.80 0.60,0.80 0.56,0.98 0.44,0.98");
                    p.Poly(Col.Shade(cb, 1.6f), "0.40,0.20 0.60,0.20 0.56,0.02 0.44,0.02");
                    p.Ellipse(new Color(1f, 0.6f, 0.3f), 0.50f, 0.50f, 0.10f, 0.12f, glow);
                    p.Ellipse(new Color(1f, 0.95f, 0.8f), 0.50f, 0.50f, 0.04f, 0.05f);
                    p.Ellipse(Olive, 0.30f, 0.30f, 0.04f, 0.05f); p.Ellipse(Olive, 0.30f, 0.70f, 0.04f, 0.05f);
                    p.Ellipse(Olive, 0.70f, 0.30f, 0.04f, 0.05f); p.Ellipse(Olive, 0.70f, 0.70f, 0.04f, 0.05f);
                    p.Rect(EnemyRed, 0.36f, 0.49f, 0.40f, 0.51f); p.Rect(EnemyRed, 0.60f, 0.49f, 0.64f, 0.51f);
                    break;
            }
            p.Outline(Ink);
            return p.ToSprite(64f);
        });

        // ================================================================ projectiles & pickups
        public static Sprite PlayerShot() => Get("pshot", () =>
        {
            var p = new Painter(32, 8);
            p.Soft(new Color(1f, 0.85f, 0.3f, 0.6f), 0.5f, 0.5f, 0.5f, 0.5f, 0.2f);
            p.Ellipse(new Color(1f, 1f, 0.8f), 0.6f, 0.5f, 0.38f, 0.32f);
            return p.ToSprite(64f);
        });

        public static Sprite EnemyShot() => Get("eshot", () =>
        {
            var p = new Painter(20, 20);
            p.Soft(new Color(1f, 0.3f, 0.2f, 0.8f), 0.5f, 0.5f, 0.5f, 0.5f, 0.3f);
            p.Ellipse(new Color(1f, 0.9f, 0.7f), 0.5f, 0.5f, 0.22f, 0.22f);
            return p.ToSprite(64f);
        });

        public static Sprite Missile(bool enemy) => Get("missile" + enemy, () =>
        {
            var p = new Painter(48, 12);
            var body = enemy ? new Color(0.85f, 0.85f, 0.8f) : new Color(0.9f, 0.92f, 0.95f);
            p.Poly(body, "0.12,0.30 0.80,0.30 1.0,0.5 0.80,0.70 0.12,0.70", Col.Shade(body, 0.7f));
            p.Poly(Dark, "0.10,0.10 0.30,0.30 0.30,0.70 0.10,0.90");
            p.Rect(enemy ? EnemyRed : new Color(0.3f, 0.5f, 1f), 0.70f, 0.30f, 0.78f, 0.70f);
            p.Soft(new Color(1f, 0.7f, 0.2f, 0.9f), 0.06f, 0.5f, 0.07f, 0.4f, 0.2f);
            return p.ToSprite(64f);
        });

        public static Sprite Bomb() => Get("bomb", () =>
        {
            var p = new Painter(32, 14);
            p.Ellipse(new Color(0.4f, 0.42f, 0.36f), 0.55f, 0.5f, 0.42f, 0.36f, new Color(0.25f, 0.27f, 0.22f));
            p.Poly(Dark, "0.0,0.15 0.2,0.35 0.2,0.65 0.0,0.85");
            p.Rect(new Color(1f, 0.8f, 0.2f), 0.6f, 0.2f, 0.66f, 0.8f);
            return p.ToSprite(64f);
        });

        public static Sprite Rocket() => Get("rocket", () =>
        {
            var p = new Painter(36, 8);
            p.Poly(new Color(0.85f, 0.85f, 0.85f), "0.15,0.2 0.85,0.2 1.0,0.5 0.85,0.8 0.15,0.8");
            p.Soft(new Color(1f, 0.6f, 0.15f, 1f), 0.08f, 0.5f, 0.09f, 0.5f, 0.2f);
            return p.ToSprite(64f);
        });

        public static Sprite Coin() => Get("coin", () =>
        {
            var p = new Painter(28, 28);
            p.Ellipse(new Color(1f, 0.88f, 0.35f), 0.5f, 0.5f, 0.46f, 0.46f, new Color(0.85f, 0.55f, 0.1f));
            p.Ellipse(new Color(1f, 0.95f, 0.6f), 0.5f, 0.5f, 0.30f, 0.30f, new Color(0.95f, 0.7f, 0.2f));
            p.Rect(new Color(0.85f, 0.55f, 0.1f), 0.44f, 0.26f, 0.56f, 0.74f);
            p.Outline(new Color(0.4f, 0.25f, 0.05f, 0.9f));
            return p.ToSprite(64f);
        });

        /// <summary>Gun-power capsule (blue, with an up chevron).</summary>
        public static Sprite PowerUp() => Get("pow", () =>
        {
            var p = new Painter(44, 44);
            p.Soft(new Color(0.3f, 0.7f, 1f, 0.6f), 0.5f, 0.5f, 0.5f, 0.5f, 0.3f);
            p.Ellipse(new Color(0.5f, 0.85f, 1f), 0.5f, 0.5f, 0.32f, 0.32f, new Color(0.1f, 0.35f, 0.85f));
            p.Poly(Color.white, "0.30,0.40 0.50,0.66 0.70,0.40 0.62,0.36 0.50,0.52 0.38,0.36");
            p.Outline(new Color(0.05f, 0.1f, 0.3f, 0.9f));
            return p.ToSprite(64f);
        });

        public static Sprite Repair() => Get("repair", () =>
        {
            var p = new Painter(44, 44);
            p.Soft(new Color(0.3f, 1f, 0.5f, 0.6f), 0.5f, 0.5f, 0.5f, 0.5f, 0.3f);
            p.Ellipse(new Color(0.55f, 1f, 0.6f), 0.5f, 0.5f, 0.32f, 0.32f, new Color(0.1f, 0.6f, 0.25f));
            p.Rect(Color.white, 0.43f, 0.28f, 0.57f, 0.72f); p.Rect(Color.white, 0.28f, 0.43f, 0.72f, 0.57f);
            p.Outline(new Color(0.05f, 0.25f, 0.1f, 0.9f));
            return p.ToSprite(64f);
        });

        // ================================================================ effects
        /// <summary>Soft white circle; tinted for explosions, smoke, glows and the shield.</summary>
        public static Sprite Glow() => Get("glow", () =>
        {
            var p = new Painter(64, 64);
            p.Soft(Color.white, 0.5f, 0.5f, 0.5f, 0.5f, 0.0f);
            return p.ToSprite(64f);
        });

        public static Sprite Puff() => Get("puff", () =>
        {
            var p = new Painter(64, 64);
            p.Soft(Color.white, 0.5f, 0.5f, 0.5f, 0.5f, 0.55f);
            return p.ToSprite(64f);
        });

        public static Sprite Ring() => Get("ring", () =>
        {
            var p = new Painter(128, 128);
            p.Soft(new Color(0.5f, 0.85f, 1f, 0.35f), 0.5f, 0.5f, 0.5f, 0.5f, 0.75f);
            p.Ellipse(new Color(0f, 0f, 0f, 0f), 0.5f, 0.5f, 0.1f, 0.1f);
            return p.ToSprite(64f);
        });

        public static Sprite Flame() => Get("flame", () =>
        {
            var p = new Painter(40, 16);
            p.Soft(new Color(1f, 0.45f, 0.1f, 0.9f), 0.62f, 0.5f, 0.38f, 0.45f, 0.1f);
            p.Soft(new Color(1f, 0.95f, 0.7f, 1f), 0.80f, 0.5f, 0.20f, 0.26f, 0.3f);
            return p.ToSprite(PPU, new Vector2(1f, 0.5f));
        });

        public static Sprite Star() => Get("star", () =>
        {
            var p = new Painter(64, 64);
            p.Poly(Color.white, "0.5,0.97 0.62,0.64 0.97,0.64 0.69,0.42 0.80,0.07 0.5,0.28 0.20,0.07 0.31,0.42 0.03,0.64 0.38,0.64");
            return p.ToSprite(64f);
        });

        public static Sprite Pixel() => Get("pixel", () =>
        {
            var p = new Painter(4, 4);
            p.Rect(Color.white, 0, 0, 1, 1);
            return p.ToSprite(4f, null, false);
        });

        // ================================================================ backgrounds
        public static Sprite Cloud(int variant, bool dark) => Get("cloud" + variant + dark, () =>
        {
            var p = new Painter(256, 112);
            var rng = new System.Random(variant * 977 + 3);
            var c = dark ? new Color(0.55f, 0.57f, 0.65f, 0.9f) : new Color(1f, 1f, 1f, 0.92f);
            var sh = dark ? new Color(0.35f, 0.36f, 0.43f, 0.9f) : new Color(0.82f, 0.86f, 0.93f, 0.9f);
            int n = 6 + rng.Next(4);
            for (int i = 0; i < n; i++)
            {
                float x = 0.18f + (float)rng.NextDouble() * 0.64f, y = 0.35f + (float)rng.NextDouble() * 0.2f;
                float r = 0.12f + (float)rng.NextDouble() * 0.10f;
                p.Soft(sh, x, y - 0.06f, r, r * 2.1f, 0.55f);
            }
            for (int i = 0; i < n; i++)
            {
                float x = 0.18f + (float)rng.NextDouble() * 0.64f, y = 0.42f + (float)rng.NextDouble() * 0.2f;
                float r = 0.10f + (float)rng.NextDouble() * 0.10f;
                p.Soft(c, x, y, r, r * 2.1f, 0.55f);
            }
            return p.ToSprite(64f);
        });

        public static Sprite Sky(Color top, Color bottom) => Get("sky" + top + bottom, () =>
        {
            var p = new Painter(4, 128);
            for (int y = 0; y < 128; y++)
            {
                var c = Color.Lerp(bottom, top, y / 127f);
                for (int x = 0; x < 4; x++) p.Set(x, y, c);
            }
            return p.ToSprite(4f);
        });

        /// <summary>Distant silhouette strip (mountains, skyline, dunes, ice, cliffs) that tiles horizontally.</summary>
        public static Sprite Far(Terrain t, Color c) => Get("far" + t + c, () =>
        {
            const int W = 512, H = 160;
            var p = new Painter(W, H);
            var rng = new System.Random((int)t * 131 + 7);
            float[] h = new float[W];
            switch (t)
            {
                case Terrain.Fortress: // city / industrial skyline
                case Terrain.Grassland:
                {
                    int x = 0;
                    while (x < W)
                    {
                        int bw = 14 + rng.Next(36); float bh = (t == Terrain.Grassland ? 0.25f : 0.35f) + (float)rng.NextDouble() * 0.45f;
                        if (t == Terrain.Grassland && rng.NextDouble() < 0.5) bh *= 0.45f; // farms & trees between towns
                        for (int i = 0; i < bw && x < W; i++, x++) h[x] = bh;
                    }
                    break;
                }
                default:
                {
                    // smooth periodic noise from a few sine waves (whole-number frequencies so it tiles)
                    float a1 = 0.18f + (float)rng.NextDouble() * 0.1f, a2 = 0.08f + (float)rng.NextDouble() * 0.06f, a3 = 0.04f;
                    float p1 = (float)rng.NextDouble() * 6.28f, p2 = (float)rng.NextDouble() * 6.28f, p3 = (float)rng.NextDouble() * 6.28f;
                    float baseH = t == Terrain.Ocean ? 0.18f : t == Terrain.Desert ? 0.30f : t == Terrain.Storm ? 0.35f : 0.45f;
                    for (int x = 0; x < W; x++)
                    {
                        float u = x / (float)W * Mathf.PI * 2f;
                        float v = baseH + a1 * Mathf.Sin(u * 2 + p1) + a2 * Mathf.Sin(u * 5 + p2) + a3 * Mathf.Sin(u * 11 + p3);
                        if (t == Terrain.Mountains || t == Terrain.Canyon) v += 0.10f * Mathf.Abs(Mathf.Sin(u * 7 + p2));
                        if (t == Terrain.Ocean) v *= 0.5f;
                        h[x] = Mathf.Clamp(v, 0.05f, 0.95f);
                    }
                    break;
                }
            }
            for (int x = 0; x < W; x++)
            {
                int top = Mathf.RoundToInt(h[x] * H);
                for (int y = 0; y < top; y++)
                {
                    var col = Color.Lerp(c, Col.Shade(c, 0.75f), 1f - y / (float)H);
                    if (t == Terrain.Fortress && rng.NextDouble() < 0.012) col = new Color(1f, 0.6f, 0.25f);
                    if (t == Terrain.Mountains && y > top - 6 && h[x] > 0.55f) col = Color.Lerp(col, Color.white, 0.7f);
                    p.Set(x, y, col);
                }
                if (top < H) p.BlendPx(x, top, c, h[x] * H - top + 0.5f);
            }
            var tex = p.ToTexture(true, TextureWrapMode.Repeat);
            return Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0f, 0f), 64f, 0, SpriteMeshType.FullRect);
        });

        /// <summary>Ground or sea strip that tiles horizontally (top edge at the pivot's top).</summary>
        public static Sprite GroundStrip(MissionDef m) => Get("strip" + m.terrain, () =>
        {
            const int W = 256, H = 96;
            var p = new Painter(W, H);
            var rng = new System.Random((int)m.terrain * 71 + 1);
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float t = y / (float)(H - 1);
                    Color c = Color.Lerp(m.ground2, m.ground1, t);
                    if (m.sea)
                    {
                        float wave = Mathf.Sin(x / (float)W * Mathf.PI * 8f + y * 0.45f) * 0.5f + 0.5f;
                        c = Color.Lerp(c, Col.Shade(m.ground1, 1.25f), wave * 0.25f * t);
                        if (y > H - 4) c = Color.Lerp(c, Color.white, 0.55f);
                    }
                    else
                    {
                        float n = (float)rng.NextDouble();
                        c = Col.Shade(c, 0.92f + n * 0.12f);
                        if (y > H - 5) c = Col.Shade(m.ground1, 1.18f);
                        // stripes: fields / dune ridges / rock strata, periodic so the strip tiles
                        float s = Mathf.Sin(x / (float)W * Mathf.PI * 6f + y * 0.15f);
                        if (s > 0.85f) c = Col.Shade(c, 0.88f);
                    }
                    p.Set(x, y, c);
                }
            var tex = p.ToTexture(true, TextureWrapMode.Repeat);
            return Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0f, 1f), 64f, 0, SpriteMeshType.FullRect);
        });

        // ================================================================ UI
        public static Sprite RoundedBox() => Get("uibox", () =>
        {
            const int S = 64; var p = new Painter(S, S);
            float r = 14f;
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float dx = Mathf.Max(0, Mathf.Max(r - (x + 0.5f), (x + 0.5f) - (S - r)));
                    float dy = Mathf.Max(0, Mathf.Max(r - (y + 0.5f), (y + 0.5f) - (S - r)));
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(r - d + 0.5f);
                    if (a > 0) p.Set(x, y, new Color(1, 1, 1, a));
                }
            var tex = p.ToTexture(true);
            return Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(20, 20, 20, 20));
        });

        /// <summary>Generic emblem for each special weapon (shown on the button and in the armory).</summary>
        public static Sprite SpecialIcon(int special) => Get("spicon" + special, () =>
        {
            var p = new Painter(64, 64);
            var w = Color.white;
            switch (Specials.All[special].kind)
            {
                case SpecialKind.ClusterBombs: for (int i = 0; i < 3; i++) p.Ellipse(w, 0.28f + i * 0.22f, 0.6f - i * 0.12f, 0.09f, 0.14f); break;
                case SpecialKind.SeekerMissiles: p.Poly(w, "0.15,0.40 0.70,0.40 0.90,0.50 0.70,0.60 0.15,0.60"); p.Poly(w, "0.10,0.30 0.25,0.40 0.25,0.60 0.10,0.70"); p.Ellipse(w, 0.80f, 0.80f, 0.08f, 0.08f); break;
                case SpecialKind.RocketSalvo: for (int i = 0; i < 3; i++) p.Poly(w, "0.15," + (0.22f + i * 0.22f).ToString(System.Globalization.CultureInfo.InvariantCulture) + " 0.75," + (0.22f + i * 0.22f).ToString(System.Globalization.CultureInfo.InvariantCulture) + " 0.88," + (0.28f + i * 0.22f).ToString(System.Globalization.CultureInfo.InvariantCulture) + " 0.75," + (0.34f + i * 0.22f).ToString(System.Globalization.CultureInfo.InvariantCulture) + " 0.15," + (0.34f + i * 0.22f).ToString(System.Globalization.CultureInfo.InvariantCulture)); break;
                case SpecialKind.Napalm: p.Poly(w, "0.5,0.92 0.75,0.55 0.70,0.25 0.5,0.12 0.30,0.25 0.25,0.55"); break;
                case SpecialKind.RailLance: p.Rect(w, 0.08f, 0.44f, 0.92f, 0.56f); p.Poly(w, "0.75,0.30 0.92,0.50 0.75,0.70"); break;
                case SpecialKind.AegisShield: p.Poly(w, "0.5,0.92 0.85,0.78 0.80,0.40 0.5,0.10 0.20,0.40 0.15,0.78"); break;
                case SpecialKind.SwarmDrones: for (int i = 0; i < 3; i++) p.Poly(w, F3(0.2f + i * 0.0f, 0.2f + i * 0.28f)); break;
                case SpecialKind.Thermobaric: p.Ellipse(w, 0.5f, 0.6f, 0.30f, 0.24f); p.Rect(w, 0.42f, 0.10f, 0.58f, 0.45f); break;
            }
            return p.ToSprite(64f);
        });

        static string F3(float x, float y)
        {
            var ci = System.Globalization.CultureInfo.InvariantCulture;
            return x.ToString(ci) + "," + y.ToString(ci) + " " + (x + 0.6f).ToString(ci) + "," + (y + 0.08f).ToString(ci) + " " + x.ToString(ci) + "," + (y + 0.16f).ToString(ci);
        }

        /// <summary>Icon per module slot (UI).</summary>
        public static Sprite SlotIcon(int slot) => Get("sloticon" + slot, () =>
        {
            var p = new Painter(64, 64); var w = Color.white;
            switch ((ModuleSlot)slot)
            {
                case ModuleSlot.Gun: p.Rect(w, 0.10f, 0.42f, 0.90f, 0.58f); p.Rect(w, 0.10f, 0.25f, 0.35f, 0.58f); break;
                case ModuleSlot.Engine: p.Ellipse(w, 0.5f, 0.5f, 0.36f, 0.36f); p.Ellipse(new Color(0, 0, 0, 0.6f), 0.5f, 0.5f, 0.16f, 0.16f); break;
                case ModuleSlot.Armor: p.Poly(w, "0.5,0.92 0.85,0.78 0.80,0.40 0.5,0.10 0.20,0.40 0.15,0.78"); break;
                case ModuleSlot.Avionics: p.Ellipse(w, 0.5f, 0.5f, 0.38f, 0.38f); p.Ellipse(new Color(0, 0, 0, 0.6f), 0.5f, 0.5f, 0.26f, 0.26f); p.Rect(w, 0.47f, 0.10f, 0.53f, 0.90f); p.Rect(w, 0.10f, 0.47f, 0.90f, 0.53f); break;
                case ModuleSlot.Warhead: p.Poly(w, "0.10,0.40 0.65,0.40 0.92,0.50 0.65,0.60 0.10,0.60"); break;
                case ModuleSlot.Drone: p.Poly(w, "0.08,0.50 0.30,0.30 0.85,0.40 0.95,0.50 0.85,0.60 0.30,0.70"); break;
            }
            return p.ToSprite(64f);
        });
    }
}
