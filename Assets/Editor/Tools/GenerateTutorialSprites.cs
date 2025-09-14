using System.IO;
using UnityEditor;
using UnityEngine;

namespace StoryToys.DragDrop.EditorTools
{
    public static class GenerateTutorialSprites
    {
        private const string OutputDir = "Assets/Art/UI/Tutorial";
        private const string MessagePng = OutputDir + "/message_bg_9slice.png";
        private const string SkipPng    = OutputDir + "/skip_bg_9slice.png";

        [MenuItem("Tools/StoryToys/Generate Tutorial Background Sprites")]
        public static void Generate()
        {
            EnsureDir(OutputDir);

            // Base canvas 64x64 with 9-slice borders at 16px
            const int size = 64;
            const int radius = 16;
            const int border = radius; // uniform 9-slice border

            // Light neutral colors (not bright white)
            Color msgColor  = new Color(0.93f, 0.93f, 0.93f, 1f); // message background
            Color skipColor = new Color(0.88f, 0.88f, 0.88f, 1f); // skip background

            WriteRoundedRectPng(MessagePng, size, size, radius, msgColor);
            WriteRoundedRectPng(SkipPng,    size, size, radius, skipColor);

            AssetDatabase.ImportAsset(MessagePng);
            AssetDatabase.ImportAsset(SkipPng);

            SetupSpriteImporter(MessagePng, border);
            SetupSpriteImporter(SkipPng,    border);

            // Load sprites
            var msgSprite  = AssetDatabase.LoadAssetAtPath<Sprite>(MessagePng);
            var skipSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SkipPng);

            // Ensure TutorialStyle asset exists and assign sprites
            const string stylePath = "Assets/Resources/Config/TutorialStyle.asset";
            var style = AssetDatabase.LoadAssetAtPath<TutorialStyle>(stylePath);
            if (style == null)
            {
                var dir = Path.GetDirectoryName(stylePath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                style = ScriptableObject.CreateInstance<TutorialStyle>();
                AssetDatabase.CreateAsset(style, stylePath);
            }
            style.messageBackground = msgSprite;
            style.skipBackground = skipSprite;
            // Use white tint so sprite colors are preserved
            style.messageColor = Color.white;
            style.skipColor = Color.white;
            style.messageTextColor = Color.white;
            style.skipTextColor = Color.white;
            // Keep existing sizes if already customized, else set sane defaults
            if (style.messageSize == Vector2.zero) style.messageSize = new Vector2(520, 110);
            if (style.skipSize == Vector2.zero)    style.skipSize    = new Vector2(100, 40);

            EditorUtility.SetDirty(style);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[GenerateTutorialSprites] Generated 9-slice sprites and assigned to TutorialStyle.\n" +
                      $"Message: {MessagePng}\nSkip: {SkipPng}");
        }

        private static void EnsureDir(string path)
        {
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        }

        private static void WriteRoundedRectPng(string path, int width, int height, int radius, Color fill)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false) { alphaIsTransparency = true };
            var pixels = new Color32[width * height];
            // Transparent base
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color(0, 0, 0, 0);

            // Draw AA rounded rect fill
            float r = Mathf.Max(0, radius);
            float2 min = new float2(r, r);
            float2 max = new float2(width - 1 - r, height - 1 - r);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float a = CoverageRoundedRect(x + 0.5f, y + 0.5f, min, max, r);
                    if (a <= 0f) continue;
                    var col = new Color(fill.r, fill.g, fill.b, Mathf.Clamp01(fill.a * a));
                    pixels[y * width + x] = col;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            var png = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);
            File.WriteAllBytes(path, png);
        }

        private static void SetupSpriteImporter(string assetPath, int border)
        {
            var ti = (TextureImporter)AssetImporter.GetAtPath(assetPath);
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = false;
            ti.filterMode = FilterMode.Bilinear;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.spritePixelsPerUnit = 100;
            ti.spriteBorder = new Vector4(border, border, border, border);
            ti.SaveAndReimport();
        }

        // Minimal float2 helper
        private struct float2 { public float x, y; public float2(float x, float y) { this.x = x; this.y = y; } }

        // Signed-distance rounded rectangle coverage for AA
        private static float CoverageRoundedRect(float px, float py, float2 min, float2 max, float radius)
        {
            // Distance to rectangle with rounded corners (Inigo Quilez style)
            float2 p = new float2(px, py);
            float2 q = new float2(Mathf.Max(min.x - p.x, 0) + Mathf.Max(p.x - max.x, 0),
                                  Mathf.Max(min.y - p.y, 0) + Mathf.Max(p.y - max.y, 0));
            float dist = Mathf.Sqrt(q.x * q.x + q.y * q.y) - radius;
            // Convert SDF to coverage with 1px width AA
            float aa = 1.0f; // 1 pixel
            return Mathf.Clamp01(0.5f - dist / aa);
        }
    }
}
