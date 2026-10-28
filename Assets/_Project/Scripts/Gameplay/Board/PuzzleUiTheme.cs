using UnityEngine;
using UnityEngine.UI;

namespace GameMatch3.Gameplay.Board
{
    // Shared visual language for menu, level select, pause and gameplay HUD.
    public static class PuzzleUiTheme
    {
        public static readonly Color Cream = new Color(1f, 0.96f, 0.83f, 0.98f);
        public static readonly Color CreamSoft = new Color(1f, 0.98f, 0.91f, 0.94f);
        public static readonly Color Navy = new Color(0.05f, 0.18f, 0.36f, 1f);
        public static readonly Color Blue = new Color(0.08f, 0.56f, 0.94f, 1f);
        public static readonly Color Green = new Color(0.22f, 0.75f, 0.24f, 1f);
        public static readonly Color Yellow = new Color(1f, 0.72f, 0.08f, 1f);
        public static readonly Color Coral = new Color(0.96f, 0.30f, 0.24f, 1f);
        public static readonly Color Locked = new Color(0.49f, 0.60f, 0.69f, 0.92f);

        private static Sprite roundedSprite;

        public static Sprite RoundedSprite
        {
            get
            {
                if (roundedSprite == null)
                {
                    roundedSprite = CreateRoundedSprite();
                }

                return roundedSprite;
            }
        }

        public static void ApplyRounded(Image image)
        {
            if (image == null)
            {
                return;
            }

            image.sprite = RoundedSprite;
            image.type = Image.Type.Sliced;
        }

        private static Sprite CreateRoundedSprite()
        {
            const int size = 64;
            const float radius = 18f;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "RuntimeRoundedPanel",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(radius - x, x - (size - 1 - radius));
                    float dy = Mathf.Max(radius - y, y - (size - 1 - radius));
                    float outside = Mathf.Sqrt(Mathf.Max(0f, dx) * Mathf.Max(0f, dx)
                        + Mathf.Max(0f, dy) * Mathf.Max(0f, dy)) - radius;
                    byte alpha = (byte)Mathf.RoundToInt(255f * Mathf.Clamp01(1f - outside));
                    pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0u,
                SpriteMeshType.FullRect,
                new Vector4(20f, 20f, 20f, 20f));
        }
    }
}
