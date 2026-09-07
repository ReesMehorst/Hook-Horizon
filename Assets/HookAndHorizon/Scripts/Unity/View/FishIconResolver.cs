using System.Collections.Generic;
using UnityEngine;
using HookHorizon.Domain.Fishing;
using HookHorizon.Unity.Data;

namespace HookHorizon.Unity.View
{
    /// <summary>
    /// Resolves a fish's display icon for the immediate-mode views: the authored
    /// <see cref="FishData.Icon"/> when one is assigned, otherwise a deterministic generated
    /// placeholder so every species shows something until real art lands. A pure view helper —
    /// build one from the content on boot and inject it into the views (no gameplay coupling).
    /// Also the single fish-by-id lookup the HUD needs, since a <c>CaughtFish</c> only carries ids.
    /// </summary>
    public sealed class FishIconResolver
    {
        private const int Px = 16; // pixel-art canvas (matches the 16x16 sprite target)

        private readonly Dictionary<string, FishData> _byId = new Dictionary<string, FishData>();
        private readonly Dictionary<string, Sprite> _placeholders = new Dictionary<string, Sprite>();

        public FishIconResolver(IReadOnlyList<BiomeData> biomes)
        {
            if (biomes == null) return;
            foreach (BiomeData biome in biomes)
            {
                if (biome == null) continue;
                foreach (IFishSpecies species in biome.FishPool)
                    if (species is FishData fd && !string.IsNullOrEmpty(fd.Id) && !_byId.ContainsKey(fd.Id))
                        _byId[fd.Id] = fd;
            }
        }

        /// <summary>The species behind an id (for the HUD, which only has a <c>CaughtFish</c>).</summary>
        public IFishSpecies Species(string speciesId) =>
            speciesId != null && _byId.TryGetValue(speciesId, out FishData fd) ? fd : null;

        /// <summary>Authored sprite if present, else a generated placeholder. Never null.</summary>
        public Sprite Resolve(IFishSpecies species)
        {
            if (species is FishData fd && fd.Icon != null) return fd.Icon;
            return Placeholder(species?.Id, species?.Rarity ?? Rarity.Common);
        }

        /// <summary>Same, by id — resolves the species first so an authored sprite still wins.</summary>
        public Sprite Resolve(string speciesId)
        {
            IFishSpecies s = Species(speciesId);
            return s != null ? Resolve(s) : Placeholder(speciesId, Rarity.Common);
        }

        /// <summary>The mutation's tint for a caught fish, or white when it is not mutated.</summary>
        public Color MutationTint(string speciesId, string mutationId)
        {
            if (string.IsNullOrEmpty(mutationId)) return Color.white;
            IFishSpecies s = Species(speciesId);
            if (s?.PossibleMutations != null)
                foreach (IMutation m in s.PossibleMutations)
                    if (m.Id == mutationId && m is MutationData data) return data.Tint;
            return new Color(0.77f, 0.55f, 1f);
        }

        /// <summary>Draw a sprite (possibly a sub-rect of an atlas) into a rect with a tint.</summary>
        public static void Draw(Rect rect, Sprite sprite, Color tint)
        {
            if (sprite == null) return;
            Texture tex = sprite.texture;
            Rect tr = sprite.textureRect;
            var uv = new Rect(tr.x / tex.width, tr.y / tex.height, tr.width / tex.width, tr.height / tex.height);
            Color prev = GUI.color;
            GUI.color = tint;
            GUI.DrawTextureWithTexCoords(rect, tex, uv);
            GUI.color = prev;
        }

        // ---- Placeholder generation (deterministic per id) ------------------------

        private Sprite Placeholder(string id, Rarity rarity)
        {
            string key = id ?? "unknown";
            if (_placeholders.TryGetValue(key, out Sprite cached)) return cached;

            var tex = new Texture2D(Px, Px, TextureFormat.RGBA32, false)
            { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };

            Color body = HashColor(key, rarity);
            Color fin = body * 0.75f; fin.a = 1f;
            var eye = new Color(0.08f, 0.08f, 0.10f);
            var clear = new Color(0f, 0f, 0f, 0f);

            const float cx = 8.5f, cy = 8f, rx = 5f, ry = 3f;
            for (int y = 0; y < Px; y++)
            for (int x = 0; x < Px; x++)
            {
                Color c = clear;
                // Tail: a triangle to the left, widest at the outer edge.
                if (x >= 1 && x <= 4)
                {
                    int half = 4 - x + 1;
                    if (y >= cy - half && y <= cy + half) c = fin;
                }
                // Body: an ellipse.
                float dx = (x - cx) / rx, dy = (y - cy) / ry;
                if (dx * dx + dy * dy <= 1f) c = body;

                tex.SetPixel(x, y, c);
            }
            tex.SetPixel(11, 9, eye); // a facing-right eye

            tex.Apply();
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, Px, Px), new Vector2(0.5f, 0.5f), Px);
            _placeholders[key] = sprite;
            return sprite;
        }

        private static Color HashColor(string key, Rarity rarity)
        {
            uint h = 2166136261u;
            foreach (char ch in key) { h ^= ch; h *= 16777619u; }
            float hue = (h % 360u) / 360f;
            float sat = 0.45f + ((int)rarity % 4) * 0.06f; // rarer reads a touch more saturated
            return Color.HSVToRGB(hue, Mathf.Clamp01(sat), 0.9f);
        }
    }
}
