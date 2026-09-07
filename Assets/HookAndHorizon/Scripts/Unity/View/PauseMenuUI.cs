using System.Collections.Generic;
using UnityEngine;
using HookHorizon.Unity.Persistence;

namespace HookHorizon.Unity.View
{
    /// <summary>
    /// The in-game pause menu ([Esc]). Pauses the game (<c>Time.timeScale = 0</c>) and hosts
    /// three pages — Menu, Saves and Settings — so save/load, New Game and options are all
    /// reachable from one place. A pure view over <see cref="GameSaveController"/> (capture/
    /// apply/new-game) and <see cref="SaveService"/> (list/delete) plus a small
    /// <see cref="GameSettings"/>. Immediate-mode, matching PrototypeHud / FishLogUI. Quicksave
    /// (F5) / quickload (F9) still work without opening this.
    /// </summary>
    public sealed class PauseMenuUI : MonoBehaviour
    {
        private enum Page { Menu, Saves, Settings }

        private GameSaveController _controller;
        private SaveService _service;
        private GameSettings _settings;

        /// <summary>True while the pause menu overlay is open — other UIs ignore key toggles then.</summary>
        public static bool IsOpen { get; private set; }

        private bool _open;
        private Page _page = Page.Menu;
        private bool _confirmNewGame;
        private Vector2 _scroll;
        private string _saveName = "";
        private string _flash;
        private float _flashTimer;

        private GUIStyle _title, _row;
        private bool _stylesReady;

        public void Bind(GameSaveController controller, SaveService service)
        {
            if (_controller != null)
            {
                _controller.Saved -= OnSaved;
                _controller.Loaded -= OnLoaded;
            }
            _controller = controller;
            _service = service;

            _settings = GameSettings.Load();
            _settings.Apply();
            if (_controller != null)
            {
                _controller.AutosaveEnabled = _settings.AutosaveEnabled;
                _controller.Saved += OnSaved;
                _controller.Loaded += OnLoaded;
            }
        }

        private void OnDestroy()
        {
            if (_controller != null)
            {
                _controller.Saved -= OnSaved;
                _controller.Loaded -= OnLoaded;
            }
        }

        private void OnDisable() { IsOpen = false; Time.timeScale = 1f; } // never leave the game frozen

        private void OnSaved(string slot) => Flash($"Saved to '{slot}'");
        private void OnLoaded(string slot) => Flash($"Loaded '{slot}'");
        private void Flash(string text) { _flash = text; _flashTimer = 2.5f; }

        private void Update()
        {
            if (_flashTimer > 0f) _flashTimer -= Time.unscaledDeltaTime; // unscaled: time is frozen

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (!_open) Open();
                else if (_page != Page.Menu) { _page = Page.Menu; _confirmNewGame = false; }
                else Close();
            }
        }

        private void Open()
        {
            _open = true; IsOpen = true; _page = Page.Menu; _confirmNewGame = false;
            _saveName = _controller != null ? _controller.CurrentName : "";
            Time.timeScale = 0f;
        }

        private void Close() { _open = false; IsOpen = false; _confirmNewGame = false; Time.timeScale = 1f; }

        // ---- Rendering ------------------------------------------------------------

        private void OnGUI()
        {
            if (_controller == null || _service == null) return;
            EnsureStyles();

            if (_flashTimer > 0f)
            {
                GUI.color = new Color(0.7f, 1f, 0.8f, Mathf.Clamp01(_flashTimer / 0.8f));
                GUI.Label(new Rect(Screen.width * 0.5f - 160, 96, 320, 24), _flash,
                    new GUIStyle(_row) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold });
                GUI.color = Color.white;
            }

            if (!_open)
            {
                GUI.color = new Color(1f, 1f, 1f, 0.8f);
                GUI.Label(new Rect(16, Screen.height - 30, 460, 22),
                    "[Esc] Menu   ·   [F5] quicksave   ·   [F9] quickload", _row);
                GUI.color = Color.white;
                return;
            }

            DrawRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.6f));

            float w = 440f, h = _page == Page.Saves ? 480f : 360f;
            var panel = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
            DrawRect(panel, new Color(0.08f, 0.10f, 0.13f, 0.98f));
            DrawRect(new Rect(panel.x, panel.y, panel.width, 3f), new Color(0.4f, 0.7f, 1f, 0.9f));

            GUILayout.BeginArea(new Rect(panel.x + 20, panel.y + 16, panel.width - 40, panel.height - 32));
            switch (_page)
            {
                case Page.Menu: DrawMenu(); break;
                case Page.Saves: DrawSaves(); break;
                case Page.Settings: DrawSettings(); break;
            }
            GUILayout.EndArea();
        }

        private void DrawMenu()
        {
            GUILayout.Label("Hook & Horizon", _title);
            GUILayout.Label("<color=#8FA0B0>Paused</color>", new GUIStyle(_row) { richText = true });
            GUILayout.Space(14);

            if (GUILayout.Button("Resume", GUILayout.Height(36))) Close();
            GUILayout.Space(4);
            if (GUILayout.Button("Save / Load", GUILayout.Height(36))) _page = Page.Saves;
            GUILayout.Space(4);
            if (GUILayout.Button("Settings", GUILayout.Height(36))) _page = Page.Settings;
            GUILayout.Space(12);

            if (!_confirmNewGame)
            {
                if (GUILayout.Button("New Game", GUILayout.Height(32))) _confirmNewGame = true;
            }
            else
            {
                GUILayout.Label("<color=#F2C94C>Start a fresh world? The autosave is replaced " +
                    "(manual slots are kept).</color>", new GUIStyle(_row) { richText = true });
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Yes, new game", GUILayout.Height(30))) GameSession.StartNewGame();
                if (GUILayout.Button("Cancel", GUILayout.Height(30))) _confirmNewGame = false;
                GUILayout.EndHorizontal();
            }

            GUILayout.FlexibleSpace();
            GUILayout.Label("<color=#6C7A88>[Esc] resume   ·   [F5] quicksave   ·   [F9] quickload</color>",
                new GUIStyle(_row) { richText = true });
        }

        private void DrawSaves()
        {
            GUILayout.Label("Save / Load", _title);
            GUILayout.Space(6);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Name", _row, GUILayout.Width(44));
            _saveName = GUILayout.TextField(_saveName ?? "", 40, GUILayout.Height(24));
            GUILayout.EndHorizontal();
            GUILayout.Space(4);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Save", GUILayout.Height(30)))
            {
                _controller.RenameCurrent(_saveName);   // name the active playthrough, then save it
                _controller.SaveCurrent();
            }
            if (GUILayout.Button("Save as new", GUILayout.Height(30)))
                _controller.SaveAsNewPlaythrough(NameOrDefault());
            GUILayout.EndHorizontal();
            GUILayout.Space(8);

            IReadOnlyList<SaveSlotInfo> slots = _service.ListSlots();
            _scroll = GUILayout.BeginScrollView(_scroll);
            if (slots.Count == 0)
            {
                GUILayout.Label("<i>No saves yet — catch something, or save above.</i>",
                    new GUIStyle(_row) { richText = true });
            }
            else
            {
                foreach (SaveSlotInfo slot in slots)
                {
                    GUILayout.BeginHorizontal(GUI.skin.box);
                    string when = slot.SavedAtUtc == System.DateTime.MinValue
                        ? "—"
                        : slot.SavedAtUtc.ToLocalTime().ToString("MMM d, HH:mm");
                    bool current = slot.Name == _controller.CurrentSlot;
                    string tag = current ? "<color=#7FE0A0>\u25B6 </color>" : "";
                    GUILayout.Label($"{tag}{slot.Label}\n<color=#AEB6C2>{when} · {slot.FishDiscovered} fish · {slot.Gold}g</color>",
                        new GUIStyle(_row) { richText = true }, GUILayout.Width(250));
                    if (GUILayout.Button("Load", GUILayout.Width(62), GUILayout.Height(34)))
                    {
                        _controller.LoadFrom(slot.Name);
                        _saveName = _controller.CurrentName;
                    }
                    if (GUILayout.Button("X", GUILayout.Width(28), GUILayout.Height(34)))
                        _service.Delete(slot.Name);
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndScrollView();

            GUILayout.Space(6);
            if (GUILayout.Button("Back", GUILayout.Height(30))) _page = Page.Menu;
        }

        private void DrawSettings()
        {
            GUILayout.Label("Settings", _title);
            GUILayout.Space(10);

            GUILayout.Label($"Master volume   <color=#AEB6C2>{Mathf.RoundToInt(_settings.MasterVolume * 100)}%</color>",
                new GUIStyle(_row) { richText = true });
            float vol = GUILayout.HorizontalSlider(_settings.MasterVolume, 0f, 1f, GUILayout.Height(18));
            if (!Mathf.Approximately(vol, _settings.MasterVolume))
            {
                _settings.MasterVolume = vol;
                _settings.Apply();
                _settings.Save();
            }
            GUILayout.Space(12);

            bool fs = GUILayout.Toggle(_settings.Fullscreen, "  Fullscreen");
            if (fs != _settings.Fullscreen)
            {
                _settings.Fullscreen = fs;
                _settings.Apply();
                _settings.Save();
            }
            GUILayout.Space(8);

            bool autos = GUILayout.Toggle(_settings.AutosaveEnabled, "  Autosave (on catch / sell / quit)");
            if (autos != _settings.AutosaveEnabled)
            {
                _settings.AutosaveEnabled = autos;
                if (_controller != null) _controller.AutosaveEnabled = autos;
                _settings.Save();
            }

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Back", GUILayout.Height(30))) _page = Page.Menu;
        }

        private string NameOrDefault() =>
            string.IsNullOrWhiteSpace(_saveName)
                ? "Playthrough " + System.DateTime.Now.ToString("MMM d, HH:mm")
                : _saveName.Trim();

        private void EnsureStyles()
        {
            if (_stylesReady) return;
            _title = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
            _row = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true };
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
