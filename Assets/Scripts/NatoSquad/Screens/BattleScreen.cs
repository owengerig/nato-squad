using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NatoSquad
{
    /// <summary>Fires on touch-down (not release), for the special-weapon button.</summary>
    public class PressButton : MonoBehaviour, IPointerDownHandler
    {
        public Action onPress;
        public void OnPointerDown(PointerEventData e) { onPress?.Invoke(); }
    }

    /// <summary>The in-mission HUD on top of the battle world, plus the pause menu.</summary>
    public class BattleScreen : GameScreen
    {
        public override bool Transparent => true;

        Battle battle;
        GameObject battleGo;

        Image hullFill, progressFill, bossFill, specialCdFill, whiteFlash;
        Text hullLabel, goldText, specialAmmo, bannerTitle, bannerSub, flashText, bossName, dangerText;
        Image[] pips;
        RectTransform bossBar, specialRt, pauseRt, numbersRoot, progressBar;
        CanvasGroup bannerGroup;
        float bannerT, bannerDur, flashT, whiteT;
        bool bannerBlink;
        GameObject pausePanel;

        class Num { public Text t; public float life; public Vector2 pos; }
        readonly List<Num> nums = new List<Num>();
        readonly Stack<Text> freeNums = new Stack<Text>();

        public override void Build()
        {
            battleGo = new GameObject("Battle");
            battle = battleGo.AddComponent<Battle>();

            BuildHud();
            battle.Init(G.PendingMission, this);
            RefreshSpecial();
        }

        void OnDestroy()
        {
            Time.timeScale = 1f;
            if (battleGo != null) Destroy(battleGo);
        }

        void OnApplicationPause(bool paused)
        {
            if (paused && pausePanel == null && battle != null && battle.State != Battle.Phase.Done) Pause();
        }

        public override void OnBack()
        {
            if (pausePanel != null) Resume(); else Pause();
        }

        // ------------------------------------------------------------------ build
        void BuildHud()
        {
            numbersRoot = UI.Rect("Numbers", Root);
            UI.Stretch(numbersRoot);

            // hull (top-left)
            var hullBox = UI.Box(Root, "Hull", new Color(0, 0, 0, 0.45f));
            UI.Place(hullBox.rectTransform, new Vector2(0, 1), new Vector2(520, 96), new Vector2(24, -20));
            hullLabel = UI.Label(hullBox.transform, "HULL", 26, Pal.Text, TextAnchor.UpperLeft, FontStyle.Bold);
            UI.Stretch(hullLabel.rectTransform, 18, 0, 18, 8);
            var hullBg = UI.Box(hullBox.transform, "Bar", new Color(0.2f, 0.05f, 0.05f, 0.9f));
            UI.Place(hullBg.rectTransform, new Vector2(0, 0), new Vector2(484, 30), new Vector2(18, 14));
            hullFill = UI.Img(hullBg.transform, "Fill", Pal.Green);
            hullFill.rectTransform.anchorMin = Vector2.zero; hullFill.rectTransform.anchorMax = new Vector2(1, 1);
            hullFill.rectTransform.offsetMin = new Vector2(4, 4); hullFill.rectTransform.offsetMax = new Vector2(-4, -4);
            hullFill.rectTransform.pivot = new Vector2(0, 0.5f);

            // gun power pips under the hull bar
            var powerBox = UI.Box(Root, "Power", new Color(0, 0, 0, 0.45f));
            UI.Place(powerBox.rectTransform, new Vector2(0, 1), new Vector2(520, 56), new Vector2(24, -124));
            var pl = UI.Label(powerBox.transform, "GUN", 24, Pal.TextDim, TextAnchor.MiddleLeft, FontStyle.Bold);
            UI.Stretch(pl.rectTransform, 18, 0, 0, 0);
            pips = new Image[Battle.MaxPower];
            for (int i = 0; i < pips.Length; i++)
            {
                pips[i] = UI.Box(powerBox.transform, "Pip", Pal.Disabled);
                UI.Place(pips[i].rectTransform, new Vector2(0, 0.5f), new Vector2(42, 28), new Vector2(90 + i * 51, 0), new Vector2(0, 0.5f));
            }

            dangerText = UI.Label(Root, "DANGER", 44, Pal.Red, TextAnchor.UpperLeft, FontStyle.Bold);
            UI.Place(dangerText.rectTransform, new Vector2(0, 1), new Vector2(400, 60), new Vector2(30, -190));
            UI.AddShadow(dangerText);

            // mission progress (top-center), replaced by the boss bar during the boss
            progressBar = UI.Box(Root, "Progress", new Color(0, 0, 0, 0.45f)).rectTransform;
            UI.Place(progressBar, new Vector2(0.5f, 1), new Vector2(560, 26), new Vector2(0, -34));
            progressFill = UI.Img(progressBar, "Fill", Pal.Accent);
            progressFill.rectTransform.anchorMin = Vector2.zero; progressFill.rectTransform.anchorMax = Vector2.one;
            progressFill.rectTransform.offsetMin = new Vector2(4, 4); progressFill.rectTransform.offsetMax = new Vector2(-4, -4);
            progressFill.type = Image.Type.Filled; progressFill.fillMethod = Image.FillMethod.Horizontal; progressFill.sprite = Art.Pixel();

            bossBar = UI.Box(Root, "BossBar", new Color(0, 0, 0, 0.6f)).rectTransform;
            UI.Place(bossBar, new Vector2(0.5f, 1), new Vector2(900, 76), new Vector2(0, -20));
            bossName = UI.Label(bossBar, "", 26, Pal.Text, TextAnchor.UpperCenter, FontStyle.Bold);
            UI.Stretch(bossName.rectTransform, 0, 0, 0, 4);
            var bossBg = UI.Box(bossBar, "Bg", new Color(0.15f, 0.02f, 0.02f, 1f));
            UI.Place(bossBg.rectTransform, new Vector2(0.5f, 0), new Vector2(860, 28), new Vector2(0, 10), new Vector2(0.5f, 0));
            bossFill = UI.Img(bossBg.transform, "Fill", Pal.Red, Art.Pixel());
            UI.Stretch(bossFill.rectTransform, 4, 4, 4, 4);
            bossFill.type = Image.Type.Filled; bossFill.fillMethod = Image.FillMethod.Horizontal;
            bossBar.gameObject.SetActive(false);

            // gold (top-right) + pause
            var goldBox = UI.Box(Root, "Gold", new Color(0, 0, 0, 0.45f));
            UI.Place(goldBox.rectTransform, new Vector2(1, 1), new Vector2(330, 80), new Vector2(-150, -20));
            var coin = UI.Img(goldBox.transform, "Coin", Color.white, Art.Coin());
            UI.Place(coin.rectTransform, new Vector2(0, 0.5f), new Vector2(48, 48), new Vector2(18, 0), new Vector2(0, 0.5f));
            goldText = UI.Label(goldBox.transform, "0", 38, Pal.Gold, TextAnchor.MiddleRight, FontStyle.Bold);
            UI.Stretch(goldText.rectTransform, 70, 0, 20, 0);

            var pause = UI.Button(Root, "II", 40, new Color(0, 0, 0, 0.45f), Pause);
            pauseRt = (RectTransform)pause.transform;
            UI.Place(pauseRt, new Vector2(1, 1), new Vector2(110, 80), new Vector2(-24, -20));

            // special weapon button (bottom-right)
            var sp = UI.Box(Root, "Special", new Color(0.1f, 0.2f, 0.35f, 0.75f));
            sp.raycastTarget = true;
            specialRt = sp.rectTransform;
            UI.Place(specialRt, new Vector2(1, 0), new Vector2(230, 230), new Vector2(-40, 40));
            sp.gameObject.AddComponent<PressButton>().onPress = () => battle.TrySpecial();
            specialCdFill = UI.Img(sp.transform, "Cooldown", new Color(0, 0, 0, 0.55f), Art.Pixel());
            UI.Stretch(specialCdFill.rectTransform, 6, 6, 6, 6);
            specialCdFill.type = Image.Type.Filled; specialCdFill.fillMethod = Image.FillMethod.Vertical; specialCdFill.fillOrigin = 1;
            var icon = UI.Img(sp.transform, "Icon", Color.white);
            icon.name = "Icon";
            UI.Place(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(120, 120), new Vector2(0, 18));
            specialAmmo = UI.Label(sp.transform, "", 34, Pal.Text, TextAnchor.LowerCenter, FontStyle.Bold);
            UI.Stretch(specialAmmo.rectTransform, 0, 10, 0, 0);
            UI.AddShadow(specialAmmo, 2);

            // banners and flashes
            var banner = UI.Rect("Banner", Root);
            UI.Place(banner, new Vector2(0.5f, 0.62f), new Vector2(1600, 220), Vector2.zero);
            bannerGroup = banner.gameObject.AddComponent<CanvasGroup>();
            bannerGroup.blocksRaycasts = false;
            bannerTitle = UI.Label(banner, "", 96, Pal.Text, TextAnchor.UpperCenter, FontStyle.Bold);
            UI.Stretch(bannerTitle.rectTransform);
            UI.AddShadow(bannerTitle, 5);
            bannerSub = UI.Label(banner, "", 40, Pal.Text, TextAnchor.LowerCenter, FontStyle.Bold);
            UI.Stretch(bannerSub.rectTransform);
            UI.AddShadow(bannerSub, 3);
            bannerGroup.alpha = 0;

            flashText = UI.Label(Root, "", 48, Pal.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            UI.Place(flashText.rectTransform, new Vector2(0.5f, 0.38f), new Vector2(1000, 80), Vector2.zero);
            UI.AddShadow(flashText, 3);

            whiteFlash = UI.Img(Root, "White", new Color(1, 1, 1, 0));
            UI.Stretch(whiteFlash.rectTransform, -200, -200, -200, -200);
        }

        void RefreshSpecial()
        {
            var icon = specialRt.Find("Icon").GetComponent<Image>();
            int s = battle.SpecialIndex;
            specialRt.gameObject.SetActive(s >= 0);
            if (s >= 0) icon.sprite = Art.SpecialIcon(s);
        }

        // ------------------------------------------------------------------ API used by Battle
        public bool Blocks(Vector2 screenPos)
        {
            return (specialRt != null && specialRt.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(specialRt, screenPos, null))
                || (pauseRt != null && RectTransformUtility.RectangleContainsScreenPoint(pauseRt, screenPos, null))
                || pausePanel != null || G.DialogOpen;
        }

        public void Banner(string title, string sub, float duration, Color color, bool blink = false)
        {
            bannerTitle.text = title; bannerTitle.color = color;
            bannerSub.text = sub;
            bannerT = 0f; bannerDur = duration; bannerBlink = blink;
        }

        public void Flash(string msg)
        {
            flashText.text = msg;
            flashT = 1.3f;
        }

        public void WhiteFlash() { whiteT = 0.5f; }

        public void Number(Vector2 world, float value, bool crit)
        {
            if (!G.Settings.showDamageNumbers || nums.Count > 40) return;
            var t = freeNums.Count > 0 ? freeNums.Pop() : UI.Label(numbersRoot, "", 34, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            t.gameObject.SetActive(true);
            t.text = Mathf.RoundToInt(value).ToString() + (crit ? "!" : "");
            t.fontSize = crit ? 46 : 30;
            t.color = crit ? new Color(1f, 0.85f, 0.2f) : new Color(1f, 1f, 1f, 0.9f);
            t.rectTransform.sizeDelta = new Vector2(240, 60);
            var sp = G.Cam.WorldToScreenPoint(world);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(numbersRoot, sp, null, out var local);
            var n = new Num { t = t, life = 0.7f, pos = local + new Vector2(UnityEngine.Random.Range(-20f, 20f), 20f) };
            t.rectTransform.anchoredPosition = n.pos;
            nums.Add(n);
        }

        // ------------------------------------------------------------------ update
        void Update()
        {
            if (battle == null || battle.M.name == null) return;
            float dt = Time.unscaledDeltaTime;

            float h = battle.Hull01;
            hullFill.rectTransform.anchorMax = new Vector2(Mathf.Max(0.001f, h), 1);
            hullFill.color = h > 0.6f ? Pal.Green : h > 0.3f ? Pal.Gold : Pal.Red;
            dangerText.enabled = battle.Danger && Mathf.Repeat(Time.unscaledTime * 3f, 1f) > 0.35f;
            for (int i = 0; i < pips.Length; i++) pips[i].color = i < battle.Power ? new Color(0.35f, 0.75f, 1f) : Pal.Disabled;
            goldText.text = UI.Gold(battle.Gold);

            bool bossOn = battle.BossActive;
            bossBar.gameObject.SetActive(bossOn);
            progressBar.gameObject.SetActive(!bossOn && battle.State != Battle.Phase.Victory);
            progressFill.fillAmount = battle.Progress01;
            if (bossOn) { bossFill.fillAmount = battle.BossHp01; bossName.text = battle.M.bossName; }

            if (battle.SpecialIndex >= 0)
            {
                specialCdFill.fillAmount = battle.SpecialCd01;
                int ammo = battle.SpecialAmmo;
                specialAmmo.text = "×" + ammo;
                specialAmmo.color = ammo > 0 ? Pal.Text : Pal.Red;
            }

            // banner
            bannerT += dt;
            float a = bannerT < 0.25f ? bannerT / 0.25f : bannerT > bannerDur - 0.4f ? Mathf.Clamp01((bannerDur - bannerT) / 0.4f) : 1f;
            if (bannerBlink && bannerT < bannerDur - 0.4f) a *= Mathf.Repeat(Time.unscaledTime * 2.5f, 1f) > 0.3f ? 1f : 0.2f;
            bannerGroup.alpha = bannerT < bannerDur ? a : 0f;

            flashT -= dt;
            flashText.color = new Color(Pal.Gold.r, Pal.Gold.g, Pal.Gold.b, Mathf.Clamp01(flashT * 2f));

            whiteT -= dt;
            whiteFlash.color = new Color(1, 1, 1, Mathf.Clamp01(whiteT * 1.6f));

            for (int i = nums.Count - 1; i >= 0; i--)
            {
                var n = nums[i];
                n.life -= dt;
                n.pos += new Vector2(0, 90f * dt);
                n.t.rectTransform.anchoredPosition = n.pos;
                var c = n.t.color; c.a = Mathf.Clamp01(n.life * 2.5f); n.t.color = c;
                if (n.life <= 0f) { n.t.gameObject.SetActive(false); freeNums.Push(n.t); nums.RemoveAt(i); }
            }
        }

        // ------------------------------------------------------------------ pause
        void Pause()
        {
            if (pausePanel != null || battle == null || battle.State == Battle.Phase.Done) return;
            Time.timeScale = 0f;
            var dim = UI.Img(Root, "Pause", new Color(0, 0, 0, 0.7f));
            dim.raycastTarget = true;
            UI.Stretch(dim.rectTransform, -200, -200, -200, -200);
            pausePanel = dim.gameObject;
            var panel = UI.Box(dim.transform, "Panel", Pal.Panel);
            UI.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(900, 780), Vector2.zero);
            var title = UI.Label(panel.transform, "PAUSED", 70, Pal.Text, TextAnchor.UpperCenter, FontStyle.Bold);
            UI.Stretch(title.rectTransform, 0, 0, 0, 36);
            var sub = UI.Label(panel.transform, battle.M.code + " · " + battle.M.name, 32, Pal.TextDim, TextAnchor.UpperCenter);
            UI.Stretch(sub.rectTransform, 0, 0, 0, 120);

            AddSlider(panel.transform, "Music", G.Settings.musicVolume, v => { G.Settings.musicVolume = v; G.SaveSettings(); }, -200);
            AddSlider(panel.transform, "Sound", G.Settings.soundVolume, v => { G.Settings.soundVolume = v; G.SaveSettings(); }, -300);

            var resume = UI.Button(panel.transform, "RESUME", 46, Pal.Accent, Resume);
            UI.Place((RectTransform)resume.transform, new Vector2(0.5f, 0), new Vector2(560, 120), new Vector2(0, 190), new Vector2(0.5f, 0));
            var abort = UI.Button(panel.transform, "ABORT MISSION", 36, Pal.Red, () =>
                G.Dialog("Abort mission?", "You keep the gold you collected, but get no mission reward.", "ABORT", () => { Time.timeScale = 1f; battle.Abort(); }, "CANCEL"));
            UI.Place((RectTransform)abort.transform, new Vector2(0.5f, 0), new Vector2(560, 100), new Vector2(0, 50), new Vector2(0.5f, 0));
        }

        void AddSlider(Transform parent, string label, float value, UnityEngine.Events.UnityAction<float> onChange, float y)
        {
            var l = UI.Label(parent, label, 34, Pal.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            UI.Place(l.rectTransform, new Vector2(0.5f, 1), new Vector2(200, 60), new Vector2(-300, y), new Vector2(0, 0.5f));
            var s = UI.Slider(parent, value, onChange);
            UI.Place((RectTransform)s.transform, new Vector2(0.5f, 1), new Vector2(460, 60), new Vector2(-90, y), new Vector2(0, 0.5f));
        }

        void Resume()
        {
            if (pausePanel != null) Destroy(pausePanel);
            pausePanel = null;
            Time.timeScale = 1f;
        }
    }
}
