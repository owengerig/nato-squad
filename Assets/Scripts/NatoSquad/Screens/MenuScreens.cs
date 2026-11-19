using System;
using UnityEngine;
using UnityEngine.UI;

namespace NatoSquad
{
    /// <summary>Base for menu pages: a title bar with a back button and the player's gold.</summary>
    public abstract class MenuScreen : GameScreen
    {
        Text goldLabel;
        long shownGold = -1;

        protected RectTransform TopBar(string title, Action back)
        {
            var bar = UI.Img(Root, "TopBar", new Color(0, 0, 0, 0.35f));
            bar.rectTransform.anchorMin = new Vector2(0, 1); bar.rectTransform.anchorMax = new Vector2(1, 1);
            bar.rectTransform.pivot = new Vector2(0.5f, 1); bar.rectTransform.sizeDelta = new Vector2(0, 130);
            bar.rectTransform.anchoredPosition = Vector2.zero;
            if (back != null)
            {
                var b = UI.Button(bar.transform, "< BACK", 38, Pal.PanelLight, back);
                UI.Place((RectTransform)b.transform, new Vector2(0, 0.5f), new Vector2(230, 90), new Vector2(24, 0), new Vector2(0, 0.5f));
            }
            var t = UI.Label(bar.transform, title, 60, Pal.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            UI.Stretch(t.rectTransform, 300, 0, 300, 0);
            UI.AddShadow(t);
            var goldBox = UI.Box(bar.transform, "Gold", new Color(0, 0, 0, 0.4f));
            UI.Place(goldBox.rectTransform, new Vector2(1, 0.5f), new Vector2(360, 86), new Vector2(-24, 0), new Vector2(1, 0.5f));
            var coin = UI.Img(goldBox.transform, "Coin", Color.white, Art.Coin());
            UI.Place(coin.rectTransform, new Vector2(0, 0.5f), new Vector2(54, 54), new Vector2(16, 0), new Vector2(0, 0.5f));
            goldLabel = UI.Label(goldBox.transform, "", 40, Pal.Gold, TextAnchor.MiddleRight, FontStyle.Bold);
            UI.Stretch(goldLabel.rectTransform, 76, 0, 20, 0);
            return bar.rectTransform;
        }

        protected virtual void Update()
        {
            if (goldLabel != null && shownGold != G.Save.gold)
            {
                shownGold = G.Save.gold;
                goldLabel.text = UI.Gold(shownGold);
            }
        }

        /// <summary>The page area below the top bar.</summary>
        protected RectTransform Body(float margin = 30)
        {
            var b = UI.Rect("Body", Root);
            UI.Stretch(b, margin, margin, margin, 130 + margin * 0.5f);
            return b;
        }
    }

    // =========================================================================================================
    public class MainMenuScreen : MenuScreen
    {
        RectTransform jetRt;

        public override void Build()
        {
            G.Audio.PlayMusic(GameAudio.Track.Menu);

            // scenery
            var sky = UI.Img(Root, "Sky", Color.white, Art.Sky(new Color(0.10f, 0.20f, 0.42f), new Color(0.95f, 0.62f, 0.38f)));
            UI.Stretch(sky.rectTransform, -300, -300, -300, -300);
            var far = UI.Img(Root, "Far", new Color(0.18f, 0.20f, 0.30f), Art.Far(Terrain.Mountains, Color.white));
            far.rectTransform.anchorMin = new Vector2(0, 0); far.rectTransform.anchorMax = new Vector2(1, 0);
            far.rectTransform.pivot = new Vector2(0.5f, 0); far.rectTransform.sizeDelta = new Vector2(600, 420);
            far.rectTransform.anchoredPosition = new Vector2(0, -60);
            for (int i = 0; i < 3; i++)
            {
                var c = UI.Img(Root, "Cloud", new Color(1, 1, 1, 0.55f), Art.Cloud(i, false));
                UI.Place(c.rectTransform, new Vector2(0.2f + i * 0.3f, 0.55f + (i % 2) * 0.15f), new Vector2(520, 228), Vector2.zero);
            }

            // title
            var title = UI.Label(Root, "NATO SQUAD", 170, Pal.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            UI.Place(title.rectTransform, new Vector2(0.5f, 1), new Vector2(1600, 200), new Vector2(0, -110), new Vector2(0.5f, 1));
            UI.AddShadow(title, 7);
            var sub = UI.Label(Root, "A L L I E D   A I R   S T R I K E", 40, new Color(1f, 0.85f, 0.55f), TextAnchor.MiddleCenter, FontStyle.Bold);
            UI.Place(sub.rectTransform, new Vector2(0.5f, 1), new Vector2(1600, 60), new Vector2(0, -305), new Vector2(0.5f, 1));
            UI.AddShadow(sub, 3);

            // the selected jet flying across
            var jet = UI.Img(Root, "Jet", Color.white, Art.Jet(G.Save.selectedJet));
            jetRt = jet.rectTransform;
            UI.Place(jetRt, new Vector2(0.27f, 0.42f), new Vector2(560, 210), Vector2.zero);
            var flame = UI.Img(jet.transform, "Flame", Color.white, Art.Flame());
            UI.Place(flame.rectTransform, new Vector2(0, 0.5f), new Vector2(150, 60), new Vector2(20, -6), new Vector2(1, 0.5f));

            // menu buttons
            var col = UI.Rect("Buttons", Root);
            UI.Place(col, new Vector2(0.74f, 0.4f), new Vector2(620, 600), Vector2.zero);
            AddMenuButton(col, "SORTIE", Pal.Accent, 0, () => G.Show<MissionSelectScreen>(), 64);
            AddMenuButton(col, "HANGAR", Pal.PanelLight, 1, () => G.Show<HangarScreen>());
            AddMenuButton(col, "STORE", new Color(0.75f, 0.52f, 0.12f), 2, () => G.Show<StoreScreen>());
            AddMenuButton(col, "SETTINGS", Pal.PanelLight, 3, () => G.Show<SettingsScreen>());

            // gold
            var goldBox = UI.Box(Root, "Gold", new Color(0, 0, 0, 0.4f));
            UI.Place(goldBox.rectTransform, new Vector2(1, 1), new Vector2(360, 86), new Vector2(-24, -24));
            var coin = UI.Img(goldBox.transform, "Coin", Color.white, Art.Coin());
            UI.Place(coin.rectTransform, new Vector2(0, 0.5f), new Vector2(54, 54), new Vector2(16, 0), new Vector2(0, 0.5f));
            var gold = UI.Label(goldBox.transform, UI.Gold(G.Save.gold), 40, Pal.Gold, TextAnchor.MiddleRight, FontStyle.Bold);
            UI.Stretch(gold.rectTransform, 76, 0, 20, 0);

            // small credit + version at the bottom of the main screen
            var credit = UI.Label(Root, "Creator " + Links.Creator + "   " + GameVersion.Label, 24, new Color(1, 1, 1, 0.75f), TextAnchor.LowerLeft);
            UI.Place(credit.rectTransform, new Vector2(0, 0), new Vector2(800, 40), new Vector2(24, 14));
            UI.AddShadow(credit, 2);
        }

        void AddMenuButton(RectTransform col, string label, Color c, int i, Action a, int size = 52)
        {
            var b = UI.Button(col, label, size, c, a);
            UI.Place((RectTransform)b.transform, new Vector2(0.5f, 1), new Vector2(560, i == 0 ? 150 : 115), new Vector2(0, i == 0 ? 0 : -45 - i * 130), new Vector2(0.5f, 1));
            UI.AddShadow(b.GetComponent<Image>(), 5);
        }

        protected override void Update()
        {
            base.Update();
            if (jetRt != null)
                jetRt.anchoredPosition = new Vector2(Mathf.Sin(Time.time * 0.7f) * 40f, Mathf.Sin(Time.time * 1.3f) * 22f);
        }

        public override void OnBack()
        {
#if UNITY_ANDROID
            G.Dialog("Quit NATO Squad?", "Your progress is saved.", "QUIT", Application.Quit, "CANCEL");
#endif
        }
    }

    // =========================================================================================================
    public class MissionSelectScreen : MenuScreen
    {
        int selected;
        Text briefTitle, briefText, briefInfo;
        Button launch;
        Image[] cards;

        public override void Build()
        {
            G.Audio.PlayMusic(GameAudio.Track.Menu);
            TopBar("SELECT OPERATION", () => G.Show<MainMenuScreen>());
            var body = Body();

            // 8 operation cards in two rows (left), briefing (right)
            var grid = UI.Rect("Grid", body);
            grid.anchorMin = new Vector2(0, 0); grid.anchorMax = new Vector2(0.62f, 1); grid.offsetMin = grid.offsetMax = Vector2.zero;
            cards = new Image[Missions.Count];
            selected = 0;
            for (int i = 0; i < Missions.Count; i++) if (G.Save.Unlocked(i)) selected = i;
            for (int i = 0; i < Missions.Count; i++)
            {
                int idx = i;
                var m = Missions.All[i];
                bool unlocked = G.Save.Unlocked(i);
                var card = UI.Box(grid, "Card", Pal.Panel);
                card.raycastTarget = true;
                int r = i / 4, c = i % 4;
                card.rectTransform.anchorMin = new Vector2(c * 0.25f, r == 0 ? 0.5f : 0f);
                card.rectTransform.anchorMax = new Vector2((c + 1) * 0.25f, r == 0 ? 1f : 0.5f);
                card.rectTransform.offsetMin = new Vector2(10, 10); card.rectTransform.offsetMax = new Vector2(-10, -10);
                var btn = card.gameObject.AddComponent<Button>();
                btn.targetGraphic = card;
                btn.onClick.AddListener(() => { G.Audio.Play(Sfx.Click); Select(idx); });
                cards[i] = card;

                var thumb = UI.Img(card.transform, "Sky", Color.white, Art.Sky(m.skyTop, m.skyBottom));
                thumb.rectTransform.anchorMin = new Vector2(0, 0.42f); thumb.rectTransform.anchorMax = new Vector2(1, 1);
                thumb.rectTransform.offsetMin = new Vector2(10, 0); thumb.rectTransform.offsetMax = new Vector2(-10, -10);
                var gnd = UI.Img(thumb.transform, "Ground", Color.white, Art.GroundStrip(m));
                gnd.rectTransform.anchorMin = Vector2.zero; gnd.rectTransform.anchorMax = new Vector2(1, 0.3f);
                gnd.rectTransform.offsetMin = gnd.rectTransform.offsetMax = Vector2.zero;
                var bossImg = UI.Img(thumb.transform, "Boss", unlocked ? Color.white : new Color(0, 0, 0, 0.85f), Art.Boss(m.boss));
                bossImg.preserveAspect = true;
                UI.Stretch(bossImg.rectTransform, 30, 10, 30, 14);

                var code = UI.Label(card.transform, m.code, 30, Pal.TextDim, TextAnchor.UpperCenter, FontStyle.Bold);
                code.rectTransform.anchorMin = new Vector2(0, 0); code.rectTransform.anchorMax = new Vector2(1, 0.42f);
                code.rectTransform.offsetMin = new Vector2(6, 0); code.rectTransform.offsetMax = new Vector2(-6, -6);
                var name = UI.Label(card.transform, unlocked ? m.name : "LOCKED", 34, unlocked ? Pal.Text : Pal.TextDim, TextAnchor.MiddleCenter, FontStyle.Bold);
                name.rectTransform.anchorMin = new Vector2(0, 0); name.rectTransform.anchorMax = new Vector2(1, 0.42f);
                name.rectTransform.offsetMin = new Vector2(6, 30); name.rectTransform.offsetMax = new Vector2(-6, -30);
                int stars = G.Save.Stars(i);
                if (unlocked) UI.Place(UI.Stars(card.transform, stars, 34), new Vector2(0.5f, 0), new Vector2(130, 34), new Vector2(0, 10), new Vector2(0.5f, 0));
            }

            // briefing panel
            var brief = UI.Box(body, "Briefing", Pal.Panel);
            brief.rectTransform.anchorMin = new Vector2(0.63f, 0); brief.rectTransform.anchorMax = new Vector2(1, 1);
            brief.rectTransform.offsetMin = new Vector2(10, 10); brief.rectTransform.offsetMax = new Vector2(-10, -10);
            briefTitle = UI.Label(brief.transform, "", 50, Pal.Text, TextAnchor.UpperLeft, FontStyle.Bold);
            UI.Stretch(briefTitle.rectTransform, 36, 0, 36, 30);
            briefText = UI.Label(brief.transform, "", 32, Pal.TextDim, TextAnchor.UpperLeft);
            UI.Stretch(briefText.rectTransform, 36, 300, 36, 120);
            briefInfo = UI.Label(brief.transform, "", 30, Pal.Text, TextAnchor.UpperLeft);
            UI.Stretch(briefInfo.rectTransform, 36, 170, 36, 420);
            launch = UI.Button(brief.transform, "LAUNCH", 56, Pal.Accent, Launch);
            UI.Place((RectTransform)launch.transform, new Vector2(0.5f, 0), new Vector2(520, 130), new Vector2(0, 30), new Vector2(0.5f, 0));

            Select(selected);
        }

        void Select(int i)
        {
            selected = i;
            for (int k = 0; k < cards.Length; k++)
                cards[k].color = k == i ? Pal.AccentDark : G.Save.Unlocked(k) ? Pal.Panel : new Color(0.07f, 0.09f, 0.12f, 0.95f);
            var m = Missions.All[i];
            bool unlocked = G.Save.Unlocked(i);
            briefTitle.text = m.code + "  " + m.name.ToUpper();
            briefText.text = unlocked ? m.briefing : "Clear " + Missions.All[i - 1].code + " " + Missions.All[i - 1].name + " to unlock this operation.";
            var jet = Jets.All[G.Save.selectedJet];
            string special = G.Save.equippedSpecial >= 0 ? Specials.All[G.Save.equippedSpecial].name + " ×" + G.Save.Ammo(G.Save.equippedSpecial) : "none";
            long reward = G.Save.Cleared(i) ? M(i).reward / 2 : M(i).reward;
            briefInfo.text = unlocked
                ? "Target: <b>" + m.bossName + "</b>\nReward: <color=#FFCC40>" + UI.Gold(reward) + " gold</color>" + (G.Save.Cleared(i) ? " (replay)" : " (first clear)") +
                  "\nAircraft: " + jet.name + "\nSpecial: " + special + "\nPower rating: " + Loadout.Rating(G.Save)
                : "";
            UI.SetInteractable(launch, unlocked);
        }

        static MissionDef M(int i) => Missions.All[i];

        void Launch()
        {
            if (!G.Save.Unlocked(selected)) return;
            G.PendingMission = selected;
            G.Show<BattleScreen>();
        }

        public override void OnBack() => G.Show<MainMenuScreen>();
    }

    // =========================================================================================================
    public class ResultScreen : MenuScreen
    {
        public override void Build()
        {
            var r = G.LastResult ?? new BattleResult();
            var m = Missions.All[r.mission];
            var panel = UI.Box(Root, "Panel", Pal.Panel);
            UI.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(1200, 900), Vector2.zero);

            string head = r.won ? "MISSION COMPLETE" : r.aborted ? "MISSION ABORTED" : "MISSION FAILED";
            var t = UI.Label(panel.transform, head, 80, r.won ? Pal.Gold : Pal.Red, TextAnchor.UpperCenter, FontStyle.Bold);
            UI.Stretch(t.rectTransform, 0, 0, 0, 40);
            UI.AddShadow(t, 4);
            var sub = UI.Label(panel.transform, m.code + " · " + m.name, 38, Pal.TextDim, TextAnchor.UpperCenter);
            UI.Stretch(sub.rectTransform, 0, 0, 0, 140);
            if (r.won)
            {
                UI.Place(UI.Stars(panel.transform, r.stars, 80), new Vector2(0.5f, 1), new Vector2(300, 80), new Vector2(0, -205), new Vector2(0.5f, 1));
            }

            string lines =
                "Enemies destroyed\t" + r.kills + "\n" +
                "Hull damage taken\t" + Mathf.RoundToInt(r.hullLost * 100f) + "%\n" +
                "Gold collected\t" + UI.Gold(r.goldCollected) + "\n" +
                (r.won ? "Mission reward" + (r.firstClear ? " (first clear)" : " (replay)") + "\t" + UI.Gold(r.missionReward) + "\n" : "") +
                (G.Save.doubleGold ? "Double Gold Pay\tactive\n" : "");
            var left = UI.Label(panel.transform, "", 38, Pal.Text, TextAnchor.UpperLeft);
            var right = UI.Label(panel.transform, "", 38, Pal.Text, TextAnchor.UpperRight, FontStyle.Bold);
            var ls = new System.Text.StringBuilder(); var rs = new System.Text.StringBuilder();
            foreach (var line in lines.Split('\n'))
            {
                if (line.Length == 0) continue;
                var parts = line.Split('\t');
                ls.AppendLine(parts[0]); rs.AppendLine(parts.Length > 1 ? parts[1] : "");
            }
            left.text = ls.ToString(); right.text = rs.ToString();
            UI.Stretch(left.rectTransform, 120, 260, 120, 330);
            UI.Stretch(right.rectTransform, 120, 260, 120, 330);

            var total = UI.Label(panel.transform, "TOTAL  +" + UI.Gold(r.Total) + " GOLD", 56, Pal.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            UI.Place(total.rectTransform, new Vector2(0.5f, 0), new Vector2(1100, 90), new Vector2(0, 175), new Vector2(0.5f, 0));

            bool next = r.won && r.mission + 1 < Missions.Count;
            var again = UI.Button(panel.transform, r.won ? "REPLAY" : "RETRY", 44, Pal.PanelLight, () => { G.PendingMission = r.mission; G.Show<BattleScreen>(); });
            UI.Place((RectTransform)again.transform, new Vector2(0.5f, 0), new Vector2(330, 110), new Vector2(-370, 40), new Vector2(0.5f, 0));
            var hangar = UI.Button(panel.transform, "HANGAR", 44, Pal.PanelLight, () => G.Show<HangarScreen>());
            UI.Place((RectTransform)hangar.transform, new Vector2(0.5f, 0), new Vector2(330, 110), new Vector2(0, 40), new Vector2(0.5f, 0));
            var cont = UI.Button(panel.transform, next ? "NEXT OP" : "CONTINUE", 44, Pal.Accent, () =>
            {
                if (next) G.PendingMission = r.mission + 1;
                G.Show<MissionSelectScreen>();
            });
            UI.Place((RectTransform)cont.transform, new Vector2(0.5f, 0), new Vector2(330, 110), new Vector2(370, 40), new Vector2(0.5f, 0));

            if (r.won && r.mission == Missions.Count - 1 && r.firstClear)
                G.Dialog("VICTORY", "The Black Citadel has fallen and the war is over.\nReplay any operation to keep upgrading your squad.", "OUTSTANDING");
            else if (!r.won && !r.aborted && G.Save.sorties <= 3)
                G.Dialog("Tip", "Spend gold in the HANGAR on upgrades and modules, and stock up on special weapons in the ARMORY.", "OK");
        }

        public override void OnBack() => G.Show<MissionSelectScreen>();
    }

    // =========================================================================================================
    public class SettingsScreen : MenuScreen
    {
        public override void Build()
        {
            TopBar("SETTINGS", () => G.Show<MainMenuScreen>());
            var body = Body();
            var panel = UI.Box(body, "Panel", Pal.Panel);
            panel.rectTransform.anchorMin = new Vector2(0.12f, 0); panel.rectTransform.anchorMax = new Vector2(0.88f, 1);
            panel.rectTransform.offsetMin = panel.rectTransform.offsetMax = Vector2.zero;
            var s = G.Settings;
            float y = -60;
            Slider(panel.transform, "Music volume", s.musicVolume, v => { s.musicVolume = v; G.SaveSettings(); }, ref y);
            Slider(panel.transform, "Sound volume", s.soundVolume, v => { s.soundVolume = v; G.SaveSettings(); }, ref y);
            Slider(panel.transform, "Touch sensitivity", Mathf.InverseLerp(0.5f, 2.5f, s.sensitivity), v => { s.sensitivity = Mathf.Lerp(0.5f, 2.5f, v); G.SaveSettings(); }, ref y);
            Toggle(panel.transform, "Vibration", s.vibration, v => { s.vibration = v; G.SaveSettings(); }, ref y);
            Toggle(panel.transform, "Damage numbers", s.showDamageNumbers, v => { s.showDamageNumbers = v; G.SaveSettings(); }, ref y);
            Toggle(panel.transform, "Screen shake", s.screenShake, v => { s.screenShake = v; G.SaveSettings(); }, ref y);

            // links and data
            var row = UI.Rect("Links", panel.transform);
            UI.Place(row, new Vector2(0.5f, 0), new Vector2(1300, 110), new Vector2(0, 150), new Vector2(0.5f, 0));
            var privacy = UI.Button(row, "PRIVACY POLICY", 34, Pal.PanelLight, () => Application.OpenURL(Links.PrivacyPolicy));
            UI.Place((RectTransform)privacy.transform, new Vector2(0, 0.5f), new Vector2(410, 100), Vector2.zero, new Vector2(0, 0.5f));
            var support = UI.Button(row, "SUPPORT", 34, Pal.PanelLight, () => Application.OpenURL(Links.Support));
            UI.Place((RectTransform)support.transform, new Vector2(0.5f, 0.5f), new Vector2(410, 100), Vector2.zero);
            var restore = UI.Button(row, "RESTORE PURCHASES", 30, Pal.PanelLight, () => G.Store.Restore());
            UI.Place((RectTransform)restore.transform, new Vector2(1, 0.5f), new Vector2(410, 100), Vector2.zero, new Vector2(1, 0.5f));

            var reset = UI.Button(panel.transform, "RESET PROGRESS", 30, new Color(0.45f, 0.12f, 0.12f), () =>
                G.Dialog("Reset all progress?", "This deletes your gold, upgrades, modules and mission stars on this device. Purchases of Double Gold Pay can be restored.", "RESET", () =>
                {
                    SaveSystem.Delete();
                    G.Save = SaveSystem.Load();
                    G.SaveGame();
                    G.Toast("Progress reset");
                    G.Show<MainMenuScreen>();
                }, "CANCEL"));
            UI.Place((RectTransform)reset.transform, new Vector2(0.5f, 0), new Vector2(420, 90), new Vector2(0, 40), new Vector2(0.5f, 0));

            var info = UI.Label(panel.transform, "NATO Squad " + GameVersion.LongLabel + "  ·  Creator " + Links.Creator, 24, Pal.TextDim, TextAnchor.LowerRight);
            UI.Stretch(info.rectTransform, 20, 14, 24, 0);
        }

        void Slider(Transform p, string label, float value, UnityEngine.Events.UnityAction<float> on, ref float y)
        {
            var l = UI.Label(p, label, 38, Pal.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            UI.Place(l.rectTransform, new Vector2(0, 1), new Vector2(600, 80), new Vector2(80, y), new Vector2(0, 1));
            var s = UI.Slider(p, value, on);
            UI.Place((RectTransform)s.transform, new Vector2(1, 1), new Vector2(620, 80), new Vector2(-80, y), new Vector2(1, 1));
            y -= 100;
        }

        void Toggle(Transform p, string label, bool value, Action<bool> on, ref float y)
        {
            var l = UI.Label(p, label, 38, Pal.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            UI.Place(l.rectTransform, new Vector2(0, 1), new Vector2(600, 80), new Vector2(80, y), new Vector2(0, 1));
            bool state = value;
            Button b = null;
            b = UI.Button(p, state ? "ON" : "OFF", 38, state ? Pal.Green : Pal.Disabled, () =>
            {
                state = !state;
                on(state);
                UI.BtnLabel(b).text = state ? "ON" : "OFF";
                b.GetComponent<Image>().color = state ? Pal.Green : Pal.Disabled;
            });
            UI.Place((RectTransform)b.transform, new Vector2(1, 1), new Vector2(220, 80), new Vector2(-80, y), new Vector2(1, 1));
            y -= 100;
        }

        public override void OnBack() => G.Show<MainMenuScreen>();
    }
}
