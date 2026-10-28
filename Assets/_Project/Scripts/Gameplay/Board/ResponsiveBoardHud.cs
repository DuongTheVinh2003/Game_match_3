using UnityEngine;
using UnityEngine.UI;

namespace GameMatch3.Gameplay.Board
{
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(CanvasScaler))]
    public sealed class ResponsiveBoardHud : MonoBehaviour
    {
        private Text levelText;
        private Text targetText;
        private Text movesText;
        private Text scoreText;
        private Text playerHealthText;
        private Text botHealthText;
        private Text turnText;
        private RectTransform scoreProgressFill;
        private RectTransform playerHealthFill;
        private RectTransform botHealthFill;
        private int targetScore = 1;
        private int playerMaxHealth = 1;
        private int botMaxHealth = 1;

        public void BuildClassic(int levelNumber, int newTargetScore, int remainingMoves, int score)
        {
            ConfigureCanvas();
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            RectTransform header = CreateHeader("ClassicHeader");

            RectTransform scoreCard = CreateAnchoredPanel(
                "ScoreCard", header, new Vector2(0f, 0f), new Vector2(0.46f, 1f),
                new Vector2(0f, 8f), new Vector2(-6f, -8f), PuzzleUiTheme.Cream);
            scoreText = CreateText(
                "ScoreText", scoreCard, font, 34, PuzzleUiTheme.Navy,
                new Vector2(0.05f, 0.45f), new Vector2(0.95f, 0.96f), TextAnchor.MiddleLeft);
            scoreProgressFill = CreateProgressBar(
                scoreCard, "ScoreProgress", new Vector2(0.06f, 0.14f),
                new Vector2(0.94f, 0.43f), PuzzleUiTheme.Blue, false);

            RectTransform targetCard = CreateAnchoredPanel(
                "TargetCard", header, new Vector2(0.54f, 0f), new Vector2(1f, 1f),
                new Vector2(6f, 8f), new Vector2(0f, -8f),
                new Color(0.74f, 0.91f, 1f, 0.98f));
            targetText = CreateText(
                "TargetText", targetCard, font, 34, PuzzleUiTheme.Navy,
                new Vector2(0.08f, 0.12f), new Vector2(0.92f, 0.88f), TextAnchor.MiddleRight);

            RectTransform movesCard = CreateCenteredPanel(
                "MovesCard", header, new Vector2(230f, 154f), PuzzleUiTheme.Yellow);
            levelText = CreateText(
                "LevelText", movesCard, font, 21, PuzzleUiTheme.Navy,
                new Vector2(0.08f, 0.67f), new Vector2(0.92f, 0.94f), TextAnchor.MiddleCenter);
            movesText = CreateText(
                "MovesText", movesCard, font, 39, PuzzleUiTheme.Navy,
                new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.70f), TextAnchor.MiddleCenter);

            targetScore = Mathf.Max(1, newTargetScore);
            SetLevel(levelNumber);
            SetTarget(newTargetScore);
            SetMoves(remainingMoves);
            SetScore(score);
        }

        public void BuildPve(
            int levelNumber,
            int playerHealth,
            int newPlayerMaxHealth,
            int botHealth,
            int newBotMaxHealth,
            PveTurn turn)
        {
            ConfigureCanvas();
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            RectTransform header = CreateHeader("PveHeader");

            RectTransform playerCard = CreateAnchoredPanel(
                "PlayerHealthCard", header, new Vector2(0f, 0f), new Vector2(0.46f, 1f),
                new Vector2(0f, 8f), new Vector2(-6f, -8f),
                new Color(0.78f, 0.94f, 0.72f, 0.98f));
            playerHealthText = CreateText(
                "PlayerHealthText", playerCard, font, 31, PuzzleUiTheme.Navy,
                new Vector2(0.05f, 0.44f), new Vector2(0.91f, 0.94f), TextAnchor.MiddleLeft);
            playerHealthFill = CreateProgressBar(
                playerCard, "PlayerHealth", new Vector2(0.06f, 0.14f),
                new Vector2(0.94f, 0.43f), PuzzleUiTheme.Green, false);

            RectTransform botCard = CreateAnchoredPanel(
                "BotHealthCard", header, new Vector2(0.54f, 0f), new Vector2(1f, 1f),
                new Vector2(6f, 8f), new Vector2(0f, -8f),
                new Color(1f, 0.79f, 0.74f, 0.98f));
            botHealthText = CreateText(
                "BotHealthText", botCard, font, 31, PuzzleUiTheme.Navy,
                new Vector2(0.09f, 0.44f), new Vector2(0.95f, 0.94f), TextAnchor.MiddleRight);
            botHealthFill = CreateProgressBar(
                botCard, "BotHealth", new Vector2(0.06f, 0.14f),
                new Vector2(0.94f, 0.43f), PuzzleUiTheme.Coral, true);

            RectTransform turnCard = CreateCenteredPanel(
                "TurnCard", header, new Vector2(246f, 154f),
                new Color(0.70f, 0.87f, 1f, 1f));
            levelText = CreateText(
                "LevelText", turnCard, font, 21, PuzzleUiTheme.Navy,
                new Vector2(0.06f, 0.67f), new Vector2(0.94f, 0.94f), TextAnchor.MiddleCenter);
            turnText = CreateText(
                "TurnText", turnCard, font, 34, PuzzleUiTheme.Navy,
                new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.70f), TextAnchor.MiddleCenter);

            playerMaxHealth = Mathf.Max(1, newPlayerMaxHealth);
            botMaxHealth = Mathf.Max(1, newBotMaxHealth);
            SetLevel(levelNumber);
            SetPveHealth(playerHealth, botHealth);
            SetPveTurn(turn, false);
        }

        private void ConfigureCanvas()
        {
            Canvas canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
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

        public void SetLevel(int levelNumber)
        {
            if (levelText != null)
            {
                levelText.text = $"LEVEL {levelNumber}";
            }
        }

        public void SetTarget(int newTargetScore)
        {
            targetScore = Mathf.Max(1, newTargetScore);
            if (targetText != null)
            {
                targetText.text = $"TARGET\n{newTargetScore:N0}";
            }
        }

        public void SetMoves(int remainingMoves)
        {
            if (movesText != null)
            {
                movesText.text = $"{remainingMoves}\nMOVES";
            }
        }

        public void SetScore(int score)
        {
            if (scoreText != null)
            {
                scoreText.text = $"SCORE  {score:N0}";
            }

            SetProgress(scoreProgressFill, score / (float)Mathf.Max(1, targetScore), false);
        }

        public void SetPveHealth(int playerHealth, int botHealth)
        {
            if (playerHealthText != null)
            {
                playerHealthText.text = $"PLAYER\n{playerHealth:N0} / {playerMaxHealth:N0}";
            }

            if (botHealthText != null)
            {
                botHealthText.text = $"BOT\n{botHealth:N0} / {botMaxHealth:N0}";
            }

            SetProgress(playerHealthFill, playerHealth / (float)playerMaxHealth, false);
            SetProgress(botHealthFill, botHealth / (float)botMaxHealth, true);
        }

        public void SetPveTurn(PveTurn turn, bool isThinking)
        {
            if (turnText != null)
            {
                turnText.text = turn == PveTurn.Player
                    ? "PLAYER TURN"
                    : isThinking ? "BOT THINKING..." : "BOT TURN";
            }
        }

        private RectTransform CreateHeader(string objectName)
        {
            GameObject headerObject = new GameObject(objectName, typeof(RectTransform));
            headerObject.transform.SetParent(transform, false);
            SetPreviewFlags(headerObject);
            RectTransform header = (RectTransform)headerObject.transform;
            header.anchorMin = new Vector2(0f, 1f);
            header.anchorMax = new Vector2(1f, 1f);
            header.pivot = new Vector2(0.5f, 1f);
            header.anchoredPosition = new Vector2(0f, -18f);
            header.sizeDelta = new Vector2(-36f, 164f);
            return header;
        }

        private RectTransform CreateAnchoredPanel(
            string panelName, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax, Color color)
        {
            GameObject panel = new GameObject(panelName, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            SetPreviewFlags(panel);
            RectTransform rect = (RectTransform)panel.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            ConfigurePanel(panel.GetComponent<Image>(), color, panel);
            return rect;
        }

        private RectTransform CreateCenteredPanel(
            string panelName, Transform parent, Vector2 size, Color color)
        {
            GameObject panel = new GameObject(panelName, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            SetPreviewFlags(panel);
            RectTransform rect = (RectTransform)panel.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
            ConfigurePanel(panel.GetComponent<Image>(), color, panel);
            return rect;
        }

        private static void ConfigurePanel(Image image, Color color, GameObject owner)
        {
            image.color = color;
            PuzzleUiTheme.ApplyRounded(image);
            Shadow shadow = owner.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.03f, 0.11f, 0.20f, 0.25f);
            shadow.effectDistance = new Vector2(0f, -6f);
        }

        private Text CreateText(
            string objectName, Transform parent, Font font, int fontSize, Color color,
            Vector2 anchorMin, Vector2 anchorMax, TextAnchor alignment)
        {
            GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            SetPreviewFlags(textObject);
            RectTransform rect = (RectTransform)textObject.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Text text = textObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = alignment;
            text.color = color;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 15;
            text.resizeTextMaxSize = fontSize;
            text.raycastTarget = false;
            return text;
        }

        private RectTransform CreateProgressBar(
            Transform parent, string objectName, Vector2 anchorMin, Vector2 anchorMax,
            Color fillColor, bool fillFromRight)
        {
            GameObject trackObject = new GameObject(objectName + "Track", typeof(RectTransform), typeof(Image));
            trackObject.transform.SetParent(parent, false);
            SetPreviewFlags(trackObject);
            RectTransform track = (RectTransform)trackObject.transform;
            track.anchorMin = anchorMin;
            track.anchorMax = anchorMax;
            track.offsetMin = Vector2.zero;
            track.offsetMax = Vector2.zero;
            Image trackImage = trackObject.GetComponent<Image>();
            trackImage.color = new Color(0.04f, 0.15f, 0.28f, 0.30f);
            trackImage.raycastTarget = false;
            PuzzleUiTheme.ApplyRounded(trackImage);

            GameObject fillObject = new GameObject(objectName + "Fill", typeof(RectTransform), typeof(Image));
            fillObject.transform.SetParent(track, false);
            SetPreviewFlags(fillObject);
            RectTransform fill = (RectTransform)fillObject.transform;
            Image fillImage = fillObject.GetComponent<Image>();
            fillImage.color = fillColor;
            fillImage.raycastTarget = false;
            PuzzleUiTheme.ApplyRounded(fillImage);
            SetProgress(fill, 1f, fillFromRight);
            return fill;
        }

        private static void SetProgress(RectTransform fill, float value, bool fromRight)
        {
            if (fill == null)
            {
                return;
            }

            float normalized = Mathf.Clamp01(value);
            fill.anchorMin = fromRight ? new Vector2(1f - normalized, 0f) : Vector2.zero;
            fill.anchorMax = fromRight ? Vector2.one : new Vector2(normalized, 1f);
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
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
