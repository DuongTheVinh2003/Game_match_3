using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GameMatch3.Gameplay.Board
{
    // Font bitmap 5x7 ve truc tiep bang UI mesh, khong phu thuoc font he thong/WebGL.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class PrototypeTextGraphic : MaskableGraphic
    {
        private static readonly Dictionary<char, string> Glyphs = new Dictionary<char, string>
        {
            ['A'] = "01110100011000111111100011000110001",
            ['B'] = "11110100011000111110100011000111110",
            ['C'] = "01111100001000010000100001000001111",
            ['D'] = "11110100011000110001100011000111110",
            ['E'] = "11111100001000011110100001000011111",
            ['F'] = "11111100001000011110100001000010000",
            ['G'] = "01111100001000010111100011000101111",
            ['H'] = "10001100011000111111100011000110001",
            ['I'] = "11111001000010000100001000010011111",
            ['J'] = "00111000100001000010100101001001100",
            ['K'] = "10001100101010011000101001001010001",
            ['L'] = "10000100001000010000100001000011111",
            ['M'] = "10001110111010110101100011000110001",
            ['N'] = "10001110011010110011100011000110001",
            ['O'] = "01110100011000110001100011000101110",
            ['P'] = "11110100011000111110100001000010000",
            ['Q'] = "01110100011000110001101011001001101",
            ['R'] = "11110100011000111110101001001010001",
            ['S'] = "01111100001000001110000010000111110",
            ['T'] = "11111001000010000100001000010000100",
            ['U'] = "10001100011000110001100011000101110",
            ['V'] = "10001100011000110001100010101000100",
            ['W'] = "10001100011000110101101011101110001",
            ['X'] = "10001100010101000100010101000110001",
            ['Y'] = "10001100010101000100001000010000100",
            ['Z'] = "11111000010001000100010001000011111",
            ['0'] = "01110100011001110101110011000101110",
            ['1'] = "00100011000010000100001000010001110",
            ['2'] = "01110100010000100010001000100011111",
            ['3'] = "11110000010000101110000010000111110",
            ['4'] = "00010001100101010010111110001000010",
            ['5'] = "11111100001000011110000010000111110",
            ['6'] = "01110100001000011110100011000101110",
            ['7'] = "11111000010001000100010000100001000",
            ['8'] = "01110100011000101110100011000101110",
            ['9'] = "01110100011000101111000010000101110",
            ['-'] = "00000000000000011111000000000000000",
            ['.'] = "00000000000000000000000000010000100",
            [','] = "00000000000000000000000000100001000",
            [':'] = "00000001000010000000001000010000000",
            ['?'] = "01110100010000100010001000000000100"
        };

        [SerializeField] private string value = string.Empty;
        [SerializeField, Min(7f)] private float characterHeight = 32f;
        [SerializeField] private TextAnchor alignment = TextAnchor.MiddleCenter;

        public string Value
        {
            get => value;
            set
            {
                this.value = value ?? string.Empty;
                SetVerticesDirty();
            }
        }

        public float CharacterHeight
        {
            get => characterHeight;
            set
            {
                characterHeight = Mathf.Max(7f, value);
                SetVerticesDirty();
            }
        }

        public TextAnchor Alignment
        {
            get => alignment;
            set
            {
                alignment = value;
                SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            string[] lines = value.ToUpperInvariant().Split('\n');
            int longestLine = 1;
            foreach (string line in lines)
            {
                longestLine = Mathf.Max(longestLine, line.Length);
            }

            Rect rect = rectTransform.rect;
            float requestedPixel = characterHeight / 7f;
            float widthPixel = rect.width / Mathf.Max(1f, longestLine * 6f - 1f);
            float heightPixel = rect.height / Mathf.Max(1f, lines.Length * 8f - 1f);
            float pixel = Mathf.Max(0.25f, Mathf.Min(requestedPixel, widthPixel, heightPixel));
            float totalHeight = (lines.Length * 8f - 1f) * pixel;
            float top = rect.center.y + totalHeight * 0.5f;
            Color32 vertexColor = color;

            for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                string line = lines[lineIndex];
                float lineWidth = Mathf.Max(0f, line.Length * 6f - 1f) * pixel;
                float startX = GetStartX(rect, lineWidth);
                for (int characterIndex = 0; characterIndex < line.Length; characterIndex++)
                {
                    char character = line[characterIndex];
                    if (character == ' ')
                    {
                        continue;
                    }

                    if (!Glyphs.TryGetValue(character, out string glyph))
                    {
                        glyph = Glyphs['?'];
                    }

                    for (int row = 0; row < 7; row++)
                    {
                        for (int column = 0; column < 5; column++)
                        {
                            if (glyph[row * 5 + column] != '1')
                            {
                                continue;
                            }

                            float x = startX + (characterIndex * 6f + column) * pixel;
                            float y = top - (lineIndex * 8f + row + 1f) * pixel;
                            AddQuad(vertexHelper, x, y, pixel, vertexColor);
                        }
                    }
                }
            }
        }

        private float GetStartX(Rect rect, float lineWidth)
        {
            switch (alignment)
            {
                case TextAnchor.UpperLeft:
                case TextAnchor.MiddleLeft:
                case TextAnchor.LowerLeft:
                    return rect.xMin;
                case TextAnchor.UpperRight:
                case TextAnchor.MiddleRight:
                case TextAnchor.LowerRight:
                    return rect.xMax - lineWidth;
                default:
                    return rect.center.x - lineWidth * 0.5f;
            }
        }

        private static void AddQuad(VertexHelper vertexHelper, float x, float y, float size, Color32 color)
        {
            int start = vertexHelper.currentVertCount;
            vertexHelper.AddVert(new Vector3(x, y), color, Vector2.zero);
            vertexHelper.AddVert(new Vector3(x, y + size), color, Vector2.up);
            vertexHelper.AddVert(new Vector3(x + size, y + size), color, Vector2.one);
            vertexHelper.AddVert(new Vector3(x + size, y), color, Vector2.right);
            vertexHelper.AddTriangle(start, start + 1, start + 2);
            vertexHelper.AddTriangle(start, start + 2, start + 3);
        }
    }
}
