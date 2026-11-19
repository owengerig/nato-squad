using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace NatoSquad
{
    public class BattleResult
    {
        public int mission;
        public bool won, aborted, firstClear;
        public int stars, kills;
        public long goldCollected, missionReward, bossBonus;
        public long Total => goldCollected + missionReward + bossBonus;
        public float hullLost;     // fraction of max hull lost over the sortie
        public float time;
    }

    /// <summary>
    /// One sortie: side-scrolling, left to right. The jet fires automatically; drag to fly; the special-weapon button
    /// fires the equipped special. Waves of air and ground enemies until the mission length is reached, then the boss.
    /// All entities are plain objects updated here (no physics) for speed and simplicity.
    /// </summary>
    public class Battle : MonoBehaviour
    {
        // ------------------------------------------------------------------ setup
        GameManager G;
        public MissionDef M { get; private set; }
        public int MissionIndex { get; private set; }
        StatBlock stats;
        JetDef jet;
        int jetIndex;
        BattleScreen hud;
        float diff;
        Transform world;
        SpritePool pool;
        Fx fx;
        Backdrop backdrop;

        float halfW, halfH, groundTop;
        const float Scroll = 3.2f;

        public enum Phase { Intro, Waves, Warning, Boss, BossDown, Victory, Dead, Done }
        public Phase State { get; private set; }
        float phaseT, elapsed;

        // player
        Transform player;
        SpriteRenderer playerSr, flameSr, shieldSr, beamSr, droneSr;
        Vector2 ppos, dronePos;
        float hull, maxHull, invuln, regenDelay, fireT, droneFireT, shieldT, beamT, beamTick, speedMult;
        public int Power { get; private set; }
        public const int MaxPower = 8;

        // world objects
        readonly List<Enemy> enemies = new List<Enemy>();
        readonly List<Shot> shots = new List<Shot>();
        readonly List<Pickup> pickups = new List<Pickup>();
        readonly List<Shot> enemyMissiles = new List<Shot>();
        class Pending { public float t; public Action a; }
        readonly List<Pending> queue = new List<Pending>();
        float spawnT;
        Enemy boss;

        // sortie stats
        long goldCollected;
        int kills, killsSinceDrop;
        float damageTaken, shake;

        // special
        int specialIndex = -1;
        float specialCd;

        // input
        int dragTouch = -1;
        Vector2 lastDrag;
        bool mouseDrag;

        // ------------------------------------------------------------------ public read-outs for the HUD
        public float Hull01 => maxHull > 0 ? Mathf.Clamp01(hull / maxHull) : 0f;
        public bool Danger => Hull01 < 0.3f && State != Phase.Dead;
        public long Gold => goldCollected;
        public float Progress01 => Mathf.Clamp01(elapsed / M.length);
        public float BossHp01 => boss != null ? Mathf.Clamp01(boss.hp / boss.maxHp) : 0f;
        public bool BossActive => boss != null && !boss.dead && (State == Phase.Boss);
        public int SpecialIndex => specialIndex;
        public int SpecialAmmo => specialIndex >= 0 ? G.Save.Ammo(specialIndex) : 0;
        public float SpecialCd01 => specialIndex >= 0 ? Mathf.Clamp01(specialCd / Specials.All[specialIndex].cooldown) : 0f;
        public bool ShieldOn => shieldT > 0f;

        public void Init(int mission, BattleScreen screen)
        {
            G = GameManager.I;
            hud = screen;
            MissionIndex = mission;
            M = Missions.All[mission];
            diff = Missions.Difficulty(mission);
            stats = Loadout.Compute(G.Save);
            jetIndex = G.Save.selectedJet;
            jet = Jets.All[jetIndex];

            world = new GameObject("World").transform;
            world.SetParent(transform, false);
            pool = new SpritePool(world);
            fx = new Fx(pool);
            backdrop = new Backdrop(world, M);
            UpdateView();

            // player jet
            player = new GameObject("Player").transform;
            player.SetParent(world, false);
            playerSr = player.gameObject.AddComponent<SpriteRenderer>();
            playerSr.sprite = Art.Jet(jetIndex);
            playerSr.sortingOrder = 50;
            var flame = new GameObject("Flame");
            flame.transform.SetParent(player, false);
            flame.transform.localPosition = new Vector3(-0.78f, -0.02f, 0);
            flameSr = flame.AddComponent<SpriteRenderer>();
            flameSr.sprite = Art.Flame();
            flameSr.sortingOrder = 49;
            var shield = new GameObject("Shield");
            shield.transform.SetParent(player, false);
            shieldSr = shield.AddComponent<SpriteRenderer>();
            shieldSr.sprite = Art.Ring();
            shieldSr.sortingOrder = 52;
            shieldSr.transform.localScale = Vector3.one * 1.1f;
            shieldSr.enabled = false;
            var beam = new GameObject("Beam");
            beam.transform.SetParent(world, false);
            beamSr = beam.AddComponent<SpriteRenderer>();
            beamSr.sprite = Art.Glow();
            beamSr.sortingOrder = 48;
            beamSr.enabled = false;

            maxHull = jet.hull * stats.Mult(Stat.MaxHull);
            hull = maxHull;
            speedMult = stats.Mult(Stat.Speed);
            Power = Mathf.Clamp(1 + Mathf.FloorToInt(stats[Stat.PowerStart] + 0.0001f), 1, MaxPower);
            ppos = new Vector2(-halfW - 1.5f, 0.8f);

            if (stats[Stat.DroneDamage] > 0f)
            {
                var d = new GameObject("Wingman Drone");
                d.transform.SetParent(world, false);
                droneSr = d.AddComponent<SpriteRenderer>();
                droneSr.sprite = Art.Enemy(EnemyKind.Drone);
                droneSr.flipX = true;
                droneSr.color = new Color(0.6f, 0.85f, 1f);
                droneSr.sortingOrder = 49;
                dronePos = ppos;
            }

            specialIndex = G.Save.equippedSpecial;
            if (specialIndex >= Specials.All.Length) specialIndex = -1;

            State = Phase.Intro;
            phaseT = 0f;
            spawnT = 1.5f;
            hud.Banner(M.code + "  ·  " + M.name.ToUpper(), "Drag to fly · Guns fire automatically", 2.6f, Pal.Text);
            G.Audio.PlayMusic(GameAudio.Track.Battle);
        }

        void OnDestroy()
        {
            if (G != null && G.Cam != null) G.Cam.transform.position = new Vector3(0, 0, -10);
        }

        void UpdateView()
        {
            var cam = G.Cam;
            halfH = cam.orthographicSize;
            halfW = halfH * cam.aspect;
            backdrop.Layout(halfW, halfH);
            groundTop = backdrop.GroundTop;
        }

        // ------------------------------------------------------------------ main loop
        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            dt = Mathf.Min(dt, 1f / 20f);
            UpdateView();
            elapsed += State == Phase.Waves ? dt : 0f;
            phaseT += dt;

            bool scrolling = State != Phase.Done;
            backdrop.Update(dt, halfW, scrolling ? Scroll : 0f);

            UpdatePhase(dt);
            UpdatePlayer(dt);
            UpdateQueue(dt);
            UpdateEnemies(dt);
            UpdateShots(dt);
            UpdatePickups(dt);
            fx.Update(dt);
            UpdateShake(dt);
        }

        void UpdatePhase(float dt)
        {
            switch (State)
            {
                case Phase.Intro:
                    if (phaseT > 1.6f) { State = Phase.Waves; phaseT = 0f; }
                    break;
                case Phase.Waves:
                    spawnT -= dt;
                    if (elapsed < M.length && spawnT <= 0f) SpawnFormation();
                    if (elapsed >= M.length && (CountAlive() == 0 || phaseT > 0f && elapsed >= M.length + 6f))
                    {
                        State = Phase.Warning; phaseT = 0f;
                        hud.Banner("WARNING", M.bossName + " APPROACHING", 3f, Pal.Red, true);
                        G.Audio.Play(Sfx.Warning);
                        G.Audio.PlayMusic(GameAudio.Track.Boss);
                    }
                    break;
                case Phase.Warning:
                    if (phaseT > 3f) { State = Phase.Boss; phaseT = 0f; SpawnBoss(); }
                    break;
                case Phase.Boss:
                    break;
                case Phase.BossDown:
                    if (phaseT > 3f)
                    {
                        State = Phase.Victory; phaseT = 0f;
                        hud.Banner("MISSION COMPLETE", M.code + " · " + M.name, 3f, Pal.Gold);
                        G.Audio.Play(Sfx.Victory);
                        G.Audio.PlayMusic(GameAudio.Track.None);
                    }
                    break;
                case Phase.Victory:
                    if (phaseT > 3f) Finish(true, false);
                    break;
                case Phase.Dead:
                    if (phaseT > 2.4f) Finish(false, false);
                    break;
            }
        }

        int CountAlive()
        {
            int n = 0;
            foreach (var e in enemies) if (!e.dead) n++;
            return n;
        }

        void Queue(float delay, Action a) => queue.Add(new Pending { t = delay, a = a });

        void UpdateQueue(float dt)
        {
            for (int i = queue.Count - 1; i >= 0; i--)
            {
                queue[i].t -= dt;
                if (queue[i].t <= 0f)
                {
                    var a = queue[i].a;
                    queue.RemoveAt(i);
                    a();
                }
            }
        }

        // ------------------------------------------------------------------ player
        bool Alive => State != Phase.Dead && State != Phase.Done;

        void UpdatePlayer(float dt)
        {
            if (!Alive) return;

            if (State == Phase.Intro)
                ppos = Vector2.MoveTowards(ppos, new Vector2(-halfW * 0.55f, 0.8f), 10f * dt);
            else if (State == Phase.Victory)
                ppos += new Vector2(4f + phaseT * 8f, 0.6f) * dt;
            else
                ReadInput(dt);

            float minX = -halfW + 0.8f, maxX = halfW - 0.8f, minY = groundTop + 0.35f, maxY = halfH - 0.9f;
            if (State != Phase.Intro && State != Phase.Victory)
                ppos = new Vector2(Mathf.Clamp(ppos.x, minX, maxX), Mathf.Clamp(ppos.y, minY, maxY));

            player.position = ppos;
            // bank slightly with vertical movement
            float tilt = Mathf.Clamp((ppos.y - lastY) / Mathf.Max(dt, 0.001f) * 1.5f, -10f, 10f);
            lastY = ppos.y;
            player.rotation = Quaternion.Lerp(player.rotation, Quaternion.Euler(0, 0, tilt), 10f * dt);
            flameSr.transform.localScale = new Vector3(Random.Range(0.8f, 1.25f) * (State == Phase.Victory ? 1.8f : 1f), Random.Range(0.85f, 1.1f), 1);

            invuln -= dt;
            playerSr.enabled = invuln <= 0f || Mathf.Repeat(Time.time * 18f, 1f) > 0.35f;

            // hull repair after a short time without being hit
            regenDelay -= dt;
            if (regenDelay <= 0f && hull < maxHull)
                hull = Mathf.Min(maxHull, hull + jet.regen * stats.Mult(Stat.Regen) * (maxHull / 100f) * dt);
            if (Danger && Random.value < dt * 6f) fx.Smoke(ppos + new Vector2(-0.4f, 0.1f), 0.35f);

            // shield
            shieldT -= dt;
            shieldSr.enabled = shieldT > 0f;
            if (shieldT > 0f) shieldSr.color = new Color(1, 1, 1, shieldT < 1f ? Mathf.Repeat(Time.time * 8f, 1f) : 1f);

            // guns fire automatically while there's something to fight (not during the intro / outro)
            if (State == Phase.Waves || State == Phase.Warning || State == Phase.Boss || State == Phase.BossDown)
            {
                fireT -= dt * stats.Mult(Stat.FireRate);
                if (fireT <= 0f) { FireGuns(); fireT += jet.fireInterval; if (fireT < 0f) fireT = 0f; }
                UpdateDrone(dt);
                UpdateBeam(dt);
                specialCd -= dt;
            }
        }
        float lastY;

        void ReadInput(float dt)
        {
            float worldPerPixel = halfH * 2f / Mathf.Max(1, Screen.height);
            float sens = G.Settings.sensitivity * speedMult;
            Vector2 move = Vector2.zero;

            // touch: relative drag with any finger that didn't start on a HUD button
            var ts = UnityEngine.InputSystem.Touchscreen.current;
            if (ts != null)
            {
                bool found = false;
                foreach (var t in ts.touches)
                {
                    if (!t.press.isPressed) continue;
                    int id = t.touchId.ReadValue();
                    var pos = t.position.ReadValue();
                    if (dragTouch == id)
                    {
                        move += (pos - lastDrag) * worldPerPixel * sens;
                        lastDrag = pos;
                        found = true;
                    }
                    else if (dragTouch < 0 && !hud.Blocks(t.startPosition.ReadValue()))
                    {
                        dragTouch = id; lastDrag = pos; found = true;
                    }
                }
                if (!found) dragTouch = -1;
            }

            // mouse (editor / desktop): drag like a finger
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse != null && dragTouch < 0)
            {
                var mp = mouse.position.ReadValue();
                if (mouse.leftButton.wasPressedThisFrame && !hud.Blocks(mp)) { mouseDrag = true; lastDrag = mp; }
                if (!mouse.leftButton.isPressed) mouseDrag = false;
                if (mouseDrag) { move += (mp - lastDrag) * worldPerPixel * sens; lastDrag = mp; }
            }

            // keyboard
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null)
            {
                Vector2 k = Vector2.zero;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) k.x -= 1;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) k.x += 1;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) k.y -= 1;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) k.y += 1;
                if (k != Vector2.zero) move += k.normalized * jet.speed * speedMult * dt;
                if (kb.spaceKey.wasPressedThisFrame || kb.kKey.wasPressedThisFrame) TrySpecial();
            }

            // a touch drag is limited to the jet's top speed (heavier jets feel heavier)
            float maxStep = jet.speed * speedMult * 2.2f * dt;
            if (move.magnitude > maxStep) move = move.normalized * maxStep;
            ppos += move;
        }

        void FireGuns()
        {
            float dmg = jet.damage * stats.Mult(Stat.Damage) * (1f + 0.07f * (Power - 1));
            var muzzle = ppos + new Vector2(0.75f, -0.02f);
            fx.Muzzle(muzzle);
            switch (jet.gun)
            {
                case GunPattern.Spread:
                {
                    int n = Mathf.Min(5, 1 + Power / 2);
                    for (int i = 0; i < n; i++)
                    {
                        float a = (i - (n - 1) * 0.5f) * 6f * Mathf.Deg2Rad;
                        PlayerBullet(muzzle, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 18f, dmg);
                    }
                    break;
                }
                case GunPattern.Stream:
                {
                    int rows = Power >= 6 ? 4 : Power >= 4 ? 3 : Power >= 2 ? 2 : 1;
                    for (int i = 0; i < rows; i++)
                        PlayerBullet(muzzle + new Vector2(0, (i - (rows - 1) * 0.5f) * 0.14f), new Vector2(21f, 0), dmg);
                    break;
                }
                case GunPattern.ForwardAndDown:
                {
                    int rows = Power >= 7 ? 3 : Power >= 4 ? 2 : 1;
                    for (int i = 0; i < rows; i++)
                        PlayerBullet(muzzle + new Vector2(0, (i - (rows - 1) * 0.5f) * 0.14f), new Vector2(17f, 0), dmg);
                    int down = Power >= 5 ? 2 : 1;
                    var belly = ppos + new Vector2(0.2f, -0.18f);
                    for (int i = 0; i < down; i++)
                        PlayerBullet(belly + new Vector2(i * 0.18f, 0), new Vector2(1f, -1f).normalized * 15f, dmg * 0.9f);
                    break;
                }
            }
            G.Audio.Play(Sfx.Shot, 0.25f);
        }

        Shot PlayerBullet(Vector2 at, Vector2 vel, float dmg)
        {
            var s = NewShot(Art.PlayerShot(), at, vel, dmg, true, ShotKind.Bullet, 45);
            s.radius = 0.12f;
            return s;
        }

        void UpdateDrone(float dt)
        {
            if (droneSr == null) return;
            var target = ppos + new Vector2(-0.6f, 0.75f + Mathf.Sin(Time.time * 3f) * 0.1f);
            dronePos = Vector2.Lerp(dronePos, target, 8f * dt);
            droneSr.transform.position = dronePos;
            droneFireT -= dt;
            if (droneFireT <= 0f)
            {
                droneFireT = 0.33f;
                var e = Nearest(dronePos, 12f, false);
                var dir = e != null ? (e.pos - dronePos).normalized : Vector2.right;
                if (dir.x < 0.2f) dir = Vector2.right;
                var s = NewShot(Art.PlayerShot(), dronePos + dir * 0.3f, dir * 16f, stats[Stat.DroneDamage], true, ShotKind.Bullet, 45);
                s.sr.color = new Color(0.6f, 0.9f, 1f);
            }
        }

        void HurtPlayer(float dmg)
        {
            if (!Alive || State == Phase.Victory || State == Phase.Intro) return;
            if (invuln > 0f || shieldT > 0f) return;
            dmg *= 1f - stats[Stat.DamageReduction] / 100f;
            hull -= dmg;
            damageTaken += dmg;
            invuln = 0.75f;
            regenDelay = 2.5f;
            AddShake(0.25f);
            G.Vibrate();
            G.Audio.Play(Sfx.Hurt);
            fx.Spark(ppos, new Color(1f, 0.5f, 0.3f));
            if (hull <= 0f)
            {
                hull = 0f;
                State = Phase.Dead; phaseT = 0f;
                fx.Explosion(ppos, 1.6f);
                G.Audio.Play(Sfx.BigExplosion);
                player.gameObject.SetActive(false);
                if (droneSr != null) droneSr.enabled = false;
                beamSr.enabled = false;
                hud.Banner("AIRCRAFT LOST", "Gold collected is kept", 2.4f, Pal.Red);
                G.Audio.PlayMusic(GameAudio.Track.None);
                G.Audio.Play(Sfx.Fail);
            }
        }

        // ------------------------------------------------------------------ specials
        public void TrySpecial()
        {
            if (!Alive || specialIndex < 0) return;
            if (!(State == Phase.Waves || State == Phase.Warning || State == Phase.Boss)) return;
            if (specialCd > 0f) return;
            int ammo = G.Save.Ammo(specialIndex);
            if (ammo <= 0) { hud.Flash("OUT OF AMMO"); return; }
            G.Save.SetAmmo(specialIndex, ammo - 1);
            var def = Specials.All[specialIndex];
            specialCd = def.cooldown;
            float dmg = def.damage * stats.Mult(Stat.SpecialDamage) * (1f + 0.25f * MissionIndex);
            G.Audio.Play(Sfx.Special);
            switch (def.kind)
            {
                case SpecialKind.ClusterBombs:
                    for (int i = 0; i < 6; i++)
                    {
                        var b = NewShot(Art.Bomb(), ppos + new Vector2(0.1f * i, -0.2f), new Vector2(2.5f + i * 0.7f, -0.5f - i * 0.15f), dmg, true, ShotKind.Bomb, 44);
                        b.aoe = 1.0f; b.special = true; b.radius = 0.18f;
                    }
                    break;
                case SpecialKind.SeekerMissiles:
                    for (int i = 0; i < 4; i++)
                    {
                        var m = NewShot(Art.Missile(false), ppos + new Vector2(0, -0.15f), new Vector2(4f, (i - 1.5f) * 2.5f), dmg, true, ShotKind.Missile, 44);
                        m.speed = 10f; m.turn = 6f; m.aoe = 0.6f; m.special = true; m.life = 4f;
                        m.target = NthNearest(ppos, i);
                    }
                    G.Audio.Play(Sfx.Missile);
                    break;
                case SpecialKind.RocketSalvo:
                    for (int i = 0; i < 8; i++)
                    {
                        float a = (i - 3.5f) * 5f * Mathf.Deg2Rad;
                        var r = NewShot(Art.Rocket(), ppos + new Vector2(0.4f, -0.1f), new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 15f, dmg, true, ShotKind.Rocket, 44);
                        r.aoe = 0.45f; r.special = true; r.radius = 0.15f;
                    }
                    G.Audio.Play(Sfx.Missile);
                    break;
                case SpecialKind.Napalm:
                {
                    var n = NewShot(Art.Bomb(), ppos + new Vector2(0, -0.2f), new Vector2(3f, -1f), dmg, true, ShotKind.Napalm, 44);
                    n.special = true; n.radius = 0.2f; n.life = 8f;
                    break;
                }
                case SpecialKind.RailLance:
                    beamT = 1.3f; beamTick = 0f;
                    break;
                case SpecialKind.AegisShield:
                    shieldT = 5f;
                    break;
                case SpecialKind.SwarmDrones:
                    for (int i = 0; i < 3; i++)
                    {
                        var k = NewShot(Art.Enemy(EnemyKind.Drone), ppos + new Vector2(-0.2f, (i - 1) * 0.4f), new Vector2(2f, (i - 1) * 2f), dmg, true, ShotKind.Kamikaze, 44);
                        k.sr.flipX = true; k.sr.color = new Color(0.6f, 0.85f, 1f);
                        k.speed = 5f; k.turn = 4f; k.aoe = 1.0f; k.special = true; k.life = 6f; k.radius = 0.25f;
                        k.target = NthNearest(ppos, i);
                    }
                    break;
                case SpecialKind.Thermobaric:
                    hud.WhiteFlash();
                    AddShake(0.6f);
                    G.Audio.Play(Sfx.BigExplosion);
                    foreach (var s in shots) if (!s.fromPlayer) s.dead = true;
                    foreach (var e in enemies.ToArray())
                    {
                        if (e.dead || !OnScreen(e.pos, 0.5f)) continue;
                        fx.Explosion(e.pos, 1f);
                        DamageEnemy(e, dmg * (e.isBoss ? 0.5f : 1f), true, e.pos);
                    }
                    break;
            }
        }

        void UpdateBeam(float dt)
        {
            if (beamT <= 0f) { beamSr.enabled = false; return; }
            beamT -= dt;
            beamSr.enabled = true;
            float len = halfW - ppos.x + 1f;
            beamSr.transform.position = new Vector3(ppos.x + 0.6f + len / 2f, ppos.y, 0);
            beamSr.transform.localScale = new Vector3(len, Random.Range(0.45f, 0.6f), 1);
            beamSr.color = new Color(0.55f, 0.9f, 1f, 0.9f);
            beamTick -= dt;
            if (beamTick <= 0f)
            {
                beamTick = 0.1f;
                float dmg = Specials.All[(int)SpecialKind.RailLance].damage * stats.Mult(Stat.SpecialDamage) * (1f + 0.25f * MissionIndex);
                foreach (var e in enemies.ToArray())
                    if (!e.dead && e.pos.x > ppos.x && Mathf.Abs(e.pos.y - ppos.y) < e.half.y + 0.3f && OnScreen(e.pos, 0.3f))
                        DamageEnemy(e, dmg, true, new Vector2(e.pos.x - e.half.x, ppos.y));
                foreach (var s in shots) if (!s.fromPlayer && s.pos.x > ppos.x && Mathf.Abs(s.pos.y - ppos.y) < 0.35f) s.dead = true;
            }
        }

        // ------------------------------------------------------------------ spawning
        static bool IsGround(EnemyKind k) => k >= EnemyKind.Tank;

        void SpawnFormation()
        {
            float prog = Progress01;
            bool ground = M.ground.Length > 0 && Random.value < 0.42f;
            if (ground)
            {
                var k = M.ground[Random.Range(0, M.ground.Length)];
                int n = k == EnemyKind.Bunker ? 1 : Random.Range(1, 3 + (prog > 0.5f ? 1 : 0));
                for (int i = 0; i < n; i++) Queue(i * 0.9f, () => SpawnEnemy(k, new Vector2(halfW + 1.5f, 0)));
            }
            else
            {
                var k = M.air[Random.Range(0, M.air.Length)];
                float y = Random.Range(groundTop + 1.2f, halfH - 1.4f);
                switch (k)
                {
                    case EnemyKind.Fighter:
                    {
                        int n = Random.Range(3, 6);
                        for (int i = 0; i < n; i++) { int ii = i; Queue(i * 0.35f, () => { var e = SpawnEnemy(k, new Vector2(halfW + 1f, y)); e.phase = ii * 0.6f; }); }
                        break;
                    }
                    case EnemyKind.Drone:
                    {
                        int n = Random.Range(5, 8);
                        for (int i = 0; i < n; i++)
                        {
                            int row = (i + 1) / 2; float side = i % 2 == 0 ? 1f : -1f;
                            var at = new Vector2(halfW + 1f + row * 0.6f, Mathf.Clamp(y + side * row * 0.45f, groundTop + 0.8f, halfH - 1f));
                            SpawnEnemy(k, at).phase = 0f;
                        }
                        break;
                    }
                    case EnemyKind.Interceptor:
                    {
                        int n = Random.Range(2, 4);
                        for (int i = 0; i < n; i++) Queue(i * 0.5f, () => SpawnEnemy(k, new Vector2(halfW + 1f, Random.Range(groundTop + 1f, halfH - 1f))));
                        break;
                    }
                    case EnemyKind.Gunship:
                    {
                        int n = Random.Range(1, 3);
                        for (int i = 0; i < n; i++) { int ii = i; Queue(i * 1.2f, () => { var e = SpawnEnemy(k, new Vector2(halfW + 1.5f, Random.Range(groundTop + 1.5f, halfH - 1.5f))); e.target = new Vector2(halfW * (0.25f + 0.3f * ii), e.pos.y); }); }
                        break;
                    }
                    default:
                        SpawnEnemy(k, new Vector2(halfW + 2f, k == EnemyKind.Bomber ? Random.Range(1.2f, halfH - 1.6f) : y));
                        break;
                }
            }
            spawnT = Mathf.Lerp(3.0f, 1.7f, prog) / (1f + 0.05f * MissionIndex);
        }

        struct EnemyStats { public float hp, speed; public long gold; public EnemyStats(float h, float s, long g) { hp = h; speed = s; gold = g; } }

        static EnemyStats Base(EnemyKind k)
        {
            switch (k)
            {
                case EnemyKind.Fighter: return new EnemyStats(30, 4.2f, 12);
                case EnemyKind.Interceptor: return new EnemyStats(42, 7f, 15);
                case EnemyKind.Bomber: return new EnemyStats(260, 1.3f, 60);
                case EnemyKind.Gunship: return new EnemyStats(170, 3f, 45);
                case EnemyKind.Drone: return new EnemyStats(12, 4.2f, 6);
                case EnemyKind.AceFighter: return new EnemyStats(140, 5f, 55);
                case EnemyKind.Tank: return new EnemyStats(60, 0.5f, 20);
                case EnemyKind.AAGun: return new EnemyStats(70, 0f, 22);
                case EnemyKind.SamLauncher: return new EnemyStats(80, 0.3f, 28);
                case EnemyKind.Truck: return new EnemyStats(40, 1.4f, 32);
                case EnemyKind.Gunboat: return new EnemyStats(110, 0.4f, 35);
                case EnemyKind.Bunker: return new EnemyStats(200, 0f, 45);
            }
            return new EnemyStats(30, 4f, 10);
        }

        Enemy SpawnEnemy(EnemyKind k, Vector2 at)
        {
            var b = Base(k);
            var e = new Enemy { kind = k, ground = IsGround(k) };
            e.go = new GameObject(k.ToString());
            e.go.transform.SetParent(world, false);
            e.sr = e.go.AddComponent<SpriteRenderer>();
            e.sr.sprite = e.ground ? Art.Ground(k) : Art.Enemy(k);
            e.sr.sortingOrder = e.ground ? 15 : 30;
            var size = e.sr.sprite.bounds.size;
            e.half = new Vector2(size.x * 0.42f, size.y * 0.36f);
            e.maxHp = e.hp = b.hp * diff;
            e.speed = b.speed;
            e.gold = (long)Mathf.Round(b.gold * (1f + 0.25f * MissionIndex));
            e.pos = at;
            if (e.ground) e.pos.y = groundTop + size.y * 0.5f - (M.sea ? 0.18f : 0.06f);
            e.anchorY = e.pos.y;
            e.fireT = Random.Range(0.8f, 2.2f);

            // turrets
            Vector2 barrelAt = Vector2.zero; bool twin = false; bool hasBarrel = true;
            switch (k)
            {
                case EnemyKind.Tank: barrelAt = new Vector2(0.03f, 0.08f); break;
                case EnemyKind.AAGun: barrelAt = new Vector2(0f, -0.06f); twin = true; break;
                case EnemyKind.Gunboat: barrelAt = new Vector2(-0.6f, -0.03f); break;
                case EnemyKind.Bunker: barrelAt = new Vector2(0f, 0.08f); break;
                default: hasBarrel = false; break;
            }
            if (hasBarrel)
            {
                var bgo = new GameObject("Barrel");
                bgo.transform.SetParent(e.go.transform, false);
                bgo.transform.localPosition = barrelAt;
                bgo.transform.localRotation = Quaternion.Euler(0, 0, 150f);
                var bsr = bgo.AddComponent<SpriteRenderer>();
                bsr.sprite = Art.Barrel(twin);
                bsr.sortingOrder = e.sr.sortingOrder - 1;
                e.barrel = bgo.transform;
            }
            if (k == EnemyKind.Gunship)
            {
                var r = new GameObject("Rotor");
                r.transform.SetParent(e.go.transform, false);
                r.transform.localPosition = new Vector3(0, size.y * 0.33f, 0);
                var rsr = r.AddComponent<SpriteRenderer>();
                rsr.sprite = Art.Rotor();
                rsr.sortingOrder = e.sr.sortingOrder + 1;
                e.rotor = r.transform;
            }
            e.go.transform.position = e.pos;
            enemies.Add(e);
            return e;
        }

        // ------------------------------------------------------------------ enemy behavior
        float EnemyShotDamage => 12f * (1f + 0.22f * MissionIndex);
        float EnemyShotSpeed => 5.5f + 0.25f * MissionIndex;
        float FireRate => 1f + 0.08f * MissionIndex;

        bool OnScreen(Vector2 p, float margin) => p.x > -halfW - margin && p.x < halfW + margin && p.y > -halfH - margin && p.y < halfH + margin;

        void UpdateEnemies(float dt)
        {
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                var e = enemies[i];
                if (e.dead) { Destroy(e.go); enemies.RemoveAt(i); continue; }
                e.t += dt;
                e.fireT -= dt * FireRate;
                if (e.isBoss) UpdateBoss(e, dt);
                else if (e.ground) UpdateGround(e, dt);
                else UpdateAir(e, dt);

                e.go.transform.position = e.pos;
                if (e.rotor != null) e.rotor.localScale = new Vector3(Mathf.Abs(Mathf.Sin(e.t * 30f)) * 0.8f + 0.2f, 1, 1);
                if (e.flash > 0f && e.state != 99) { e.flash -= dt; e.sr.color = e.flash > 0f ? new Color(1f, 0.55f, 0.5f) : Color.white; }
                if (!e.isBoss && e.hp < e.maxHp * 0.35f && Random.value < dt * 5f) fx.Smoke(e.pos, 0.3f);

                // body collision with the player
                if (Alive && !e.dead && (!e.isBoss || (e.entered && e.state != 99)) && e.Contains(ppos, 0.25f))
                {
                    if (shieldT > 0f) DamageEnemy(e, 120f * dt * diff, true, ppos);
                    else if (!e.ground || e.isBoss) { HurtPlayer(25f * (1f + 0.2f * MissionIndex)); if (!e.isBoss) DamageEnemy(e, 60f, true, ppos); }
                }

                if (!e.dead && (e.pos.x < -halfW - 3f || e.pos.y > halfH + 4f || e.pos.y < -halfH - 4f))
                {
                    Destroy(e.go); enemies.RemoveAt(i);
                }
            }
        }

        void UpdateAir(Enemy e, float dt)
        {
            bool visible = e.pos.x < halfW - 0.3f && e.pos.x > -halfW + 0.5f;
            switch (e.kind)
            {
                case EnemyKind.Fighter:
                    e.pos.x -= e.speed * dt;
                    e.pos.y = e.anchorY + Mathf.Sin(e.t * 2f + e.phase) * 0.7f;
                    if (visible && e.fireT <= 0f) { Aimed(e.pos + new Vector2(-0.6f, 0), 1, 0f); e.fireT = Random.Range(1.8f, 3.2f); }
                    break;
                case EnemyKind.Interceptor:
                    e.pos.x -= e.speed * dt;
                    if (e.pos.x < halfW * 0.4f) e.vel.y = Mathf.MoveTowards(e.vel.y, Mathf.Sign(ppos.y - e.pos.y) * 3f, 6f * dt);
                    e.pos.y += e.vel.y * dt;
                    if (visible && e.state == 0 && e.pos.x < halfW * 0.6f) { e.state = 1; Aimed(e.pos + new Vector2(-0.6f, 0), 1, 0f); }
                    break;
                case EnemyKind.Bomber:
                    e.pos.x -= e.speed * dt;
                    e.pos.y = e.anchorY + Mathf.Sin(e.t * 0.8f) * 0.25f;
                    if (visible && e.fireT <= 0f) { Aimed(e.pos + new Vector2(-0.9f, -0.1f), 3, 16f); e.fireT = 2.6f; }
                    break;
                case EnemyKind.Gunship:
                    if (e.state == 0)
                    {
                        e.pos = Vector2.MoveTowards(e.pos, e.target, 3.5f * dt);
                        if ((e.pos - e.target).sqrMagnitude < 0.01f) { e.state = 1; e.stateT = 0f; }
                    }
                    else if (e.state == 1)
                    {
                        e.stateT += dt;
                        e.pos.y = e.target.y + Mathf.Sin(e.t * 1.6f) * 0.3f;
                        if (e.fireT <= 0f) { e.shotsLeft = 3; e.fireT = 2.4f; }
                        if (e.shotsLeft > 0 && Mathf.Repeat(e.stateT, 0.2f) < dt) { e.shotsLeft--; Aimed(e.pos + new Vector2(-0.7f, -0.15f), 1, 0f); }
                        if (e.stateT > 6.5f) e.state = 2;
                    }
                    else { e.pos += new Vector2(-4f, 1.2f) * dt; }
                    break;
                case EnemyKind.Drone:
                    e.pos.x -= e.speed * dt;
                    e.pos.y = e.anchorY + Mathf.Sin(e.t * 3f + e.phase) * 0.9f;
                    if (MissionIndex >= 4 && visible && e.fireT <= 0f) { Aimed(e.pos, 1, 0f); e.fireT = Random.Range(3f, 5f); }
                    break;
                case EnemyKind.AceFighter:
                    if (e.state == 0)
                    {
                        e.target = new Vector2(Random.Range(0f, halfW - 1.2f), Random.Range(groundTop + 1f, halfH - 1f));
                        e.state = 1;
                    }
                    e.stateT += dt;
                    if (e.stateT > 11f) { e.target = new Vector2(-halfW - 5f, e.pos.y + 2f); e.speed = 9f; }
                    else if ((e.pos - e.target).sqrMagnitude < 0.04f || Mathf.Repeat(e.stateT, 1.3f) < dt)
                        e.target = new Vector2(Random.Range(0f, halfW - 1.2f), Random.Range(groundTop + 1f, halfH - 1f));
                    e.pos = Vector2.MoveTowards(e.pos, e.target, e.speed * dt);
                    if (e.stateT <= 11f && e.fireT <= 0f) { e.fireT = 1.7f; for (int s = 0; s < 3; s++) { var ee = e; Queue(s * 0.12f, () => { if (!ee.dead) Aimed(ee.pos + new Vector2(-0.6f, 0), 1, 0f); }); } }
                    break;
            }
        }

        void UpdateGround(Enemy e, float dt)
        {
            e.pos.x -= (Scroll + e.speed) * dt;
            if (M.sea) e.pos.y = e.anchorY + Mathf.Sin(e.t * 2f + e.anchorY) * 0.04f;
            bool active = e.pos.x < halfW - 0.4f && e.pos.x > -halfW + 0.8f;
            Vector2 tip = e.pos;
            if (e.barrel != null)
            {
                var d = ppos - (Vector2)e.barrel.position;
                float ang = Mathf.Clamp(Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg, 15f, 165f);
                e.barrel.rotation = Quaternion.Lerp(e.barrel.rotation, Quaternion.Euler(0, 0, ang), 4f * dt);
                tip = (Vector2)e.barrel.position + (Vector2)(e.barrel.right * 0.55f);
            }
            if (!active || e.fireT > 0f) return;
            switch (e.kind)
            {
                case EnemyKind.Tank: ShootFrom(tip, e.barrel.right, 1, 0f); e.fireT = 2.6f; break;
                case EnemyKind.Gunboat: ShootFrom(tip, e.barrel.right, 1, 0f); e.fireT = 2.2f; break;
                case EnemyKind.Bunker: ShootFrom(tip, e.barrel.right, 3, 12f); e.fireT = 3f; break;
                case EnemyKind.AAGun:
                {
                    var f = NewShot(Art.EnemyShot(), tip, (Vector2)e.barrel.right * (EnemyShotSpeed + 1.5f), EnemyShotDamage, false, ShotKind.Flak, 40);
                    f.life = Mathf.Clamp(Vector2.Distance(tip, ppos) / (EnemyShotSpeed + 1.5f), 0.4f, 1.6f);
                    f.sr.color = new Color(1f, 0.8f, 0.4f);
                    e.fireT = 2.2f;
                    break;
                }
                case EnemyKind.SamLauncher:
                {
                    var m = NewShot(Art.Missile(true), e.pos + new Vector2(-0.4f, 0.4f), new Vector2(-1.5f, 3f), EnemyShotDamage * 1.4f, false, ShotKind.Missile, 40);
                    m.speed = 4.2f + 0.2f * MissionIndex; m.turn = 1.8f; m.life = 5f; m.hp = 8f * diff; m.radius = 0.18f;
                    e.fireT = 4.5f;
                    G.Audio.Play(Sfx.Missile, 0.5f);
                    break;
                }
                default: e.fireT = 99f; break; // trucks don't shoot
            }
        }

        void Aimed(Vector2 from, int count, float spreadDeg)
        {
            var dir = (ppos - from);
            if (dir.sqrMagnitude < 0.01f) dir = Vector2.left;
            ShootFrom(from, dir.normalized, count, spreadDeg);
        }

        void ShootFrom(Vector2 from, Vector2 dir, int count, float spreadDeg)
        {
            float baseAng = Mathf.Atan2(dir.y, dir.x);
            for (int i = 0; i < count; i++)
            {
                float a = baseAng + (count > 1 ? (i - (count - 1) * 0.5f) * spreadDeg * Mathf.Deg2Rad : 0f);
                var s = NewShot(Art.EnemyShot(), from, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * EnemyShotSpeed, EnemyShotDamage, false, ShotKind.Bullet, 40);
                s.faceVelocity = false;
                s.radius = 0.1f;
            }
        }

        void Radial(Vector2 from, int count, float speedMul, float offset)
        {
            for (int i = 0; i < count; i++)
            {
                float a = offset + i * Mathf.PI * 2f / count;
                var s = NewShot(Art.EnemyShot(), from, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * EnemyShotSpeed * speedMul, EnemyShotDamage, false, ShotKind.Bullet, 40);
                s.faceVelocity = false; s.radius = 0.1f;
            }
        }

        // ------------------------------------------------------------------ bosses
        enum Atk { Aimed, Radial, Missiles, Flak, Minions, Stream }
        struct AtkDef { public Atk a; public int n; public float spread; public EnemyKind minion; public AtkDef(Atk a, int n, float s = 0f, EnemyKind m = EnemyKind.Drone) { this.a = a; this.n = n; spread = s; minion = m; } }

        static AtkDef[] Attacks(BossKind k)
        {
            switch (k)
            {
                case BossKind.RailBattery: return new[] { new AtkDef(Atk.Aimed, 5, 12f), new AtkDef(Atk.Flak, 3), new AtkDef(Atk.Stream, 6) };
                case BossKind.Destroyer: return new[] { new AtkDef(Atk.Radial, 12), new AtkDef(Atk.Missiles, 3), new AtkDef(Atk.Aimed, 3, 18f) };
                case BossKind.PeakFortress: return new[] { new AtkDef(Atk.Flak, 4), new AtkDef(Atk.Missiles, 2), new AtkDef(Atk.Radial, 10), new AtkDef(Atk.Aimed, 5, 10f) };
                case BossKind.SandCrawler: return new[] { new AtkDef(Atk.Aimed, 5, 14f), new AtkDef(Atk.Minions, 4, 0f, EnemyKind.Drone), new AtkDef(Atk.Stream, 8) };
                case BossKind.StratoGunship: return new[] { new AtkDef(Atk.Radial, 14), new AtkDef(Atk.Minions, 5, 0f, EnemyKind.Drone), new AtkDef(Atk.Aimed, 7, 10f), new AtkDef(Atk.Stream, 8) };
                case BossKind.Submarine: return new[] { new AtkDef(Atk.Missiles, 5), new AtkDef(Atk.Radial, 16), new AtkDef(Atk.Flak, 4) };
                case BossKind.WraithAce: return new[] { new AtkDef(Atk.Stream, 10), new AtkDef(Atk.Aimed, 3, 20f), new AtkDef(Atk.Missiles, 2), new AtkDef(Atk.Stream, 6) };
                default: return new[] { new AtkDef(Atk.Radial, 20), new AtkDef(Atk.Missiles, 4), new AtkDef(Atk.Minions, 3, 0f, EnemyKind.Interceptor), new AtkDef(Atk.Aimed, 7, 10f), new AtkDef(Atk.Flak, 5), new AtkDef(Atk.Stream, 10) };
            }
        }

        static float BossBaseHp(BossKind k)
        {
            switch (k)
            {
                case BossKind.RailBattery: return 1800f;
                case BossKind.Destroyer: return 2200f;
                case BossKind.PeakFortress: return 2500f;
                case BossKind.SandCrawler: return 2700f;
                case BossKind.StratoGunship: return 2900f;
                case BossKind.Submarine: return 3100f;
                case BossKind.WraithAce: return 2200f;
                default: return 4200f;
            }
        }

        bool BossOnGround(BossKind k) => k == BossKind.RailBattery || k == BossKind.Destroyer || k == BossKind.PeakFortress || k == BossKind.SandCrawler || k == BossKind.Submarine;

        void SpawnBoss()
        {
            var k = M.boss;
            var e = new Enemy { kind = EnemyKind.Bunker, isBoss = true, boss = k };
            e.go = new GameObject("Boss " + k);
            e.go.transform.SetParent(world, false);
            e.sr = e.go.AddComponent<SpriteRenderer>();
            e.sr.sprite = Art.Boss(k);
            bool onGround = BossOnGround(k);
            e.sr.sortingOrder = onGround ? 12 : 28;
            e.ground = onGround;
            var size = e.sr.sprite.bounds.size;
            e.half = new Vector2(size.x * 0.42f, size.y * 0.36f);
            e.maxHp = e.hp = BossBaseHp(k) * (1f + 0.55f * MissionIndex);
            e.gold = 0;
            float y = onGround ? groundTop + size.y * 0.5f - (M.sea ? 0.55f : 0.12f) : 1.0f;
            e.pos = new Vector2(halfW + size.x * 0.5f + 0.5f, y);
            e.anchorY = y;
            e.target = new Vector2(halfW - size.x * 0.42f, y);
            if (k == BossKind.WraithAce) e.target = new Vector2(halfW * 0.5f, 1f);
            e.attackT = 1.5f;
            e.go.transform.position = e.pos;
            enemies.Add(e);
            boss = e;
        }

        void UpdateBoss(Enemy e, float dt)
        {
            if (!e.entered)
            {
                e.pos = Vector2.MoveTowards(e.pos, e.target, 2.2f * dt);
                if ((e.pos - e.target).sqrMagnitude < 0.01f) { e.entered = true; e.t = 0f; }
                return;
            }
            if (e.state == 99)
            {
                // wreck: shudders and sinks while it explodes
                e.pos += new Vector2(Random.Range(-0.04f, 0.04f), -0.35f * dt);
                e.sr.color = Color.Lerp(Color.white, new Color(0.35f, 0.3f, 0.3f), phaseT / 2.4f);
                if (phaseT > 2.3f) e.dead = true;
                return;
            }
            var k = e.boss;
            if (k == BossKind.WraithAce)
            {
                e.stateT -= dt;
                if (e.stateT <= 0f) { e.stateT = Random.Range(0.8f, 1.6f); e.vel = new Vector2(Random.Range(-1f, halfW - 1.5f), Random.Range(groundTop + 1.2f, halfH - 1.2f)); }
                e.pos = Vector2.MoveTowards(e.pos, e.vel, 5.5f * dt);
            }
            else if (BossOnGround(k))
            {
                e.pos.x = e.target.x + Mathf.Sin(e.t * 0.3f) * (k == BossKind.SandCrawler ? 1.2f : 0.4f);
                if (M.sea) e.pos.y = e.anchorY + Mathf.Sin(e.t * 1.1f) * 0.08f;
            }
            else
            {
                e.pos.x = e.target.x + Mathf.Sin(e.t * 0.37f) * 0.8f;
                e.pos.y = e.anchorY + Mathf.Sin(e.t * 0.6f) * (halfH - e.half.y - 1.2f) * 0.6f;
            }

            e.attackT -= dt;
            if (e.attackT > 0f) return;
            var atks = Attacks(k);
            var atk = atks[e.attackIndex % atks.Length];
            e.attackIndex++;
            bool enraged = e.hp < e.maxHp * 0.5f;
            e.attackT = (k == BossKind.BlackCitadel ? 1.6f : 2.1f) * (enraged ? 0.7f : 1f) / (1f + 0.04f * MissionIndex);
            DoAttack(e, atk, enraged);
        }

        Vector2 Muzzle(Enemy e, int i)
        {
            switch (i % 3)
            {
                case 0: return e.pos + new Vector2(-e.half.x * 0.9f, e.half.y * 0.35f);
                case 1: return e.pos + new Vector2(-e.half.x * 0.2f, e.half.y * 0.8f);
                default: return e.pos + new Vector2(e.half.x * 0.4f, e.half.y * 0.6f);
            }
        }

        void DoAttack(Enemy e, AtkDef atk, bool enraged)
        {
            int extra = enraged ? 1 : 0;
            switch (atk.a)
            {
                case Atk.Aimed:
                    Aimed(Muzzle(e, 0), atk.n + extra * 2, atk.spread);
                    break;
                case Atk.Radial:
                {
                    float off = Random.value * Mathf.PI;
                    Radial(e.pos, atk.n + extra * 4, 0.8f, off);
                    if (enraged) { var ee = e; Queue(0.35f, () => { if (!ee.dead) Radial(ee.pos, atk.n, 0.8f, off + Mathf.PI / atk.n); }); }
                    break;
                }
                case Atk.Missiles:
                    for (int i = 0; i < atk.n + extra; i++)
                    {
                        var ee = e; int ii = i;
                        Queue(i * 0.25f, () =>
                        {
                            if (ee.dead) return;
                            var m = NewShot(Art.Missile(true), Muzzle(ee, ii + 1), new Vector2(-2f, Random.Range(1.5f, 3.5f)), EnemyShotDamage * 1.4f, false, ShotKind.Missile, 40);
                            m.speed = 4.5f + 0.2f * MissionIndex; m.turn = 1.9f; m.life = 5f; m.hp = 8f * diff; m.radius = 0.18f;
                        });
                    }
                    G.Audio.Play(Sfx.Missile, 0.6f);
                    break;
                case Atk.Flak:
                    for (int i = 0; i < atk.n + extra; i++)
                    {
                        var from = Muzzle(e, i);
                        var aim = (ppos + Random.insideUnitCircle * 1.6f - from).normalized;
                        var f = NewShot(Art.EnemyShot(), from, aim * (EnemyShotSpeed + 1.5f), EnemyShotDamage, false, ShotKind.Flak, 40);
                        f.life = Random.Range(0.6f, 1.4f);
                        f.sr.color = new Color(1f, 0.8f, 0.4f);
                    }
                    break;
                case Atk.Minions:
                    for (int i = 0; i < atk.n; i++)
                    {
                        var m = SpawnEnemy(atk.minion, e.pos + new Vector2(-e.half.x * 0.5f, (i - atk.n * 0.5f) * 0.5f));
                        m.anchorY = Mathf.Clamp(m.pos.y, groundTop + 1f, halfH - 1f);
                        m.phase = i * 0.8f;
                        m.gold = Math.Max(1, m.gold / 3);
                    }
                    break;
                case Atk.Stream:
                    for (int i = 0; i < atk.n + extra * 3; i++)
                    {
                        var ee = e;
                        Queue(i * 0.11f, () => { if (!ee.dead && Alive) Aimed(Muzzle(ee, 0), 1, 0f); });
                    }
                    break;
            }
        }

        void BossDefeated(Enemy e)
        {
            State = Phase.BossDown; phaseT = 0f;
            foreach (var s in shots) if (!s.fromPlayer) s.dead = true;
            foreach (var other in enemies) if (other != e && !other.dead) { other.dead = true; fx.Explosion(other.pos, 0.8f); }
            for (int i = 0; i < 12; i++)
            {
                var at = e.pos + new Vector2(Random.Range(-e.half.x, e.half.x), Random.Range(-e.half.y, e.half.y));
                Queue(i * 0.18f, () => { fx.Explosion(at, Random.Range(1f, 2f)); G.Audio.Play(Sfx.Explosion); AddShake(0.2f); });
            }
            var center = e.pos;
            Queue(2.2f, () => { fx.Explosion(center, 4f); G.Audio.Play(Sfx.BigExplosion); AddShake(0.8f); });
            // a shower of gold from the wreck
            long bonus = (long)(M.reward * 0.25f);
            for (int i = 0; i < 16; i++) SpawnCoin(center + Random.insideUnitCircle * 1.5f, bonus / 16);
            boss = e;
        }

        // ------------------------------------------------------------------ damage
        void DamageEnemy(Enemy e, float dmg, bool special, Vector2 at)
        {
            if (e.dead) return;
            if (e.isBoss && (!e.entered || e.state == 99)) return;
            if (e.isBoss) dmg *= stats.Mult(Stat.BossDamage);
            bool crit = !special && Random.value * 100f < stats[Stat.Crit];
            if (crit) dmg *= 1.5f + stats[Stat.CritDamage] / 100f;
            e.hp -= dmg;
            e.flash = 0.06f;
            if (crit) hud.Number(at, dmg, true);
            else if (special || e.isBoss) hud.Number(at, dmg, false);
            if (e.hp <= 0f) Kill(e);
        }

        void Kill(Enemy e)
        {
            e.dead = true;
            kills++; killsSinceDrop++;
            float size = Mathf.Clamp(Mathf.Max(e.half.x, e.half.y) * 1.6f, 0.6f, 3f);
            fx.Explosion(e.pos, size);
            G.Audio.Play(size > 1.5f ? Sfx.BigExplosion : Sfx.Explosion, 0.8f);
            AddShake(Mathf.Min(0.12f * size, 0.4f));
            if (e.isBoss) { e.dead = false; e.hp = 0; e.state = 99; BossDefeated(e); return; }

            // gold
            int coins = Mathf.Clamp((int)(e.gold / 8), 1, 7);
            for (int i = 0; i < coins; i++) SpawnCoin(e.pos + Random.insideUnitCircle * 0.3f, Math.Max(1, e.gold / coins));

            // gun power: guaranteed from big targets, otherwise roughly every 10 kills
            bool bigTarget = e.kind == EnemyKind.Bomber || e.kind == EnemyKind.Gunship || e.kind == EnemyKind.AceFighter || e.kind == EnemyKind.Bunker;
            if (Power < MaxPower && (bigTarget || killsSinceDrop >= 10 || Random.value < 0.05f))
            {
                killsSinceDrop = 0;
                SpawnPickup(PickupKind.Power, e.pos);
            }
            else if (Random.value < 0.035f || (bigTarget && Hull01 < 0.5f))
                SpawnPickup(PickupKind.Repair, e.pos);
        }

        void Explode(Vector2 at, float radius, float dmg)
        {
            fx.Explosion(at, radius * 1.3f);
            G.Audio.Play(Sfx.Explosion, 0.6f);
            foreach (var e in enemies.ToArray())
                if (!e.dead && e.Contains(at, radius)) DamageEnemy(e, dmg, true, at);
            foreach (var s in enemyMissiles) if (!s.dead && (s.pos - at).sqrMagnitude < radius * radius) { s.dead = true; fx.Explosion(s.pos, 0.4f); }
        }

        Enemy Nearest(Vector2 from, float maxDist, bool includeOffscreen)
        {
            Enemy best = null; float bd = maxDist * maxDist;
            foreach (var e in enemies)
            {
                if (e.dead || (e.isBoss && (!e.entered || e.state == 99))) continue;
                if (!includeOffscreen && !OnScreen(e.pos, -0.2f)) continue;
                float d = (e.pos - from).sqrMagnitude;
                if (d < bd) { bd = d; best = e; }
            }
            return best;
        }

        Enemy NthNearest(Vector2 from, int n)
        {
            var list = new List<Enemy>();
            foreach (var e in enemies) if (!e.dead && OnScreen(e.pos, -0.2f) && (!e.isBoss || (e.entered && e.state != 99))) list.Add(e);
            if (list.Count == 0) return null;
            list.Sort((a, b) => (a.pos - from).sqrMagnitude.CompareTo((b.pos - from).sqrMagnitude));
            return list[n % list.Count];
        }

        // ------------------------------------------------------------------ shots
        Shot NewShot(Sprite sprite, Vector2 at, Vector2 vel, float dmg, bool fromPlayer, ShotKind kind, int order)
        {
            var s = new Shot { pos = at, vel = vel, damage = dmg, fromPlayer = fromPlayer, kind = kind };
            s.sr = pool.Get(sprite, order);
            s.sr.transform.position = at;
            shots.Add(s);
            if (!fromPlayer && kind == ShotKind.Missile) enemyMissiles.Add(s);
            return s;
        }

        void UpdateShots(float dt)
        {
            for (int i = shots.Count - 1; i >= 0; i--)
            {
                var s = shots[i];
                if (!s.dead) StepShot(s, dt);
                if (s.dead)
                {
                    pool.Release(s.sr);
                    shots.RemoveAt(i);
                    if (!s.fromPlayer && s.kind == ShotKind.Missile) enemyMissiles.Remove(s);
                    continue;
                }
                s.sr.transform.position = s.pos;
                if (s.faceVelocity && s.vel.sqrMagnitude > 0.01f && !(s.kind == ShotKind.Napalm && s.pierce == 1))
                    s.sr.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(s.vel.y, s.vel.x) * Mathf.Rad2Deg + (s.sr.flipX ? 180f : 0f));
            }
        }

        void StepShot(Shot s, float dt)
        {
            s.life -= dt;
            switch (s.kind)
            {
                case ShotKind.Bullet:
                case ShotKind.Rocket:
                    s.pos += s.vel * dt;
                    break;
                case ShotKind.Missile:
                case ShotKind.Kamikaze:
                {
                    Vector2 goal;
                    if (s.fromPlayer)
                    {
                        if (s.target == null || s.target.dead) s.target = Nearest(s.pos, 30f, false);
                        goal = s.target != null ? s.target.pos : s.pos + new Vector2(5f, 0);
                    }
                    else goal = ppos;
                    if (s.kind == ShotKind.Kamikaze) s.speed = Mathf.Min(s.speed + 6f * dt, 13f);
                    var want = (goal - s.pos).normalized;
                    var cur = s.vel.sqrMagnitude > 0.001f ? s.vel.normalized : want;
                    float ang = Vector2.SignedAngle(cur, want) * Mathf.Deg2Rad;
                    float step = Mathf.Clamp(ang, -s.turn * dt, s.turn * dt);
                    float ca = Mathf.Cos(step), sa = Mathf.Sin(step);
                    var nd = new Vector2(cur.x * ca - cur.y * sa, cur.x * sa + cur.y * ca);
                    float spd = Mathf.MoveTowards(s.vel.magnitude, s.speed, 10f * dt);
                    s.vel = nd * spd;
                    s.pos += s.vel * dt;
                    if (Random.value < dt * 20f) fx.Smoke(s.pos - nd * 0.3f, 0.18f);
                    if (s.life <= 0f) { if (s.fromPlayer && s.aoe > 0) Explode(s.pos, s.aoe, s.damage); else fx.Explosion(s.pos, 0.4f); s.dead = true; return; }
                    break;
                }
                case ShotKind.Bomb:
                    s.vel.y -= 9f * dt;
                    s.vel.x = Mathf.MoveTowards(s.vel.x, 0f, 0.8f * dt);
                    s.pos += s.vel * dt;
                    if (s.pos.y <= groundTop + 0.05f) { Explode(new Vector2(s.pos.x, groundTop + 0.2f), s.aoe, s.damage); s.dead = true; return; }
                    break;
                case ShotKind.Napalm:
                    if (s.pierce == 0)
                    {
                        s.vel.y -= 9f * dt;
                        s.pos += s.vel * dt;
                        if (s.pos.y <= groundTop + 0.05f)
                        {
                            s.pierce = 1; s.life = 3.5f; s.vel = new Vector2(-Scroll, 0); s.pos.y = groundTop + 0.15f;
                            s.sr.color = new Color(0, 0, 0, 0);
                            fx.Explosion(s.pos, 1f);
                        }
                    }
                    else
                    {
                        s.pos += s.vel * dt;
                        for (int i = 0; i < 3; i++) if (Random.value < dt * 25f) fx.Fire(s.pos + new Vector2(Random.Range(-1.4f, 1.4f), 0));
                        s.tick -= dt;
                        if (s.tick <= 0f)
                        {
                            s.tick = 0.2f;
                            foreach (var e in enemies.ToArray())
                                if (!e.dead && e.ground && Mathf.Abs(e.pos.x - s.pos.x) < 1.5f + e.half.x) DamageEnemy(e, s.damage, true, e.pos);
                        }
                        if (s.life <= 0f) s.dead = true;
                        return;
                    }
                    break;
                case ShotKind.Flak:
                    s.pos += s.vel * dt;
                    if (s.life <= 0f)
                    {
                        fx.Explosion(s.pos, 0.35f);
                        for (int i = 0; i < 6; i++)
                        {
                            float a = i * Mathf.PI / 3f + Random.value * 0.3f;
                            var frag = NewShot(Art.EnemyShot(), s.pos, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 3.2f, s.damage * 0.6f, false, ShotKind.Bullet, 40);
                            frag.faceVelocity = false; frag.radius = 0.08f; frag.life = 1.2f;
                            frag.sr.transform.localScale = Vector3.one * 0.7f;
                        }
                        s.dead = true; return;
                    }
                    break;
            }
            if (s.life <= 0f || !OnScreen(s.pos, 2.5f)) { s.dead = true; return; }

            if (s.fromPlayer)
            {
                // player shots shoot down enemy missiles
                if (s.kind == ShotKind.Bullet || s.kind == ShotKind.Rocket)
                    foreach (var m in enemyMissiles)
                        if (!m.dead && (m.pos - s.pos).sqrMagnitude < 0.09f)
                        {
                            m.hp -= s.damage;
                            s.dead = true;
                            if (m.hp <= 0f) { m.dead = true; fx.Explosion(m.pos, 0.45f); G.Audio.Play(Sfx.Hit, 0.5f); }
                            return;
                        }
                foreach (var e in enemies)
                {
                    if (e.dead || !e.Contains(s.pos, s.radius)) continue;
                    if (e.isBoss && (!e.entered || e.state == 99)) continue;
                    if (s.aoe > 0f) Explode(s.pos, s.aoe, s.damage);
                    else { DamageEnemy(e, s.damage, s.special, s.pos); fx.Spark(s.pos, new Color(1f, 0.9f, 0.5f)); G.Audio.Play(Sfx.Hit, 0.25f); }
                    s.dead = true;
                    return;
                }
            }
            else if (Alive)
            {
                if ((s.pos - ppos).sqrMagnitude < (s.radius + 0.22f) * (s.radius + 0.22f))
                {
                    if (shieldT > 0f) { s.dead = true; fx.Spark(s.pos, new Color(0.5f, 0.9f, 1f)); return; }
                    if (invuln > 0f) return;
                    HurtPlayer(s.damage);
                    if (s.kind == ShotKind.Missile) fx.Explosion(s.pos, 0.6f);
                    s.dead = true;
                }
            }
        }

        // ------------------------------------------------------------------ pickups
        void SpawnCoin(Vector2 at, long value)
        {
            var p = new Pickup { kind = PickupKind.Coin, pos = at, vel = Random.insideUnitCircle * 2.5f + new Vector2(0, 1f), value = value };
            p.sr = pool.Get(Art.Coin(), 46);
            p.sr.transform.position = at;
            pickups.Add(p);
        }

        void SpawnPickup(PickupKind k, Vector2 at)
        {
            var p = new Pickup { kind = k, pos = at, vel = new Vector2(-0.6f, 0.8f), life = 14f };
            p.sr = pool.Get(k == PickupKind.Power ? Art.PowerUp() : Art.Repair(), 47);
            p.sr.transform.position = at;
            pickups.Add(p);
        }

        void UpdatePickups(float dt)
        {
            float magnet = 1.6f * stats.Mult(Stat.Magnet);
            for (int i = pickups.Count - 1; i >= 0; i--)
            {
                var p = pickups[i];
                p.t += dt; p.life -= dt;
                var toPlayer = ppos - p.pos;
                float d = toPlayer.magnitude;
                bool collecting = State == Phase.BossDown || State == Phase.Victory; // vacuum everything after the boss
                if (Alive && (d < magnet || (collecting && p.kind == PickupKind.Coin)))
                    p.vel = Vector2.Lerp(p.vel, toPlayer.normalized * 14f, 8f * dt);
                else if (p.kind == PickupKind.Coin)
                    p.vel = Vector2.Lerp(p.vel, new Vector2(-1.8f, -0.3f), 2f * dt);
                else
                {
                    p.vel = new Vector2(-0.9f, Mathf.Cos(p.t * 2.5f) * 0.9f);
                    // power-ups and repairs stay on screen until they time out
                    if (p.pos.x < -halfW + 0.6f) p.vel.x = 0.6f;
                }
                p.pos += p.vel * dt;
                p.sr.transform.position = p.pos;
                if (p.kind == PickupKind.Coin) p.sr.transform.localScale = new Vector3(Mathf.Abs(Mathf.Cos(p.t * 6f)) * 0.8f + 0.2f, 1, 1);
                if (p.life < 2f && p.kind != PickupKind.Coin) p.sr.enabled = Mathf.Repeat(p.t * 8f, 1f) > 0.3f;

                bool gone = p.life <= 0f || p.pos.x < -halfW - 1.5f;
                if (Alive && d < 0.5f)
                {
                    Collect(p);
                    gone = true;
                }
                if (gone) { pool.Release(p.sr); p.sr.enabled = true; pickups.RemoveAt(i); }
            }
        }

        void Collect(Pickup p)
        {
            switch (p.kind)
            {
                case PickupKind.Coin:
                    long v = (long)Mathf.Max(1, Mathf.Round(p.value * stats.Mult(Stat.GoldBonus) * G.GoldMultiplier));
                    goldCollected += v;
                    G.Audio.Play(Sfx.Coin, 0.5f);
                    break;
                case PickupKind.Power:
                    if (Power < MaxPower) Power++;
                    hud.Flash(Power >= MaxPower ? "GUN POWER MAX" : "GUN POWER " + Power);
                    G.Audio.Play(Sfx.PowerUp);
                    fx.Ring(ppos, 2f, new Color(0.4f, 0.8f, 1f, 0.8f));
                    break;
                case PickupKind.Repair:
                    hull = Mathf.Min(maxHull, hull + maxHull * 0.35f);
                    hud.Flash("HULL REPAIRED");
                    G.Audio.Play(Sfx.Pickup);
                    fx.Ring(ppos, 2f, new Color(0.4f, 1f, 0.5f, 0.8f));
                    break;
            }
        }

        // ------------------------------------------------------------------ camera shake
        void AddShake(float amount)
        {
            if (!G.Settings.screenShake) return;
            shake = Mathf.Min(1f, Mathf.Max(shake, amount));
        }

        void UpdateShake(float dt)
        {
            shake = Mathf.Max(0f, shake - dt * 2.5f);
            var off = shake > 0f ? Random.insideUnitCircle * shake * 0.25f : Vector2.zero;
            G.Cam.transform.position = new Vector3(off.x, off.y, -10f);
        }

        // ------------------------------------------------------------------ finish
        public void Abort() => Finish(false, true);

        void Finish(bool won, bool aborted)
        {
            if (State == Phase.Done) return;
            State = Phase.Done;
            var save = G.Save;
            var r = new BattleResult
            {
                mission = MissionIndex, won = won, aborted = aborted, kills = kills, goldCollected = goldCollected,
                hullLost = maxHull > 0 ? damageTaken / maxHull : 0f, time = elapsed
            };
            if (won)
            {
                r.firstClear = !save.Cleared(MissionIndex);
                r.stars = r.hullLost < 0.25f ? 3 : r.hullLost < 0.9f ? 2 : 1;
                float starMult = 1f + 0.25f * (r.stars - 1);
                r.missionReward = (long)(M.reward * (r.firstClear ? 1f : 0.5f) * starMult * stats.Mult(Stat.GoldBonus) * G.GoldMultiplier);
                save.SetStars(MissionIndex, r.stars);
            }
            save.AddGold(r.Total);
            save.totalKills += kills;
            save.sorties++;
            save.tutorialSeen = true;
            G.SaveGame();
            G.LastResult = r;
            G.Audio.PlayMusic(GameAudio.Track.None);
            G.Show<ResultScreen>();
        }
    }
}
