using System.Collections.Generic;
using UnityEngine;
using HookHorizon.Domain.Progression;
using HookHorizon.Unity.Player;

namespace HookHorizon.Unity.View
{
    /// <summary>
    /// The player's Journal ([L]): one panel with tabs. Hosts the Quests tab and the Fish Log
    /// tab (delegated to <see cref="FishLogUI"/>), with room for a Map tab later. Owns the [L]
    /// toggle and the outer panel; a pure view over <see cref="QuestSystem"/> + the hosted
    /// Fish Log. Immediate-mode, matching PrototypeHud. Blocked while the pause menu is open.
    /// </summary>
    public sealed class JournalUI : MonoBehaviour
    {
        private enum Tab { Quests, FishLog }

        private FishLogUI _fishLog;
        private QuestSystem _quests;
        private IInputSource _input;

        private bool _open;
        private Tab _tab = Tab.Quests;
        private Vector2 _questScroll;
        private string _toast;
        private float _toastTimer;

        private GUIStyle _title, _sub, _row, _tabStyle;
        private bool _stylesReady;

        public void Bind(FishLogUI fishLog, QuestSystem quests, IInputSource input)
        {
            if (_quests != null) _quests.QuestCompleted -= OnQuestCompleted;
            _fishLog = fishLog;
            _quests = quests;
            _input = input;
            if (_quests != null) _quests.QuestCompleted += OnQuestCompleted;
        }

        private void OnDestroy() { if (_quests != null) _quests.QuestCompleted -= OnQuestCompleted; }

        private void OnQuestCompleted(Quest q) { _toast = $"Quest complete: {q.Title}"; _toastTimer = 3.5f; }

        private void Update()
        {
            if (_toastTimer > 0f) _toastTimer -= Time.deltaTime;
            if (_input != null && _input.ToggleFishLogDown && !PauseMenuUI.IsOpen)
            {
                _open = !_open;
                if (!_open && _fishLog != null) _fishLog.OnClosed();
            }
        }

        private void OnGUI()
        {
            EnsureStyles();

            if (_toastTimer > 0f)
            {
                GUI.color = new Color(1f, 0.9f, 0.6f, Mathf.Clamp01(_toastTimer / 0.9f));
                GUI.Label(new Rect(Screen.width * 0.5f - 220, 120, 440, 26), _toast,
                    new GUIStyle(_row) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold });
                GUI.color = Color.white;
            }

            if (!_open)
            {
                GUI.color = new Color(1f, 1f, 1f, 0.85f);
                GUI.Label(new Rect(16, Screen.height - 30, 460, 22), "[L] Journal", _sub);
                GUI.color = Color.white;
                return;
            }

            float w = Mathf.Min(Screen.width - 60, 980);
            float h = Screen.height - 100;
            var panel = new Rect((Screen.width - w) * 0.5f, 50, w, h);
            DrawRect(panel, new Color(0f, 0f, 0f, 0.9f));

            GUI.Label(new Rect(panel.x + 18, panel.y + 12, panel.width - 36, 28), "Journal", _title);

            const float tabW = 130f, tabH = 28f;
            float tabY = panel.y + 46;
            if (TabButton(new Rect(panel.x + 18, tabY, tabW, tabH), "Quests", _tab == Tab.Quests)) _tab = Tab.Quests;
            if (TabButton(new Rect(panel.x + 18 + tabW + 6, tabY, tabW, tabH), "Fish Log", _tab == Tab.FishLog)) _tab = Tab.FishLog;

            string sub = _tab == Tab.FishLog && _fishLog != null ? _fishLog.GetHeader() : QuestHeader();
            GUI.Label(new Rect(panel.x + 18, tabY + tabH + 4, panel.width - 36, 20), sub + "        (press [L] to close)", _sub);

            float top = tabY + tabH + 28;
            var content = new Rect(panel.x + 18, top, panel.width - 36, panel.yMax - top - 16);
            if (_tab == Tab.Quests) DrawQuests(content);
            else if (_fishLog != null) { _fishLog.DrawBody(content); _fishLog.DrawOverlay(); }
        }

        // ---- Quests tab -----------------------------------------------------------

        private string QuestHeader()
        {
            if (_quests == null) return "";
            int total = _quests.All.Count, done = 0;
            foreach (Quest q in _quests.All) if (q.Completed) done++;
            return $"{done} / {total} quests complete";
        }

        private void DrawQuests(Rect content)
        {
            GUILayout.BeginArea(content);
            _questScroll = GUILayout.BeginScrollView(_questScroll);

            if (_quests == null)
            {
                GUILayout.Label("No quests.", _row);
            }
            else
            {
                bool anyActive = false;
                foreach (Quest q in _quests.Active) { anyActive = true; DrawQuest(q, false); }
                if (!anyActive) GUILayout.Label("<i>No active quests right now.</i>", new GUIStyle(_row) { richText = true });

                GUILayout.Space(10);
                bool anyDone = false;
                foreach (Quest q in _quests.All) if (q.Completed) { anyDone = true; break; }
                if (anyDone)
                {
                    GUILayout.Label("<color=#7FB0A0><b>Completed</b></color>", new GUIStyle(_row) { richText = true });
                    foreach (Quest q in _quests.All) if (q.Completed) DrawQuest(q, true);
                }
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawQuest(Quest q, bool done)
        {
            GUILayout.BeginVertical(GUI.skin.box);
            string title = done ? $"<color=#8FB89F>✓ {q.Title}</color>" : $"<b>{q.Title}</b>";
            GUILayout.Label(title, new GUIStyle(_row) { richText = true, fontSize = 15 });
            GUILayout.Label($"<color=#AEB6C2>{q.Description}</color>", new GUIStyle(_row) { richText = true });

            QuestObjective o = q.Objective;
            GUILayout.Label($"{ObjectiveText(o)}  <color=#CFCFCF>{o.Progress}/{o.Target}</color>",
                new GUIStyle(_row) { richText = true });

            // progress bar
            Rect bar = GUILayoutUtility.GetRect(1, 8, GUILayout.ExpandWidth(true));
            DrawRect(bar, new Color(1f, 1f, 1f, 0.12f));
            float frac = o.Target <= 0 ? 0f : Mathf.Clamp01((float)o.Progress / o.Target);
            DrawRect(new Rect(bar.x, bar.y, bar.width * frac, bar.height),
                done ? new Color(0.5f, 0.8f, 0.6f) : new Color(0.4f, 0.7f, 1f));

            string reward = RewardText(q.Reward);
            if (!string.IsNullOrEmpty(reward))
                GUILayout.Label($"<color=#F2C94C>Reward: {reward}</color>", new GUIStyle(_row) { richText = true });
            GUILayout.EndVertical();
            GUILayout.Space(6);
        }

        private static string ObjectiveText(QuestObjective o) => o.Kind switch
        {
            ObjectiveKind.CatchAny => $"Catch {o.Target} fish",
            ObjectiveKind.CatchDistinctSpecies => $"Catch {o.Target} different species",
            ObjectiveKind.VisitDistinctBiomes => $"Fish in {o.Target} biomes",
            ObjectiveKind.EarnGoldTotal => $"Earn {o.Target}g total",
            ObjectiveKind.SellFishCount => $"Sell {o.Target} fish",
            _ => "Objective"
        };

        private static string RewardText(QuestReward r)
        {
            if (r == null) return "";
            var parts = new List<string>();
            if (r.Coins > 0) parts.Add($"{r.Coins}g");
            if (r.BackpackSlots > 0) parts.Add($"+{r.BackpackSlots} bag slots");
            if (!string.IsNullOrEmpty(r.UnlockBiomeId)) parts.Add($"unlocks a biome");
            return string.Join(", ", parts);
        }

        // ---- Chrome ---------------------------------------------------------------

        private bool TabButton(Rect rect, string label, bool active)
        {
            Color prev = GUI.backgroundColor;
            GUI.backgroundColor = active ? new Color(0.35f, 0.6f, 0.95f) : new Color(0.25f, 0.28f, 0.32f);
            bool clicked = GUI.Button(rect, label, _tabStyle);
            GUI.backgroundColor = prev;
            return clicked && !active;
        }

        private void EnsureStyles()
        {
            if (_stylesReady) return;
            _title = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
            _sub = new GUIStyle(GUI.skin.label) { fontSize = 14 };
            _row = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true };
            _tabStyle = new GUIStyle(GUI.skin.button) { fontSize = 14, fontStyle = FontStyle.Bold };
            _stylesReady = true;
        }

        private static void DrawRect(Rect rect, Color color)
        {
            Color prev = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = prev;
        }
    }
}
