using System.Collections.Generic;
using UnityEngine;

namespace NatoSquad
{
    /// <summary>Recycles sprite GameObjects (bullets, coins, effects) so the battle doesn't allocate every frame.</summary>
    public class SpritePool
    {
        readonly Transform parent;
        readonly Stack<SpriteRenderer> free = new Stack<SpriteRenderer>();

        public SpritePool(Transform parent) { this.parent = parent; }

        public SpriteRenderer Get(Sprite s, int order, Color? tint = null)
        {
            SpriteRenderer sr;
            if (free.Count > 0) { sr = free.Pop(); sr.gameObject.SetActive(true); }
            else
            {
                var go = new GameObject("p");
                go.transform.SetParent(parent, false);
                sr = go.AddComponent<SpriteRenderer>();
            }
            sr.sprite = s;
            sr.sortingOrder = order;
            sr.color = tint ?? Color.white;
            sr.flipX = false;
            sr.transform.localScale = Vector3.one;
            sr.transform.rotation = Quaternion.identity;
            return sr;
        }

        public void Release(SpriteRenderer sr)
        {
            if (sr == null) return;
            sr.gameObject.SetActive(false);
            free.Push(sr);
        }
    }

    public enum ShotKind { Bullet, Missile, Bomb, Rocket, Flak, Kamikaze, Napalm }

    public class Shot
    {
        public SpriteRenderer sr;
        public Vector2 pos, vel;
        public float damage, radius = 0.12f, life = 4f, turn, speed, hp;
        public bool fromPlayer, special, faceVelocity = true, dead;
        public ShotKind kind;
        public Enemy target;
        public float aoe;          // explosion radius (bombs, missiles, kamikaze)
        public float tick;         // napalm damage timer
        public int pierce;         // rail rounds / thermobaric
    }

    public enum PickupKind { Coin, Power, Repair }

    public class Pickup
    {
        public SpriteRenderer sr;
        public Vector2 pos, vel;
        public PickupKind kind;
        public long value;
        public float life = 12f, t;
        public bool dead;
    }

    public class Enemy
    {
        public EnemyKind kind;
        public GameObject go;
        public SpriteRenderer sr;
        public Transform barrel, rotor;
        public Vector2 pos, vel, half;   // half = collision half-extents
        public float hp, maxHp, t, fireT, flash, anchorY, phase, speed, stateT;
        public int state, shotsLeft;
        public bool ground, dead, entered;
        public long gold;
        public int score;
        // boss
        public bool isBoss;
        public BossKind boss;
        public int attackIndex;
        public float attackT;
        public Vector2 target;

        public bool Contains(Vector2 p, float pad = 0f) =>
            Mathf.Abs(p.x - pos.x) < half.x + pad && Mathf.Abs(p.y - pos.y) < half.y + pad;
    }

    /// <summary>Short-lived visual effects: explosions, smoke, sparks, flashes.</summary>
    public class Fx
    {
        class P
        {
            public SpriteRenderer sr;
            public Vector2 pos, vel;
            public float life, max, size0, size1, spin;
            public Color c0, c1;
            public bool scrollWithGround;
        }

        readonly SpritePool pool;
        readonly List<P> live = new List<P>();
        public float GroundScroll;

        public Fx(SpritePool pool) { this.pool = pool; }

        void Add(Sprite s, Vector2 pos, Vector2 vel, float life, float s0, float s1, Color c0, Color c1, int order, float spin = 0f)
        {
            var sr = pool.Get(s, order, c0);
            live.Add(new P { sr = sr, pos = pos, vel = vel, life = life, max = life, size0 = s0, size1 = s1, c0 = c0, c1 = c1, spin = spin });
            sr.transform.position = pos;
            sr.transform.localScale = Vector3.one * s0;
        }

        public void Explosion(Vector2 at, float size)
        {
            Add(Art.Glow(), at, Vector2.zero, 0.25f, size * 1.2f, size * 3.2f, new Color(1f, 1f, 0.85f, 1f), new Color(1f, 0.6f, 0.2f, 0f), 60);
            int n = Mathf.Clamp(Mathf.RoundToInt(5 * size), 4, 18);
            for (int i = 0; i < n; i++)
            {
                var dir = Random.insideUnitCircle;
                Add(Art.Puff(), at + dir * 0.2f * size, dir * Random.Range(0.6f, 2.2f) * size, Random.Range(0.35f, 0.7f),
                    size * Random.Range(0.6f, 1.0f), size * Random.Range(1.4f, 2.2f),
                    new Color(1f, Random.Range(0.55f, 0.85f), 0.2f, 1f), new Color(0.25f, 0.22f, 0.2f, 0f), 58);
            }
            for (int i = 0; i < n; i++)
            {
                var dir = Random.insideUnitCircle.normalized;
                Add(Art.Pixel(), at, dir * Random.Range(3f, 7f) * Mathf.Sqrt(size), Random.Range(0.25f, 0.5f), 0.12f, 0.04f,
                    new Color(1f, 0.9f, 0.5f, 1f), new Color(1f, 0.4f, 0.1f, 0f), 61);
            }
        }

        public void Smoke(Vector2 at, float size = 0.5f)
        {
            Add(Art.Puff(), at, new Vector2(-1.2f, 0.4f), 0.8f, size, size * 2.2f, new Color(0.35f, 0.35f, 0.38f, 0.6f), new Color(0.3f, 0.3f, 0.3f, 0f), 20);
        }

        public void Spark(Vector2 at, Color c)
        {
            Add(Art.Glow(), at, Vector2.zero, 0.08f, 0.25f, 0.55f, c, Col.A(c, 0f), 62);
        }

        public void Muzzle(Vector2 at)
        {
            Add(Art.Glow(), at, Vector2.zero, 0.05f, 0.25f, 0.35f, new Color(1f, 0.9f, 0.5f, 0.9f), new Color(1f, 0.7f, 0.2f, 0f), 41);
        }

        public void Fire(Vector2 at)
        {
            Add(Art.Puff(), at + new Vector2(Random.Range(-0.1f, 0.1f), 0), new Vector2(-0.5f, Random.Range(0.8f, 1.6f)), Random.Range(0.3f, 0.5f), 0.35f, 0.8f,
                new Color(1f, 0.75f, 0.2f, 0.95f), new Color(0.6f, 0.15f, 0.05f, 0f), 30);
        }

        public void Ring(Vector2 at, float size, Color c)
        {
            Add(Art.Ring(), at, Vector2.zero, 0.4f, size * 0.3f, size, c, Col.A(c, 0f), 63);
        }

        public void Update(float dt)
        {
            for (int i = live.Count - 1; i >= 0; i--)
            {
                var p = live[i];
                p.life -= dt;
                if (p.life <= 0f) { pool.Release(p.sr); live.RemoveAt(i); continue; }
                float k = 1f - p.life / p.max;
                p.pos += p.vel * dt;
                p.vel *= 1f - 2.5f * dt;
                p.sr.transform.position = p.pos;
                p.sr.transform.localScale = Vector3.one * Mathf.Lerp(p.size0, p.size1, k);
                p.sr.color = Color.Lerp(p.c0, p.c1, k);
            }
        }

        public void Clear()
        {
            foreach (var p in live) pool.Release(p.sr);
            live.Clear();
        }
    }
}
