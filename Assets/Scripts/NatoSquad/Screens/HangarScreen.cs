using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace NatoSquad
{
    /// <summary>
    /// Hangar: AIRCRAFT (choose / buy jets), UPGRADES (permanent tracks), MODULES (rarity-tier gear: equip, level up,
    /// fuse, salvage) and ARMORY (buy and equip special-weapon ammo).
    /// </summary>
    public class HangarScreen : MenuScreen
    {
        static int tab;            // remembered while the game runs
        static int moduleSlot;
        RectTransform content;
        Button[] tabButtons;
        Text rating;
        Vector2 scrollPos;
        RectTransform lastScroll;

        static readonly string[] TabNames = { "AIRCRAFT", "UPGRADES", "MODULES", "ARMORY" };

        public override void Build()
        {
            G.Audio.PlayMusic(GameAudio.Track.Menu);
            TopBar("HANGAR", () => G.Show<MainMenuScreen>());
            var body = Body(24);

            var tabs = UI.Rect("Tabs", body);
            tabs.anchorMin = new Vector2(0, 1); tabs.anchorMax = new Vector2(1, 1); tabs.pivot = new Vector2(0.5f, 1);
            tabs.sizeDelta = new Vector2(0, 96); tabs.anchoredPosition = Vector2.zero;
            tabButtons = new Button[TabNames.Length];
            for (int i = 0; i < TabNames.Length; i++)
            {
                int idx = i;
                tabButtons[i] = UI.Button(tabs, TabNames[i], 38, Pal.PanelLight, () => { tab = idx; Rebuild(true); });
                var rt = (RectTransform)tabButtons[i].transform;
                rt.anchorMin = new Vector2(i * 0.19f, 0); rt.anchorMax = new Vector2(i * 0.19f + 0.18f, 1);
                rt.offsetMin = rt.offsetMax = Vector2.zero;
            }
            var ratingBox = UI.Box(tabs, "Rating", new Color(0, 0, 0, 0.35f));
            ratingBox.rectTransform.anchorMin = new Vector2(0.77f, 0); ratingBox.rectTransform.anchorMax = new Vector2(1, 1);
            ratingBox.rectTransform.offsetMin = ratingBox.rectTransform.offsetMax = Vector2.zero;
            rating = UI.Label(ratingBox.transform, "", 32, Pal.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            UI.Stretch(rating.rectTransform);

            content = UI.Rect("Content", body);
            UI.Stretch(content, 0, 0, 0, 116);
            Rebuild(true);
        }

        public override void OnBack() => G.Show<MainMenuScreen>();

        /// <summary>Rebuilds the current tab. Keeps the list's scroll position unless the tab changed.</summary>
        void Rebuild(bool resetScroll = false)
        {
            if (!resetScroll && lastScroll != null) scrollPos = lastScroll.anchoredPosition;
            else scrollPos = Vector2.zero;
            lastScroll = null;
            for (int i = content.childCount - 1; i >= 0; i--) Destroy(content.GetChild(i).gameObject);
            for (int i = 0; i < tabButtons.Length; i++) tabButtons[i].GetComponent<Image>().color = i == tab ? Pal.Accent : Pal.PanelLight;
            rating.text = "POWER RATING  " + Loadout.Rating(G.Save);
            switch (tab)
            {
                case 0: BuildAircraft(); break;
                case 1: BuildUpgrades(); break;
                case 2: BuildModules(); break;
                default: BuildArmory(); break;
            }
            if (lastScroll != null) lastScroll.anchoredPosition = scrollPos;
        }

        RectTransform ScrollList(Transform parent)
        {
            var c = UI.Scroll(parent, "List", 12, 12);
            lastScroll = c;
            return c;
        }

        // ================================================================= AIRCRAFT
        void BuildAircraft()
        {
            var save = G.Save;
            for (int i = 0; i < Jets.All.Length; i++)
            {
                int idx = i;
                var j = Jets.All[i];
                bool owned = save.ownedJets.Contains(i);
                bool selected = save.selectedJet == i;
                var card = UI.Box(content, "Jet", selected ? Pal.AccentDark : Pal.Panel);
                card.rectTransform.anchorMin = new Vector2(i / 3f, 0); card.rectTransform.anchorMax = new Vector2((i + 1) / 3f, 1);
                card.rectTransform.offsetMin = new Vector2(10, 0); card.rectTransform.offsetMax = new Vector2(-10, 0);

                var img = UI.Img(card.transform, "Image", Color.white, Art.Jet(i));
                img.preserveAspect = true;
                UI.Place(img.rectTransform, new Vector2(0.5f, 1), new Vector2(460, 172), new Vector2(0, -20), new Vector2(0.5f, 1));
                var name = UI.Label(card.transform, j.name, 44, Pal.Text, TextAnchor.UpperCenter, FontStyle.Bold);
                UI.Stretch(name.rectTransform, 10, 0, 10, 200);
                var role = UI.Label(card.transform, j.role.ToUpper() + "\n<color=#9fb0c8>" + j.pilot + " \"" + j.callsign + "\" · " + j.nation + "</color>", 26, Pal.Gold, TextAnchor.UpperCenter, FontStyle.Bold);
                UI.Stretch(role.rectTransform, 10, 0, 10, 255);
                var desc = UI.Label(card.transform, j.desc, 26, Pal.TextDim, TextAnchor.UpperCenter);
                UI.Stretch(desc.rectTransform, 30, 0, 30, 340);

                float y = 300;
                Bar(card.transform, "Firepower", j.damage / j.fireInterval / 95f, ref y);
                Bar(card.transform, "Hull", j.hull / 145f, ref y);
                Bar(card.transform, "Speed", j.speed / 9f, ref y);
                Bar(card.transform, "Repair", j.regen / 10f, ref y);

                string label = selected ? "SELECTED" : owned ? "SELECT" : "BUY  " + UI.Gold(j.price);
                var b = UI.Button(card.transform, label, 38, selected ? Pal.Disabled : owned ? Pal.Accent : new Color(0.75f, 0.52f, 0.12f), () =>
                {
                    if (save.selectedJet == idx) return;
                    if (!save.ownedJets.Contains(idx))
                    {
                        G.Dialog("Buy " + j.name + "?", j.role + " flown by " + j.pilot + ".\nPrice: " + UI.Gold(j.price) + " gold", "BUY", () =>
                        {
                            if (!G.Spend(j.price)) return;
                            save.ownedJets.Add(idx); save.selectedJet = idx; G.SaveGame();
                            G.Toast(j.name + " joins the squad!", Pal.Gold);
                            Rebuild();
                        }, "CANCEL");
                        return;
                    }
                    save.selectedJet = idx; G.SaveGame(); Rebuild();
                });
                UI.Place((RectTransform)b.transform, new Vector2(0.5f, 0), new Vector2(400, 96), new Vector2(0, 24), new Vector2(0.5f, 0));
            }
        }

        void Bar(Transform parent, string label, float v, ref float y)
        {
            var l = UI.Label(parent, label, 26, Pal.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            UI.Place(l.rectTransform, new Vector2(0, 0), new Vector2(170, 40), new Vector2(36, y), new Vector2(0, 0.5f));
            var bg = UI.Box(parent, "Bar", new Color(0, 0, 0, 0.4f));
            bg.rectTransform.anchorMin = new Vector2(0, 0); bg.rectTransform.anchorMax = new Vector2(1, 0);
            bg.rectTransform.pivot = new Vector2(0, 0.5f);
            bg.rectTransform.offsetMin = new Vector2(210, y - 12); bg.rectTransform.offsetMax = new Vector2(-36, y + 12);
            var f = UI.Img(bg.transform, "Fill", Pal.Accent);
            f.rectTransform.anchorMin = Vector2.zero; f.rectTransform.anchorMax = new Vector2(Mathf.Clamp(v, 0.05f, 1f), 1);
            f.rectTransform.offsetMin = new Vector2(3, 3); f.rectTransform.offsetMax = new Vector2(-3, -3);
            y -= 46;
        }

        // ================================================================= UPGRADES
        void BuildUpgrades()
        {
            var list = ScrollList(content);
            UI.Stretch((RectTransform)list.parent);
            var save = G.Save;
            for (int i = 0; i < Tracks.All.Length; i++)
            {
                int idx = i;
                var t = Tracks.All[i];
                int lv = save.Track(i);
                bool max = lv >= t.maxLevel;
                var row = UI.Row(list, 118, Pal.Panel);

                var name = UI.Label(row, t.name, 38, Pal.Text, TextAnchor.UpperLeft, FontStyle.Bold);
                UI.Stretch(name.rectTransform, 30, 0, 900, 14);
                var desc = UI.Label(row, t.desc, 26, Pal.TextDim, TextAnchor.LowerLeft);
                UI.Stretch(desc.rectTransform, 30, 16, 900, 0);

                // level pips
                var pips = UI.Rect("Pips", row);
                pips.anchorMin = new Vector2(0.36f, 0.5f); pips.anchorMax = new Vector2(0.36f, 0.5f);
                for (int k = 0; k < t.maxLevel; k++)
                {
                    var pip = UI.Box(pips, "Pip", k < lv ? Pal.Accent : new Color(1, 1, 1, 0.12f));
                    float w = Mathf.Min(30f, 450f / t.maxLevel);
                    UI.Place(pip.rectTransform, new Vector2(0, 0.5f), new Vector2(w - 5, 34), new Vector2(k * w, 14), new Vector2(0, 0.5f));
                }
                var cur = UI.Label(row, (lv > 0 ? StatInfo.Format(t.stat, t.perLevel * lv) : "Not upgraded") + (max ? "" : "   <color=#7fd17f>next: " + StatInfo.Format(t.stat, t.perLevel * (lv + 1)) + "</color>"),
                    24, Pal.TextDim, TextAnchor.LowerLeft);
                cur.rectTransform.anchorMin = new Vector2(0.36f, 0); cur.rectTransform.anchorMax = new Vector2(0.78f, 0.5f);
                cur.rectTransform.offsetMin = new Vector2(0, 10); cur.rectTransform.offsetMax = Vector2.zero;
                var lvl = UI.Label(row, "LV " + lv + "/" + t.maxLevel, 28, Pal.Text, TextAnchor.UpperRight, FontStyle.Bold);
                lvl.rectTransform.anchorMin = new Vector2(0.6f, 0.5f); lvl.rectTransform.anchorMax = new Vector2(0.78f, 1);
                lvl.rectTransform.offsetMin = Vector2.zero; lvl.rectTransform.offsetMax = new Vector2(0, -10);

                int cost = max ? 0 : Tracks.Cost(i, lv);
                var b = UI.Button(row, max ? "MAX" : UI.Gold(cost), 36, max ? Pal.Disabled : save.gold >= cost ? new Color(0.75f, 0.52f, 0.12f) : Pal.Disabled, () =>
                {
                    if (save.Track(idx) >= Tracks.All[idx].maxLevel) return;
                    if (!G.Spend(Tracks.Cost(idx, save.Track(idx)))) return;
                    save.SetTrack(idx, save.Track(idx) + 1);
                    G.SaveGame();
                    Rebuild();
                });
                UI.Place((RectTransform)b.transform, new Vector2(1, 0.5f), new Vector2(300, 86), new Vector2(-20, 0), new Vector2(1, 0.5f));
                if (!max)
                {
                    var coin = UI.Img(b.transform, "Coin", Color.white, Art.Coin());
                    UI.Place(coin.rectTransform, new Vector2(0, 0.5f), new Vector2(40, 40), new Vector2(18, 0), new Vector2(0, 0.5f));
                }
            }
        }

        // ================================================================= MODULES
        void BuildModules()
        {
            var save = G.Save;
            // slots (left)
            var left = UI.Box(content, "Slots", Pal.Panel);
            left.rectTransform.anchorMin = Vector2.zero; left.rectTransform.anchorMax = new Vector2(0.36f, 1);
            left.rectTransform.offsetMin = Vector2.zero; left.rectTransform.offsetMax = new Vector2(-10, 0);
            for (int s = 0; s < Modules.SlotCount; s++)
            {
                int slot = s;
                var m = save.Equipped(s);
                var row = UI.Box(left.transform, "Slot", s == moduleSlot ? Pal.AccentDark : new Color(0, 0, 0, 0.3f));
                row.raycastTarget = true;
                row.rectTransform.anchorMin = new Vector2(0, 1 - (s + 1) / 7f); row.rectTransform.anchorMax = new Vector2(1, 1 - s / 7f);
                row.rectTransform.offsetMin = new Vector2(12, 5); row.rectTransform.offsetMax = new Vector2(-12, -5);
                var btn = row.gameObject.AddComponent<Button>();
                btn.targetGraphic = row;
                btn.onClick.AddListener(() => { G.Audio.Play(Sfx.Click); moduleSlot = slot; Rebuild(true); });
                var icon = UI.Img(row.transform, "Icon", m != null ? RarityInfo.Col(m.rarity) : new Color(1, 1, 1, 0.3f), Art.SlotIcon(s));
                UI.Place(icon.rectTransform, new Vector2(0, 0.5f), new Vector2(64, 64), new Vector2(14, 0), new Vector2(0, 0.5f));
                var txt = UI.Label(row.transform, Modules.SlotNames[s].ToUpper() + "\n" +
                    (m != null ? "<color=#" + ColorUtility.ToHtmlStringRGB(RarityInfo.Col(m.rarity)) + ">" + Modules.Name(m) + "</color>  Lv " + m.level : "<color=#8090a0>empty</color>"),
                    26, Pal.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
                UI.Stretch(txt.rectTransform, 92, 0, 10, 0);
            }
            var bonuses = UI.Button(left.transform, "ALL BONUSES", 30, Pal.PanelLight, ShowBonuses);
            var brt = (RectTransform)bonuses.transform;
            brt.anchorMin = new Vector2(0, 0); brt.anchorMax = new Vector2(1, 1 / 7f);
            brt.offsetMin = new Vector2(12, 10); brt.offsetMax = new Vector2(-12, -6);

            // inventory (right)
            var right = UI.Rect("Inventory", content);
            right.anchorMin = new Vector2(0.36f, 0); right.anchorMax = Vector2.one; right.offsetMin = new Vector2(10, 0); right.offsetMax = Vector2.zero;

            var header = UI.Rect("Header", right);
            header.anchorMin = new Vector2(0, 1); header.anchorMax = new Vector2(1, 1); header.pivot = new Vector2(0.5f, 1);
            header.sizeDelta = new Vector2(0, 86); header.anchoredPosition = Vector2.zero;
            var owned = save.modules.Where(x => x.slot == moduleSlot).ToList();
            var htxt = UI.Label(header, Modules.SlotNames[moduleSlot].ToUpper() + " MODULES  (" + owned.Count + ")", 34, Pal.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            UI.Stretch(htxt.rectTransform, 10, 0, 700, 0);

            // fuse: lowest rarity with 3 spare modules
            int fuseRarity = -1;
            for (int r = 0; r < RarityInfo.Count - 1 && fuseRarity < 0; r++) if (Modules.FuseCandidates(save, moduleSlot, r).Count >= 3) fuseRarity = r;
            if (fuseRarity >= 0)
            {
                int fr = fuseRarity;
                var fuse = UI.Button(header, "FUSE 3 " + RarityInfo.Name(fr).ToUpper() + " > " + RarityInfo.Name(fr + 1).ToUpper(), 26, new Color(0.45f, 0.25f, 0.65f), () => Fuse(fr));
                UI.Place((RectTransform)fuse.transform, new Vector2(1, 0.5f), new Vector2(430, 74), new Vector2(-290, 0), new Vector2(1, 0.5f));
            }
            var crates = UI.Button(header, "GET MORE", 28, new Color(0.75f, 0.52f, 0.12f), () => G.Show<StoreScreen>());
            UI.Place((RectTransform)crates.transform, new Vector2(1, 0.5f), new Vector2(270, 74), new Vector2(0, 0), new Vector2(1, 0.5f));

            var listRoot = UI.Rect("ListRoot", right);
            UI.Stretch(listRoot, 0, 0, 0, 96);
            var list = ScrollList(listRoot);
            UI.Stretch((RectTransform)list.parent);

            if (owned.Count == 0)
            {
                var row = UI.Row(list, 200, new Color(0, 0, 0, 0));
                var t = UI.Label(row, "No " + Modules.SlotNames[moduleSlot] + " modules yet.\nOpen Supply Crates in the STORE, or grab the Daily Deal.", 32, Pal.TextDim);
                UI.Stretch(t.rectTransform);
                return;
            }
            owned.Sort((a, b) => a.equipped != b.equipped ? (a.equipped ? -1 : 1) : b.rarity != a.rarity ? b.rarity.CompareTo(a.rarity) : b.level.CompareTo(a.level));
            foreach (var m in owned) ModuleRow(list, m);
        }

        void ModuleRow(RectTransform list, ModuleItem m)
        {
            var save = G.Save;
            var rc = RarityInfo.Col(m.rarity);
            var row = UI.Row(list, 132, m.equipped ? new Color(0.12f, 0.22f, 0.34f, 0.98f) : Pal.Panel);
            var stripe = UI.Img(row, "Rarity", rc);
            stripe.rectTransform.anchorMin = Vector2.zero; stripe.rectTransform.anchorMax = new Vector2(0, 1);
            stripe.rectTransform.sizeDelta = new Vector2(12, -16); stripe.rectTransform.anchoredPosition = new Vector2(14, 0);
            var icon = UI.Img(row, "Icon", rc, Art.SlotIcon(m.slot));
            UI.Place(icon.rectTransform, new Vector2(0, 0.5f), new Vector2(70, 70), new Vector2(40, 0), new Vector2(0, 0.5f));
            var name = UI.Label(row, Modules.Name(m) + (m.equipped ? "  <color=#7fd17f>EQUIPPED</color>" : ""), 32, Pal.Text, TextAnchor.UpperLeft, FontStyle.Bold);
            UI.Stretch(name.rectTransform, 128, 0, 560, 12);
            var tier = UI.Label(row, "<color=#" + ColorUtility.ToHtmlStringRGB(rc) + ">" + RarityInfo.Name(m.rarity).ToUpper() + "</color>  ·  Lv " + m.level + "/" + Modules.MaxLevel(m), 24, Pal.TextDim, TextAnchor.LowerLeft, FontStyle.Bold);
            UI.Stretch(tier.rectTransform, 128, 12, 560, 0);
            var stats = UI.Label(row, Modules.Describe(m), 24, Pal.Text, TextAnchor.MiddleLeft);
            stats.rectTransform.anchorMin = new Vector2(0.36f, 0); stats.rectTransform.anchorMax = new Vector2(0.62f, 1);
            stats.rectTransform.offsetMin = stats.rectTransform.offsetMax = Vector2.zero;

            // buttons: equip / upgrade / salvage
            var eq = UI.Button(row, m.equipped ? "UNEQUIP" : "EQUIP", 26, m.equipped ? Pal.PanelLight : Pal.Accent, () =>
            {
                if (m.equipped) m.equipped = false;
                else { var cur = save.Equipped(m.slot); if (cur != null) cur.equipped = false; m.equipped = true; }
                G.SaveGame(); Rebuild();
            });
            UI.Place((RectTransform)eq.transform, new Vector2(1, 0.5f), new Vector2(170, 80), new Vector2(-400, 0), new Vector2(1, 0.5f));

            bool max = m.level >= Modules.MaxLevel(m);
            int cost = Modules.LevelUpCost(m);
            var up = UI.Button(row, max ? "MAX" : "LV UP\n" + UI.Gold(cost), 22, max ? Pal.Disabled : save.gold >= cost ? new Color(0.75f, 0.52f, 0.12f) : Pal.Disabled, () =>
            {
                if (m.level >= Modules.MaxLevel(m)) return;
                if (!G.Spend(Modules.LevelUpCost(m))) return;
                m.level++; G.SaveGame(); Rebuild();
            });
            UI.Place((RectTransform)up.transform, new Vector2(1, 0.5f), new Vector2(190, 80), new Vector2(-200, 0), new Vector2(1, 0.5f));

            int value = Modules.SalvageValue(m);
            var sal = UI.Button(row, "SALVAGE\n+" + UI.Gold(value), 22, new Color(0.40f, 0.18f, 0.18f), () =>
            {
                if (m.equipped) { G.Toast("Unequip it first"); return; }
                Action doIt = () => { save.modules.Remove(m); save.AddGold(value); G.Audio.Play(Sfx.Coin); G.SaveGame(); Rebuild(); };
                if (m.rarity >= (int)Rarity.Epic) G.Dialog("Salvage " + RarityInfo.Name(m.rarity) + " module?", Modules.Name(m) + " will be scrapped for " + UI.Gold(value) + " gold.", "SALVAGE", doIt, "CANCEL");
                else doIt();
            });
            UI.Place((RectTransform)sal.transform, new Vector2(1, 0.5f), new Vector2(180, 80), new Vector2(-12, 0), new Vector2(1, 0.5f));
        }

        void Fuse(int rarity)
        {
            var save = G.Save;
            var c = Modules.FuseCandidates(save, moduleSlot, rarity);
            if (c.Count < 3) return;
            G.Dialog("Fuse modules?", "Three " + RarityInfo.Name(rarity) + " " + Modules.SlotNames[moduleSlot] + " modules (lowest level first) become one random " +
                     RarityInfo.Name(rarity + 1) + " " + Modules.SlotNames[moduleSlot] + " module.", "FUSE", () =>
            {
                for (int i = 0; i < 3; i++) save.modules.Remove(c[i]);
                var rng = new System.Random();
                var m = save.AddModule(moduleSlot, rng.Next(Modules.Variants[moduleSlot].Length), rarity + 1);
                G.SaveGame();
                G.Audio.Play(Sfx.Crate);
                Rebuild();
                StoreScreen.Reveal(new List<ModuleItem> { m }, "FUSION COMPLETE");
            }, "CANCEL");
        }

        void ShowBonuses()
        {
            var b = Loadout.Compute(G.Save);
            var lines = new List<string>();
            foreach (Stat s in Enum.GetValues(typeof(Stat))) if (b[s] > 0.001f) lines.Add(StatInfo.Format(s, b[s]));
            G.Dialog("Total bonuses", lines.Count == 0 ? "No bonuses yet. Buy upgrades and equip modules." : string.Join("    ", lines), "OK");
        }

        // ================================================================= ARMORY
        void BuildArmory()
        {
            var list = ScrollList(content);
            UI.Stretch((RectTransform)list.parent);
            var save = G.Save;
            var stats = Loadout.Compute(save);
            int cleared = 0;
            for (int i = 0; i < Missions.Count; i++) if (save.Cleared(i)) cleared = i + 1;

            for (int i = 0; i < Specials.All.Length; i++)
            {
                int idx = i;
                var d = Specials.All[i];
                bool unlocked = d.unlockMission <= cleared;
                int ammo = save.Ammo(i), max = Specials.MaxAmmo(i, stats);
                bool equipped = save.equippedSpecial == i;
                var row = UI.Row(list, 132, equipped ? new Color(0.12f, 0.22f, 0.34f, 0.98f) : Pal.Panel);
                var icon = UI.Img(row, "Icon", unlocked ? Color.white : new Color(1, 1, 1, 0.25f), Art.SpecialIcon(i));
                UI.Place(icon.rectTransform, new Vector2(0, 0.5f), new Vector2(84, 84), new Vector2(26, 0), new Vector2(0, 0.5f));
                var name = UI.Label(row, d.name + (equipped ? "  <color=#7fd17f>EQUIPPED</color>" : ""), 34, unlocked ? Pal.Text : Pal.TextDim, TextAnchor.UpperLeft, FontStyle.Bold);
                UI.Stretch(name.rectTransform, 130, 0, 700, 12);
                var desc = UI.Label(row, unlocked ? d.desc : "Clear " + Missions.All[Mathf.Max(0, d.unlockMission - 1)].code + " to unlock.", 24, Pal.TextDim, TextAnchor.LowerLeft);
                UI.Stretch(desc.rectTransform, 130, 12, 700, 0);
                if (!unlocked) continue;

                var count = UI.Label(row, ammo + " / " + max, 34, ammo > 0 ? Pal.Text : Pal.Red, TextAnchor.MiddleCenter, FontStyle.Bold);
                UI.Place(count.rectTransform, new Vector2(1, 0.5f), new Vector2(170, 80), new Vector2(-640, 0), new Vector2(1, 0.5f));

                var eq = UI.Button(row, equipped ? "EQUIPPED" : "EQUIP", 26, equipped ? Pal.Disabled : Pal.Accent, () => { save.equippedSpecial = idx; G.SaveGame(); Rebuild(); });
                UI.Place((RectTransform)eq.transform, new Vector2(1, 0.5f), new Vector2(170, 80), new Vector2(-450, 0), new Vector2(1, 0.5f));

                int buyOne = d.costPerAmmo;
                var b1 = UI.Button(row, "+1\n" + UI.Gold(buyOne), 22, ammo < max ? new Color(0.75f, 0.52f, 0.12f) : Pal.Disabled, () =>
                {
                    if (save.Ammo(idx) >= max) { G.Toast("Racks are full. Upgrade Payload Bay for more."); return; }
                    if (!G.Spend(buyOne)) return;
                    save.SetAmmo(idx, save.Ammo(idx) + 1); if (save.equippedSpecial < 0) save.equippedSpecial = idx;
                    G.SaveGame(); Rebuild();
                });
                UI.Place((RectTransform)b1.transform, new Vector2(1, 0.5f), new Vector2(170, 80), new Vector2(-260, 0), new Vector2(1, 0.5f));

                int need = Mathf.Max(0, max - ammo);
                int fillCost = need * d.costPerAmmo;
                var bf = UI.Button(row, need > 0 ? "FILL\n" + UI.Gold(fillCost) : "FULL", 22, need > 0 ? new Color(0.75f, 0.52f, 0.12f) : Pal.Disabled, () =>
                {
                    int n = Mathf.Max(0, max - save.Ammo(idx));
                    if (n == 0) return;
                    // fill as much as the player can afford
                    int afford = (int)Math.Min(n, save.gold / d.costPerAmmo);
                    if (afford <= 0) { G.Spend(d.costPerAmmo); return; }
                    if (!G.Spend(afford * d.costPerAmmo)) return;
                    save.SetAmmo(idx, save.Ammo(idx) + afford); if (save.equippedSpecial < 0) save.equippedSpecial = idx;
                    G.SaveGame(); Rebuild();
                });
                UI.Place((RectTransform)bf.transform, new Vector2(1, 0.5f), new Vector2(220, 80), new Vector2(-20, 0), new Vector2(1, 0.5f));
            }
        }
    }
}
