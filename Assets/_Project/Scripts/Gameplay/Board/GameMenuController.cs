using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameMatch3.Gameplay.Board
{
    // Prototype flow: Mode Select -> Classic/PVE -> Gameplay.
    public sealed class GameMenuController : MonoBehaviour
    {
        private const float ReferenceWidth = 886f;
        private const float ReferenceHeight = 1920f;
        private const string CatalogResourcePath = "LevelCatalog";

        private static readonly Color SideColor = new Color(0.055f, 0.075f, 0.12f);
        private static readonly Color MenuColor = new Color(0.18f, 0.56f, 0.78f);
        private static readonly Color ClassicColor = new Color(0.15f, 0.50f, 0.72f);
        private static readonly Color PveColor = new Color(0.36f, 0.68f, 0.28f);
        private static readonly Color CompletedColor = new Color(0.20f, 0.72f, 0.30f);
        private static readonly Color CurrentColor = new Color(0.10f, 0.58f, 0.92f);
        private static readonly Color LockedColor = new Color(0.42f, 0.45f, 0.50f);
        private static readonly Color AccentColor = new Color(1f, 0.76f, 0.16f);

        private BoardController board;
        private LevelCatalog catalog;
        private Canvas menuCanvas;
        private RectTransform portraitFrame;
        private RectTransform screenRoot;
        private LevelData selectedClassicLevel;
        private LevelData playingLevel;
        private LevelMode playingMode;
        private Coroutine returnRoutine;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateMenuFlow()
        {
            if (FindObjectOfType<GameMenuController>() != null)
            {
                return;
            }

            new GameObject("GameMenuFlow").AddComponent<GameMenuController>();
        }

        private void Awake()
        {
#if UNITY_ANDROID || UNITY_IOS
            Screen.orientation = ScreenOrientation.Portrait;
#endif
            catalog = Resources.Load<LevelCatalog>(CatalogResourcePath);
            board = FindObjectOfType<BoardController>();
            if (board == null)
            {
                Debug.LogError("Game menu requires a BoardController in the active scene.", this);
                enabled = false;
                return;
            }

            board.LevelFinished += OnLevelFinished;
            board.gameObject.SetActive(false);
            EnsureEventSystem();
            BuildCanvas();
            ShowModeSelection();
        }

        private void LateUpdate()
        {
            if (portraitFrame == null)
            {
                return;
            }

            float scale = Mathf.Min(Screen.width / ReferenceWidth, Screen.height / ReferenceHeight);
            portraitFrame.localScale = Vector3.one * Mathf.Max(0.01f, scale);
        }

        private void OnDestroy()
        {
            if (board != null)
            {
                board.LevelFinished -= OnLevelFinished;
            }
        }

        private void BuildCanvas()
        {
            GameObject canvasObject = new GameObject(
                "MenuCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            menuCanvas = canvasObject.GetComponent<Canvas>();
            menuCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            menuCanvas.sortingOrder = 500;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

            Image sideBackground = CreateImage("DesktopBackground", canvasObject.transform, SideColor);
            Stretch(sideBackground.rectTransform);
            sideBackground.raycastTarget = false;

            Image frame = CreateImage("PortraitFrame", canvasObject.transform, MenuColor);
            portraitFrame = frame.rectTransform;
            portraitFrame.anchorMin = portraitFrame.anchorMax = new Vector2(0.5f, 0.5f);
            portraitFrame.pivot = new Vector2(0.5f, 0.5f);
            portraitFrame.sizeDelta = new Vector2(ReferenceWidth, ReferenceHeight);
            portraitFrame.anchoredPosition = Vector2.zero;

            screenRoot = CreateRect("Screen", portraitFrame);
            Stretch(screenRoot);
            LateUpdate();
        }

        private void ShowModeSelection()
        {
            ClearScreen(MenuColor);
            CreateText(
                "Title",
                screenRoot,
                "Game match-3",
                86,
                Color.white,
                new Vector2(0f, 470f),
                new Vector2(760f, 180f));
            CreateText(
                "Subtitle",
                screenRoot,
                "CHOOSE GAME MODE",
                30,
                new Color(0.88f, 0.95f, 1f),
                new Vector2(0f, 320f),
                new Vector2(600f, 70f));

            CreateButton(
                "ClassicButton",
                screenRoot,
                "CLASSIC",
                new Vector2(0f, 80f),
                new Vector2(650f, 170f),
                new Color(0.30f, 0.78f, 0.28f),
                ShowClassic);
            CreateButton(
                "PveButton",
                screenRoot,
                "PVE",
                new Vector2(0f, -150f),
                new Vector2(650f, 170f),
                new Color(0.12f, 0.65f, 0.92f),
                ShowPve);

            CreateText(
                "PrototypeNote",
                screenRoot,
                "PROTOTYPE UI",
                24,
                new Color(1f, 1f, 1f, 0.55f),
                new Vector2(0f, -780f),
                new Vector2(400f, 60f));
        }

        private void ShowClassic()
        {
            ClearScreen(ClassicColor);
            IReadOnlyList<LevelData> levels = GetLevels(LevelMode.Classic);
            int completedCount = Progress.GetCompletedCount(LevelMode.Classic, levels.Count);
            int currentIndex = Mathf.Min(completedCount, Mathf.Max(0, levels.Count - 1));

            CreateHeader("CLASSIC", ShowSettings);
            CreateText(
                "Instruction",
                screenRoot,
                "SELECT A LEVEL, THEN PRESS PLAY",
                26,
                new Color(0.9f, 0.96f, 1f),
                new Vector2(0f, 690f),
                new Vector2(720f, 60f));

            if (levels.Count == 0)
            {
                CreateEmptyMessage("No Classic levels.\nCreate one in Level Editor.");
                CreateClassicNavigation();
                return;
            }

            int startIndex = completedCount == 0 ? 0 : Mathf.Max(0, currentIndex - 1);
            startIndex = Mathf.Min(startIndex, Mathf.Max(0, levels.Count - 3));
            int endIndex = Mathf.Min(levels.Count, startIndex + 3);

            if (selectedClassicLevel == null || !levels.Contains(selectedClassicLevel))
            {
                selectedClassicLevel = levels[currentIndex];
            }

            for (int index = startIndex; index < endIndex; index++)
            {
                LevelData level = levels[index];
                int slot = index - startIndex;
                float y = -300f + slot * 300f;
                LevelState state = GetState(index, completedCount);
                bool selected = level == selectedClassicLevel;
                Color color = GetStateColor(state);
                if (selected)
                {
                    CreateImageAt(
                        $"Selected_{level.LevelNumber}",
                        screenRoot,
                        AccentColor,
                        new Vector2(0f, y),
                        new Vector2(310f, 230f));
                }

                Button button = CreateButton(
                    $"Level_{level.LevelNumber}",
                    screenRoot,
                    BuildLevelLabel(level, state),
                    new Vector2(0f, y),
                    new Vector2(280f, 200f),
                    color,
                    () =>
                    {
                        selectedClassicLevel = level;
                        ShowClassic();
                    },
                    46);
                button.interactable = state != LevelState.Locked;
            }

            int selectedIndex = IndexOf(levels, selectedClassicLevel);
            bool canPlay = selectedIndex >= 0 && GetState(selectedIndex, completedCount) != LevelState.Locked;
            Button play = CreateButton(
                "PlayButton",
                screenRoot,
                "PLAY",
                new Vector2(0f, -650f),
                new Vector2(560f, 170f),
                canPlay ? new Color(0.38f, 0.85f, 0.16f) : LockedColor,
                () => StartLevel(LevelMode.Classic, selectedClassicLevel),
                66);
            play.interactable = canPlay;
            CreateClassicNavigation();
        }

        private void ShowPve()
        {
            ClearScreen(PveColor);
            IReadOnlyList<LevelData> levels = GetLevels(LevelMode.Pve);
            int completedCount = Progress.GetCompletedCount(LevelMode.Pve, levels.Count);

            CreateHeader("PVE", ShowSettings);
            if (levels.Count == 0)
            {
                CreateEmptyMessage("No PVE levels.\nCreate one in Level Editor.");
                return;
            }

            RectTransform viewport = CreateRect("MapViewport", screenRoot);
            viewport.anchorMin = new Vector2(0f, 0f);
            viewport.anchorMax = new Vector2(1f, 1f);
            viewport.offsetMin = new Vector2(40f, 50f);
            viewport.offsetMax = new Vector2(-40f, -230f);
            Image viewportImage = viewport.gameObject.AddComponent<Image>();
            viewportImage.color = new Color(0.20f, 0.52f, 0.23f, 0.35f);
            viewport.gameObject.AddComponent<RectMask2D>();
            ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 55f;
            scroll.viewport = viewport;

            float contentHeight = Mathf.Max(1650f, 300f + levels.Count * 260f);
            RectTransform content = CreateRect("MapContent", viewport);
            content.anchorMin = new Vector2(0f, 0f);
            content.anchorMax = new Vector2(1f, 0f);
            content.pivot = new Vector2(0.5f, 0f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, contentHeight);
            scroll.content = content;

            Vector2 previous = Vector2.zero;
            for (int index = 0; index < levels.Count; index++)
            {
                LevelData level = levels[index];
                Vector2 position = GetMapPosition(index);
                if (index > 0)
                {
                    CreatePathSegment(content, previous, position);
                }

                LevelState state = GetState(index, completedCount);
                Button node = CreateButton(
                    $"PveLevel_{level.LevelNumber}",
                    content,
                    BuildLevelLabel(level, state),
                    position,
                    new Vector2(190f, 150f),
                    GetStateColor(state),
                    () => StartLevel(LevelMode.Pve, level),
                    42);
                node.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0f);
                node.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0f);
                node.interactable = state != LevelState.Locked;
                previous = position;
            }

            Canvas.ForceUpdateCanvases();
            if (contentHeight > viewport.rect.height)
            {
                float currentY = GetMapPosition(Mathf.Min(completedCount, levels.Count - 1)).y;
                scroll.verticalNormalizedPosition = Mathf.Clamp01(
                    (currentY - viewport.rect.height * 0.45f) /
                    Mathf.Max(1f, contentHeight - viewport.rect.height));
            }
        }

        private void ShowSettings()
        {
            Image shade = CreateImage("SettingsShade", screenRoot, new Color(0f, 0f, 0f, 0.65f));
            Stretch(shade.rectTransform);

            Image panel = CreateImageAt(
                "SettingsPanel",
                shade.transform,
                new Color(0.86f, 0.91f, 0.83f),
                Vector2.zero,
                new Vector2(740f, 1120f));
            CreateText("SettingsTitle", panel.transform, "SETTINGS", 68, new Color(0.12f, 0.35f, 0.25f),
                new Vector2(0f, 430f), new Vector2(600f, 100f));
            CreateButton(
                "CloseSettingsButton",
                panel.transform,
                "X",
                new Vector2(285f, 475f),
                new Vector2(90f, 90f),
                new Color(0.95f, 0.95f, 0.90f),
                () => Destroy(shade.gameObject),
                42,
                new Color(0.12f, 0.35f, 0.25f));

            CreateSettingRow(panel.transform, "NOTICE", 230f, 0.65f);
            CreateSettingRow(panel.transform, "MUSIC", 20f, 0.75f);
            CreateSettingRow(panel.transform, "SOUND", -190f, 0.48f);
            CreateText(
                "SettingsHint",
                panel.transform,
                "Settings controls are prototype only",
                22,
                new Color(0.2f, 0.35f, 0.28f, 0.75f),
                new Vector2(0f, -340f),
                new Vector2(600f, 60f));
            CreateButton(
                "HomeButton",
                panel.transform,
                "HOME",
                new Vector2(0f, -445f),
                new Vector2(480f, 130f),
                new Color(0.95f, 0.38f, 0.16f),
                ShowModeSelection,
                50);
        }

        private void CreateSettingRow(Transform parent, string label, float y, float fill)
        {
            CreateText(label + "Label", parent, label, 34, new Color(0.32f, 0.30f, 0.22f),
                new Vector2(-220f, y), new Vector2(220f, 70f), TextAnchor.MiddleLeft);
            CreateImageAt(label + "Track", parent, new Color(0.66f, 0.62f, 0.53f),
                new Vector2(125f, y), new Vector2(360f, 42f));
            Image fillImage = CreateImageAt(label + "Fill", parent, AccentColor,
                new Vector2(125f - 180f * (1f - fill), y), new Vector2(360f * fill, 42f));
            fillImage.raycastTarget = false;
            CreateImageAt(label + "Knob", parent, CurrentColor,
                new Vector2(-55f + 360f * fill, y), new Vector2(76f, 76f));
        }

        private void CreateHeader(string title, UnityEngine.Events.UnityAction settingsAction)
        {
            CreateText("ModeTitle", screenRoot, title, 64, Color.white,
                new Vector2(0f, 820f), new Vector2(520f, 100f));
            CreateButton("SettingsButton", screenRoot, "SETTINGS", new Vector2(-315f, 820f),
                new Vector2(180f, 110f), new Color(0.95f, 0.95f, 0.95f), settingsAction, 25, new Color(0.18f, 0.25f, 0.32f));
        }

        private void CreateClassicNavigation()
        {
            Image bar = CreateImageAt("BottomNavigation", screenRoot, new Color(0.08f, 0.34f, 0.58f),
                new Vector2(0f, -850f), new Vector2(820f, 170f));
            CreateButton("LeftPlaceholder", bar.transform, "COMING\nSOON", new Vector2(-270f, 0f),
                new Vector2(250f, 140f), new Color(0.14f, 0.43f, 0.68f), null, 27).interactable = false;
            CreateButton("ClassicTab", bar.transform, "CLASSIC", Vector2.zero,
                new Vector2(250f, 140f), new Color(0.24f, 0.62f, 0.90f), ShowClassic, 34);
            CreateButton("RightPlaceholder", bar.transform, "COMING\nSOON", new Vector2(270f, 0f),
                new Vector2(250f, 140f), new Color(0.14f, 0.43f, 0.68f), null, 27).interactable = false;
        }

        private void StartLevel(LevelMode mode, LevelData level)
        {
            if (level == null || board == null)
            {
                return;
            }

            playingMode = mode;
            playingLevel = level;
            menuCanvas.gameObject.SetActive(false);
            board.SetLevelData(level);
            board.gameObject.SetActive(true);
        }

        private void OnLevelFinished(LevelResult result)
        {
            if (result == LevelResult.Won)
            {
                IReadOnlyList<LevelData> levels = GetLevels(playingMode);
                int index = IndexOf(levels, playingLevel);
                Progress.Complete(playingMode, index, levels.Count);
                if (playingMode == LevelMode.Classic && levels.Count > 0)
                {
                    int completedCount = Progress.GetCompletedCount(LevelMode.Classic, levels.Count);
                    selectedClassicLevel = levels[Mathf.Min(completedCount, levels.Count - 1)];
                }
            }

            if (returnRoutine != null)
            {
                StopCoroutine(returnRoutine);
            }

            returnRoutine = StartCoroutine(ReturnToModeAfterDelay());
        }

        private IEnumerator ReturnToModeAfterDelay()
        {
            yield return new WaitForSecondsRealtime(0.8f);
            board.gameObject.SetActive(false);
            menuCanvas.gameObject.SetActive(true);
            if (playingMode == LevelMode.Classic)
            {
                ShowClassic();
            }
            else
            {
                ShowPve();
            }

            returnRoutine = null;
        }

        private IReadOnlyList<LevelData> GetLevels(LevelMode mode)
        {
            if (catalog != null)
            {
                return catalog.GetLevels(mode);
            }

            if (board != null && board.AssignedLevelData != null && board.AssignedLevelData.Mode == mode)
            {
                return new[] { board.AssignedLevelData };
            }

            return new LevelData[0];
        }

        private void ClearScreen(Color color)
        {
            for (int i = screenRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(screenRoot.GetChild(i).gameObject);
            }

            portraitFrame.GetComponent<Image>().color = color;
        }

        private void CreateEmptyMessage(string message)
        {
            CreateText("EmptyMessage", screenRoot, message, 38, Color.white,
                Vector2.zero, new Vector2(720f, 220f));
        }

        private static LevelState GetState(int index, int completedCount)
        {
            if (index < completedCount)
            {
                return LevelState.Completed;
            }

            return index == completedCount ? LevelState.Current : LevelState.Locked;
        }

        private static Color GetStateColor(LevelState state)
        {
            switch (state)
            {
                case LevelState.Completed:
                    return CompletedColor;
                case LevelState.Current:
                    return CurrentColor;
                default:
                    return LockedColor;
            }
        }

        private static string BuildLevelLabel(LevelData level, LevelState state)
        {
            string stateText = state == LevelState.Completed
                ? "DONE"
                : state == LevelState.Current ? "CURRENT" : "LOCKED";
            return $"LEVEL {level.LevelNumber}\n{stateText}";
        }

        private static int IndexOf(IReadOnlyList<LevelData> levels, LevelData level)
        {
            for (int i = 0; i < levels.Count; i++)
            {
                if (levels[i] == level)
                {
                    return i;
                }
            }

            return -1;
        }

        private static Vector2 GetMapPosition(int index)
        {
            float[] xPattern = { -220f, -60f, 180f, 235f, 20f, -205f };
            return new Vector2(xPattern[index % xPattern.Length], 150f + index * 260f);
        }

        private void CreatePathSegment(Transform parent, Vector2 from, Vector2 to)
        {
            Vector2 delta = to - from;
            Image line = CreateImageAt("Path", parent, new Color(0.92f, 0.78f, 0.42f),
                (from + to) * 0.5f, new Vector2(delta.magnitude, 24f));
            line.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            line.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            line.rectTransform.localRotation = Quaternion.Euler(0f, 0f,
                Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            line.transform.SetAsFirstSibling();
        }

        private Button CreateButton(
            string name,
            Transform parent,
            string label,
            Vector2 position,
            Vector2 size,
            Color color,
            UnityEngine.Events.UnityAction action,
            int fontSize = 38,
            Color? textColor = null)
        {
            Image image = CreateImageAt(name, parent, color, position, size);
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.16f);
            colors.pressedColor = Color.Lerp(color, Color.black, 0.18f);
            colors.disabledColor = new Color(color.r, color.g, color.b, 0.55f);
            button.colors = colors;
            if (action != null)
            {
                button.onClick.AddListener(action);
            }

            PrototypeTextGraphic text = CreateText("Label", image.transform, label, fontSize,
                textColor ?? Color.white, Vector2.zero, size);
            Stretch(text.rectTransform);
            text.raycastTarget = false;
            return button;
        }

        private PrototypeTextGraphic CreateText(
            string name,
            Transform parent,
            string value,
            int fontSize,
            Color color,
            Vector2 position,
            Vector2 size,
            TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            GameObject gameObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(PrototypeTextGraphic));
            gameObject.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)gameObject.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            PrototypeTextGraphic text = gameObject.GetComponent<PrototypeTextGraphic>();
            text.Value = value;
            text.CharacterHeight = fontSize;
            text.Alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        private static Image CreateImage(string name, Transform parent, Color color)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            Image image = gameObject.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static Image CreateImageAt(
            string name,
            Transform parent,
            Color color,
            Vector2 position,
            Vector2 size)
        {
            Image image = CreateImage(name, parent, color);
            RectTransform rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return image;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return (RectTransform)gameObject.transform;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        private enum LevelState
        {
            Completed,
            Current,
            Locked
        }

        private static class Progress
        {
            private const string ClassicKey = "GameMatch3.Progress.Classic.Completed";
            private const string PveKey = "GameMatch3.Progress.PVE.Completed";

            public static int GetCompletedCount(LevelMode mode, int levelCount)
            {
                return Mathf.Clamp(PlayerPrefs.GetInt(GetKey(mode), 0), 0, levelCount);
            }

            public static void Complete(LevelMode mode, int levelIndex, int levelCount)
            {
                if (levelIndex < 0)
                {
                    return;
                }

                int completed = GetCompletedCount(mode, levelCount);
                if (levelIndex == completed)
                {
                    PlayerPrefs.SetInt(GetKey(mode), Mathf.Min(levelCount, completed + 1));
                    PlayerPrefs.Save();
                }
            }

            private static string GetKey(LevelMode mode)
            {
                return mode == LevelMode.Classic ? ClassicKey : PveKey;
            }
        }
    }
}
