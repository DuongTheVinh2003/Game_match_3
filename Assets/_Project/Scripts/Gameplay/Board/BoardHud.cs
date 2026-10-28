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
        private Text playerHealthText;
        private Text botHealthText;
        private Text turnText;

        public void BuildClassic(int levelNumber, int targetScore, int remainingMoves, int score)
        {
            ConfigureCanvas();
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Color panelColor = PuzzleUiTheme.Cream;
            Color textColor = PuzzleUiTheme.Navy;

            levelText = CreatePanel("Level", new Vector2(24f, -24f), new Vector2(330f, 78f), font, panelColor, textColor);
            targetText = CreatePanel("Target", new Vector2(24f, -116f), new Vector2(330f, 104f), font, panelColor, textColor);
            movesText = CreatePanel("Moves", new Vector2(24f, -234f), new Vector2(150f, 150f), font, PuzzleUiTheme.Yellow, textColor);
            scoreText = CreatePanel("Score", new Vector2(188f, -234f), new Vector2(166f, 150f), font, panelColor, textColor);

            CreateBoosterReservedArea();
            SetLevel(levelNumber);
            SetTarget(targetScore);
            SetMoves(remainingMoves);
            SetScore(score);
        }

        public void BuildPve(int levelNumber, int playerHealth, int botHealth, PveTurn turn)
        {
            ConfigureCanvas();
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Color panelColor = PuzzleUiTheme.Cream;
            Color textColor = PuzzleUiTheme.Navy;

            levelText = CreatePanel("Level", new Vector2(24f, -24f), new Vector2(330f, 78f), font, panelColor, textColor);
            botHealthText = CreatePanel("BotHealth", new Vector2(24f, -116f), new Vector2(330f, 104f), font, new Color(1f, 0.79f, 0.74f, 0.98f), textColor);
            playerHealthText = CreatePanel("PlayerHealth", new Vector2(24f, -234f), new Vector2(330f, 104f), font, new Color(0.78f, 0.94f, 0.72f, 0.98f), textColor);
            turnText = CreatePanel("Turn", new Vector2(24f, -352f), new Vector2(330f, 104f), font, new Color(0.70f, 0.87f, 1f, 0.98f), textColor);

            CreateBoosterReservedArea();
            SetLevel(levelNumber);
            SetPveHealth(playerHealth, botHealth);
            SetPveTurn(turn, false);
        }

        private void ConfigureCanvas()
        {
            Canvas canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            // Giữ Canvas phủ đúng toàn bộ viewport. Các panel bên dưới sẽ luôn lấy
            // góc trên-trái thật của màn hình làm mốc thay vì một rect 100x100 mặc định.
            RectTransform canvasRect = (RectTransform)transform;
            canvasRect.anchorMin = Vector2.zero;
            canvasRect.anchorMax = Vector2.one;
            canvasRect.offsetMin = Vector2.zero;
            canvasRect.offsetMax = Vector2.zero;
            canvasRect.localScale = Vector3.one;

            CanvasScaler scaler = GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

        }

        private void CreateBoosterReservedArea()
        {
            GameObject boosterSpace = new GameObject("BoosterReservedArea", typeof(RectTransform));
            boosterSpace.transform.SetParent(transform, false);
            SetPreviewFlags(boosterSpace);
            RectTransform boosterRect = (RectTransform)boosterSpace.transform;
            boosterRect.anchorMin = Vector2.zero;
            boosterRect.anchorMax = Vector2.zero;
            boosterRect.pivot = Vector2.zero;
            boosterRect.anchoredPosition = new Vector2(24f, 24f);
            boosterRect.sizeDelta = new Vector2(330f, 110f);

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

        public void SetPveHealth(int playerHealth, int botHealth)
        {
            if (playerHealthText != null)
            {
                playerHealthText.text = $"Player HP\n{playerHealth}";
            }

            if (botHealthText != null)
            {
                botHealthText.text = $"Bot HP\n{botHealth}";
            }
        }

        public void SetPveTurn(PveTurn turn, bool isThinking)
        {
            if (turnText == null)
            {
                return;
            }

            turnText.text = turn == PveTurn.Player
                ? "PLAYER TURN"
                : isThinking ? "BOT THINKING..." : "BOT TURN";
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
            Image panelImage = panel.GetComponent<Image>();
            panelImage.color = panelColor;
            PuzzleUiTheme.ApplyRounded(panelImage);
            Shadow shadow = panel.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.03f, 0.11f, 0.20f, 0.25f);
            shadow.effectDistance = new Vector2(0f, -6f);

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
