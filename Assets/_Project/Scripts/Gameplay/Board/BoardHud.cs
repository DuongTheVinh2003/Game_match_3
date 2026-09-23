using UnityEngine;
using UnityEngine.UI;

namespace GameMatch3.Gameplay.Board
{
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(CanvasScaler))]
    public sealed class BoardHud : MonoBehaviour
    {
        private Text levelText;
        private Text targetText;
        private Text movesText;
        private Text scoreText;

        public void Build(int levelNumber, int targetScore, int remainingMoves, int score)
        {
            Canvas canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Color panelColor = new Color(0.96f, 0.68f, 0.76f, 0.94f);
            Color textColor = new Color(0.20f, 0.23f, 0.18f);

            levelText = CreatePanel("Level", new Vector2(24f, -24f), new Vector2(330f, 78f), font, panelColor, textColor);
            targetText = CreatePanel("Target", new Vector2(24f, -116f), new Vector2(330f, 96f), font, panelColor, textColor);
            movesText = CreatePanel("Moves", new Vector2(50f, -226f), new Vector2(280f, 108f), font, panelColor, textColor);
            scoreText = CreatePanel("Score", new Vector2(50f, -348f), new Vector2(220f, 174f), font, panelColor, textColor);

            // Khong hien thi gi; chi giu san mot vung layout cho booster o goc trai duoi.
            GameObject boosterSpace = new GameObject("BoosterReservedArea", typeof(RectTransform));
            boosterSpace.transform.SetParent(transform, false);
            SetPreviewFlags(boosterSpace);
            RectTransform boosterRect = (RectTransform)boosterSpace.transform;
            boosterRect.anchorMin = Vector2.zero;
            boosterRect.anchorMax = Vector2.zero;
            boosterRect.pivot = Vector2.zero;
            boosterRect.anchoredPosition = new Vector2(24f, 24f);
            boosterRect.sizeDelta = new Vector2(330f, 110f);

            SetLevel(levelNumber);
            SetTarget(targetScore);
            SetMoves(remainingMoves);
            SetScore(score);
        }

        public void SetLevel(int levelNumber)
        {
            if (levelText != null)
            {
                levelText.text = $"Level {levelNumber}";
            }
        }

        public void SetTarget(int targetScore)
        {
            if (targetText != null)
            {
                targetText.text = $"Target: {targetScore}";
            }
        }

        public void SetMoves(int remainingMoves)
        {
            if (movesText != null)
            {
                movesText.text = $"Moves\n{remainingMoves}";
            }
        }

        public void SetScore(int score)
        {
            if (scoreText != null)
            {
                scoreText.text = $"Score\n{score}";
            }
        }

        private Text CreatePanel(
            string panelName,
            Vector2 topLeftPosition,
            Vector2 size,
            Font font,
            Color panelColor,
            Color textColor)
        {
            GameObject panel = new GameObject(panelName, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(transform, false);
            SetPreviewFlags(panel);
            RectTransform panelRect = (RectTransform)panel.transform;
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.anchoredPosition = topLeftPosition;
            panelRect.sizeDelta = size;
            panel.GetComponent<Image>().color = panelColor;

            GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(panel.transform, false);
            SetPreviewFlags(textObject);
            RectTransform textRect = (RectTransform)textObject.transform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10f, 8f);
            textRect.offsetMax = new Vector2(-10f, -8f);

            Text text = textObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = 34;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = textColor;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 18;
            text.resizeTextMaxSize = 40;
            return text;
        }

        private static void SetPreviewFlags(GameObject gameObject)
        {
            if (!Application.isPlaying)
            {
                gameObject.hideFlags = HideFlags.DontSaveInEditor;
            }
        }
    }
}
