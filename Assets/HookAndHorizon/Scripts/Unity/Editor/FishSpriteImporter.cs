#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace HookHorizon.Unity.EditorTools
{
    /// <summary>
    /// Imports every texture under Content/Sprites/Fish/ as a crisp pixel-art sprite — no manual
    /// Inspector clicks. Point filter + no compression keep the 32x32 sprites sharp when the
    /// IMGUI views upscale them; alphaIsTransparency keeps the cut-out edges clean.
    /// </summary>
    public sealed class FishSpriteImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.Replace('\\', '/').Contains("/Content/Sprites/Fish/")) return;
            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.filterMode = FilterMode.Point;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.spritePixelsPerUnit = 48;
        }
    }
}
#endif
