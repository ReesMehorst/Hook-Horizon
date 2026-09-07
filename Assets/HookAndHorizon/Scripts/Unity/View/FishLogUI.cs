using System.Collections.Generic;
using UnityEngine;
using HookHorizon.Domain.Fishing;
using HookHorizon.Domain.Progression;
using HookHorizon.Unity.Data;

namespace HookHorizon.Unity.View
{
    /// <summary>
    /// The Fish Log / Compendium view (Epic 12.2). No longer self-toggling: it is hosted as a
    /// tab inside <see cref="JournalUI"/>, which owns the [L] key and the outer panel and calls
    /// <see cref="DrawBody"/> / <see cref="DrawOverlay"/>. It still owns its own state and the
    /// "New species!" toast (drawn independently so it shows even when the Journal is closed).
    /// A pure view — reads a <see cref="FishLog"/> + static biome content, never touches gameplay.
    /// </summary>
    public sealed class FishLogUI : MonoBehaviour
    {
        private FishLog _log;
        private FishIconResolver _icons;

        // Precomputed from the (static) content on Bind.
        private readonly List<BiomeData> _biomes = new List<BiomeData>();
        private readonly Dictionary<string, IFishSpecies> _speciesById = new Dictionary<string, IFishSpecies>();
        private readonly Dictionary<string, List<IFishSpecies>> _biomeSpecies = new Dictionary<string, List<IFishSpecies>>();
        private readonly Dictionary<string, List<string>> _biomesBySpecies = new Dictionary<string, List<string>>();
        private readonly List<string> _allSpeciesIds = new List<string>();

        // View state.
        private Vector2 _scroll;
        private string _selectedSpeciesId;
        private readonly HashSet<string> _newlyDiscovered = new HashSet<string>();
        private string _toast;
        private float _toastTimer;
        private const float ToastDuration = 3.5f;

        private GUIStyle _sub, _biomeHead, _cell, _cellDim, _detailBody, _title;
        private bool _stylesReady;

        public void Bind(FishLog log, IReadOnlyList<BiomeData> biomes, FishIconResolver icons)
        {
            Unsubscribe();
            _log = log;
            _icons = icons;
            BuildContentIndex(biomes);
            if (_log != null) _log.NewSpeciesDiscovered += OnNewSpecies;
        }

        private void BuildContentIndex(IReadOnlyList<BiomeData> biomes)
        {
            _biomes.Clear(); _speciesById.Clear(); _biomeSpecies.Clear();
            _biomesBySpecies.Clear(); _allSpeciesIds.Clear();
            if (biomes == null) return;

            var seenGlobal = new HashSet<string>();
            foreach (BiomeData biome in biomes)
            {
                if (biome == null) continue;
                _biomes.Add(biome);

                var listForBiome = new List<IFishSpecies>();
                var seenInBiome = new HashSet<string>();
                foreach (IFishSpecies species in biome.FishPool)
                {
                    if (species == null || string.IsNullOrEmpty(species.Id)) continue;
                    if (!seenInBiome.Add(species.Id)) continue;

                    listForBiome.Add(species);
                    if (!_speciesById.ContainsKey(species.Id)) _speciesById[species.Id] = species;
                    if (seenGlobal.Add(species.Id)) _allSpeciesIds.Add(species.Id);

                    if (!_biomesBySpecies.TryGetValue(species.Id, out List<string> homes))
                    {
                        homes = new List<string>();
                        _biomesBySpecies[species.Id] = homes;
                    }
                    homes.Add(biome.DisplayName);
                }
                _biomeSpecies[biome.Id] = listForBiome;
            }
        }

        private void OnNewSpecies(string speciesId)
        {
            _newlyDiscovered.Add(speciesId);
            string name = _speciesById.TryGetValue(speciesId, out IFishSpecies s) ? s.DisplayName : speciesId;
            _toast = $"New species discovered!  {name}";
            _toastTimer = ToastDuration;
        }

        private void OnDestroy() => Unsubscribe();
        private void Unsubscribe() { if (_log != null) _log.NewSpeciesDiscovered -= OnNewSpecies; }

        private void Update() { if (_toastTimer > 0f) _toastTimer -= Time.deltaTime; }

        // The New-species toast is independent of the Journal so it flashes during play.
        private void OnGUI()
        {
            if (_log == null || _toastTimer <= 0f) return;
            EnsureStyles();
            float a = Mathf.Clamp01(_toastTimer / 0.9f);
            var rect = new Rect(Screen.width * 0.5f - 230, 84, 460, 34);
            DrawRect(rect, new Color(0.12f, 0.32f, 0.16f, 0.85f * a));
            GUI.color = new Color(0.75f, 1f, 0.8f, a);
            GUI.Label(rect, _toast, new GUIStyle(_sub)
            { alignment = TextAnchor.MiddleCenter, fontSize = 16, fontStyle = FontStyle.Bold });
            GUI.color = Color.white;
        }

        // ---- Hosted API (called by JournalUI) -------------------------------------

        public string GetHeader()
        {
            int total = _allSpeciesIds.Count, found = DiscoveredGlobal();
            return $"{found} / {total} species discovered  ·  {Percent(found, total)}% complete";
        }

        public bool DetailOpen => _selectedSpeciesId != null;

        /// <summary>Called when the Journal closes: drop the detail card + clear NEW flags.</summary>
        public void OnClosed() { _selectedSpeciesId = null; _newlyDiscovered.Clear(); }

        /// <summary>Draw the biome-grouped grid inside the given content rect.</summary>
        public void DrawBody(Rect content)
        {
            EnsureStyles();
            GUILayout.BeginArea(content);

            const float cellW = 168f, cellH = 62f, gap = 6f;
            int cols = Mathf.Max(1, (int)((content.width + gap) / (cellW + gap)));

            bool prev = GUI.enabled;
            GUI.enabled = _selectedSpeciesId == null; // grid inert while a detail card is open
            _scroll = GUILayout.BeginScrollView(_scroll);
            foreach (BiomeData biome in _biomes)
            {
                List<IFishSpecies> species = _biomeSpecies[biome.Id];
                if (species.Count == 0) continue;

                int biomeFound = DiscoveredIn(species);
                GUILayout.Space(6);
                GUILayout.Label(
                    $"{biome.DisplayName}   <color=#B9C4D0>{biomeFound}/{species.Count} " +
                    $"({Percent(biomeFound, species.Count)}%)</color>", _biomeHead);

                for (int i = 0; i < species.Count; i += cols)
                {
                    GUILayout.BeginHorizontal();
                    for (int j = i; j < i + cols && j < species.Count; j++)
                        DrawCell(species[j], cellW, cellH);
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndScrollView();
            GUI.enabled = prev;
            GUILayout.EndArea();
        }

        /// <summary>Draw the detail card (full-screen overlay) if a species is selected.</summary>
        public void DrawOverlay() { if (_selectedSpeciesId != null) DrawDetail(); }

        // ---- Grid cells -----------------------------------------------------------

        private void DrawCell(IFishSpecies species, float cellW, float cellH)
        {
            bool discovered = _log.Contains(species.Id);
            var opts = new[] { GUILayout.Width(cellW), GUILayout.Height(cellH) };

            if (!discovered)
            {
                bool prev = GUI.enabled;
                GUI.enabled = false;
                GUILayout.Button("<size=20><b>?</b></size>\n<color=#888888>undiscovered</color>", _cellDim, opts);
                GUI.enabled = prev;
                DrawCellIcon(species, new Color(0f, 0f, 0f, 0.55f));
                return;
            }

            FishLogEntry e = _log.Get(species.Id);
            bool isNew = _newlyDiscovered.Contains(species.Id);

            string name = Truncate(species.DisplayName, 20);
            string header = isNew ? $"<b>{name}</b> <color=#FFE08A>NEW</color>" : $"<b>{name}</b>";
            string line2 = $"<color=#CFCFCF>{e.BestSize} · {e.BestWeightKg:0.0}kg · x{e.TimesCaught}</color>";
            string label = $"{header}\n{line2}\n{BadgeLine(species, e)}";

            Color prevBg = GUI.backgroundColor;
            if (isNew) GUI.backgroundColor = new Color(1f, 0.85f, 0.35f);
            if (GUILayout.Button(label, _cell, opts)) _selectedSpeciesId = species.Id;
            GUI.backgroundColor = prevBg;
            DrawCellIcon(species, Color.white);
        }

        // Draw the species icon over the reserved left strip of the cell just drawn.
        private void DrawCellIcon(IFishSpecies species, Color tint)
        {
            if (_icons == null) return;
            Rect cell = GUILayoutUtility.GetLastRect();
            const float sz = 44f;
            var box = new Rect(cell.x + 8f, cell.y + (cell.height - sz) * 0.5f, sz, sz);
            FishIconResolver.Draw(box, _icons.Resolve(species), tint);
        }

        private string BadgeLine(IFishSpecies species, FishLogEntry e)
        {
            var sb = new System.Text.StringBuilder();
            foreach (SizeClass size in AllSizes)
                if (e.HasSize(size)) sb.Append(Chip(size.ToString().Substring(0, 1), SizeColor(size)));
            foreach (string mutId in e.MutationsSeen)
                sb.Append(Chip("✦", MutationColor(species, mutId)));
            return sb.Length == 0 ? "<color=#666666>—</color>" : sb.ToString();
        }

        // ---- Detail card ----------------------------------------------------------

        private void DrawDetail()
        {
            IFishSpecies species = _speciesById.TryGetValue(_selectedSpeciesId, out IFishSpecies s) ? s : null;
            FishLogEntry e = _log.Get(_selectedSpeciesId);
            if (species == null || e == null) { _selectedSpeciesId = null; return; }

            DrawRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.55f));

            float w = 460f, h = 380f;
            var card = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
            DrawRect(card, new Color(0.08f, 0.09f, 0.11f, 0.98f));
            DrawRect(new Rect(card.x, card.y, card.width, 3f), new Color(0.4f, 0.7f, 1f, 0.9f));
            if (_icons != null)
            {
                const float isz = 96f;
                FishIconResolver.Draw(new Rect(card.xMax - isz - 20f, card.y + 18f, isz, isz),
                    _icons.Resolve(species), Color.white);
            }

            GUILayout.BeginArea(new Rect(card.x + 18, card.y + 14, card.width - 36, card.height - 28));
            GUILayout.Label($"{species.DisplayName}", _title);
            GUILayout.Label($"<color=#AEB6C2>{species.Rarity} · {WeightRange(species)}</color>", _detailBody);
            GUILayout.Space(6);
            GUILayout.Label($"Caught <b>{e.TimesCaught}</b>×   ·   Best <b>{e.BestWeightKg:0.0}kg</b> ({e.BestSize})", _detailBody);
            GUILayout.Space(8);
            GUILayout.Label("<b>Sizes</b>", _detailBody);
            GUILayout.Label(SizeCollectionRow(e), _detailBody);
            GUILayout.Space(6);
            GUILayout.Label("<b>Mutations</b>", _detailBody);
            GUILayout.Label(MutationCollectionRow(species, e), _detailBody);
            GUILayout.Space(6);
            GUILayout.Label($"<b>Found in</b>  <color=#AEB6C2>{FoundIn(species.Id)}</color>", _detailBody);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Close", GUILayout.Height(28))) _selectedSpeciesId = null;
            GUILayout.EndArea();
        }

        private string SizeCollectionRow(FishLogEntry e)
        {
            var sb = new System.Text.StringBuilder();
            foreach (SizeClass size in AllSizes)
            {
                string hex = e.HasSize(size) ? ColorHex(SizeColor(size)) : "555555";
                sb.Append($"<color=#{hex}>{size}</color>   ");
            }
            return sb.ToString();
        }

        private string MutationCollectionRow(IFishSpecies species, FishLogEntry e)
        {
            if (species.PossibleMutations == null || species.PossibleMutations.Count == 0)
                return "<color=#666666>this species has no mutations</color>";
            var sb = new System.Text.StringBuilder();
            foreach (IMutation m in species.PossibleMutations)
            {
                string hex = e.HasMutation(m.Id) ? ColorHex(MutationColor(species, m.Id)) : "555555";
                sb.Append($"<color=#{hex}>✦ {m.DisplayName}</color>   ");
            }
            return sb.ToString();
        }

        // ---- Helpers --------------------------------------------------------------

        private static readonly SizeClass[] AllSizes =
        {
            SizeClass.Tiny, SizeClass.Small, SizeClass.Normal,
            SizeClass.Large, SizeClass.Huge, SizeClass.Monster
        };

        private int DiscoveredGlobal()
        {
            int n = 0;
            foreach (string id in _allSpeciesIds) if (_log.Contains(id)) n++;
            return n;
        }

        private int DiscoveredIn(List<IFishSpecies> species)
        {
            int n = 0;
            foreach (IFishSpecies s in species) if (_log.Contains(s.Id)) n++;
            return n;
        }

        private string FoundIn(string speciesId) =>
            _biomesBySpecies.TryGetValue(speciesId, out List<string> homes) && homes.Count > 0
                ? string.Join(", ", homes) : "—";

        private static string WeightRange(IFishSpecies s) => $"{s.MinWeightKg:0.0}–{s.MaxWeightKg:0.0}kg";
        private static int Percent(int part, int whole) => whole <= 0 ? 0 : Mathf.RoundToInt(100f * part / whole);
        private static string Truncate(string s, int max) =>
            string.IsNullOrEmpty(s) || s.Length <= max ? s : s.Substring(0, max - 1) + "…";
        private static string Chip(string glyph, Color c) => $"<color=#{ColorHex(c)}>{glyph}</color> ";

        private static Color SizeColor(SizeClass size) => size switch
        {
            SizeClass.Tiny => new Color(0.50f, 0.69f, 1f),
            SizeClass.Small => new Color(0.55f, 0.84f, 0.63f),
            SizeClass.Normal => new Color(0.85f, 0.85f, 0.85f),
            SizeClass.Large => new Color(0.95f, 0.81f, 0.30f),
            SizeClass.Huge => new Color(0.95f, 0.60f, 0.29f),
            SizeClass.Monster => new Color(0.92f, 0.34f, 0.34f),
            _ => Color.white
        };

        private static Color MutationColor(IFishSpecies species, string mutationId)
        {
            if (species.PossibleMutations != null)
                foreach (IMutation m in species.PossibleMutations)
                    if (m.Id == mutationId && m is MutationData data) return data.Tint;
            return new Color(0.77f, 0.55f, 1f);
        }

        private static string ColorHex(Color c) => ColorUtility.ToHtmlStringRGB(c);

        private void EnsureStyles()
        {
            if (_stylesReady) return;
            _title = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold };
            _sub = new GUIStyle(GUI.skin.label) { fontSize = 14 };
            _biomeHead = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold, richText = true };
            _cell = new GUIStyle(GUI.skin.button)
            {
                richText = true, wordWrap = true, alignment = TextAnchor.UpperLeft, fontSize = 11,
                padding = new RectOffset(58, 6, 5, 5)   // left padding reserves the icon strip
            };
            _cellDim = new GUIStyle(_cell) { alignment = TextAnchor.MiddleCenter, padding = new RectOffset(6, 6, 5, 5) };
            _detailBody = new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true, fontSize = 14 };
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
