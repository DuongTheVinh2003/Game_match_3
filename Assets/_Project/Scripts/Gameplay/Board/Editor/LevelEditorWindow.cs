using System;
using System.Collections.Generic;
using System.Linq;
using GameMatch3.Gameplay.Board.Bot;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameMatch3.Gameplay.Board.Editor
{
    public sealed class LevelEditorWindow : EditorWindow
    {
        private const string LevelFolder = "Assets/_Project/Levels";
        private const string ResourcesFolder = "Assets/_Project/Resources";
        private const string RuntimeCatalogPath = ResourcesFolder + "/LevelCatalog.asset";

        private readonly List<LevelData> allLevels = new List<LevelData>();
        private readonly List<LevelData> levels = new List<LevelData>();
        private readonly List<TilePoolEntry> catalog = new List<TilePoolEntry>();

        private ListView levelList;
        private Label levelCountLabel;
        private Label listTitle;
        private Button classicModeButton;
        private Button pveModeButton;
        private VisualElement detailsRoot;
        private VisualElement tabContent;
        private Button deleteButton;
        private Button resetButton;
        private Button applyButton;
        private Button playButton;
        private LevelData currentAsset;
        private LevelDraft draft;
        private bool isDirty;
        private bool isBuildingUi;
        private bool suppressSelection;
        private int activeTab;
        private LevelMode activeMode = LevelMode.Classic;

        [MenuItem("Window/Game Match 3/Level Editor")]
        public static void ShowWindow()
        {
            LevelEditorWindow window = GetWindow<LevelEditorWindow>();
            window.titleContent = new GUIContent("Level Editor");
            window.minSize = new Vector2(820f, 520f);
            window.Show();
        }

        [InitializeOnLoadMethod]
        private static void ScheduleCatalogSync()
        {
            EditorApplication.delayCall += SyncRuntimeCatalogFromAssets;
        }

        public void CreateGUI()
        {
            LoadCatalog();
            BoardController board = FindBoardController();
            if (board != null && board.AssignedLevelData != null)
            {
                activeMode = board.AssignedLevelData.Mode;
            }

            BuildWindow();
            RefreshLevels();
            RestoreAssignedLevel();
        }

        private void BuildWindow()
        {
            VisualElement root = rootVisualElement;
            root.Clear();
            root.style.backgroundColor = new Color(0.12f, 0.13f, 0.15f);

            Toolbar toolbar = new Toolbar();
            toolbar.style.height = 36f;
            toolbar.Add(CreateToolbarButton("+ Add Level", AddLevel));
            deleteButton = CreateToolbarButton("Delete Level", DeleteLevel);
            resetButton = CreateToolbarButton("Reset Level", ResetLevel);
            applyButton = CreateToolbarButton("Apply", ApplyDraftFromButton);
            playButton = CreateToolbarButton("▶ Play Level", PlayLevel);
            toolbar.Add(deleteButton);
            toolbar.Add(resetButton);
            VisualElement toolbarSpacer = new VisualElement();
            toolbarSpacer.style.flexGrow = 1f;
            toolbar.Add(toolbarSpacer);
            toolbar.Add(applyButton);
            playButton.style.backgroundColor = new Color(0.20f, 0.52f, 0.28f);
            playButton.style.unityFontStyleAndWeight = FontStyle.Bold;
            toolbar.Add(playButton);
            root.Add(toolbar);

            TwoPaneSplitView split = new TwoPaneSplitView(0, 250f, TwoPaneSplitViewOrientation.Horizontal);
            split.style.flexGrow = 1f;
            root.Add(split);

            VisualElement listPane = new VisualElement();
            listPane.style.paddingLeft = 8f;
            listPane.style.paddingRight = 8f;
            listPane.style.paddingTop = 8f;
            listPane.style.paddingBottom = 8f;
            split.Add(listPane);

            VisualElement modeSelector = new VisualElement();
            modeSelector.style.flexDirection = FlexDirection.Row;
            modeSelector.style.marginBottom = 10f;
            classicModeButton = new Button(() => SwitchMode(LevelMode.Classic));
            pveModeButton = new Button(() => SwitchMode(LevelMode.Pve));
            classicModeButton.style.flexGrow = 1f;
            pveModeButton.style.flexGrow = 1f;
            classicModeButton.style.height = 30f;
            pveModeButton.style.height = 30f;
            modeSelector.Add(classicModeButton);
            modeSelector.Add(pveModeButton);
            listPane.Add(modeSelector);

            listTitle = new Label();
            listTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            listTitle.style.fontSize = 14f;
            listPane.Add(listTitle);
            levelCountLabel = new Label("0 levels");
            levelCountLabel.style.color = new Color(0.65f, 0.68f, 0.72f);
            levelCountLabel.style.marginBottom = 6f;
            listPane.Add(levelCountLabel);

            levelList = new ListView
            {
                selectionType = SelectionType.Single,
                fixedItemHeight = 34f,
                virtualizationMethod = CollectionVirtualizationMethod.FixedHeight,
                makeItem = CreateLevelListItem,
                bindItem = BindLevelListItem
            };
            levelList.style.flexGrow = 1f;
            levelList.selectionChanged += OnLevelSelectionChanged;
            listPane.Add(levelList);

            Button addFromListButton = new Button(AddLevel) { text = "+ Add Level" };
            addFromListButton.style.height = 32f;
            addFromListButton.style.marginTop = 8f;
            listPane.Add(addFromListButton);

            detailsRoot = new VisualElement();
            detailsRoot.style.flexGrow = 1f;
            detailsRoot.style.paddingLeft = 14f;
            detailsRoot.style.paddingRight = 14f;
            detailsRoot.style.paddingTop = 10f;
            detailsRoot.style.paddingBottom = 10f;
            split.Add(detailsRoot);

            UpdateActionButtons();
        }

        private static Button CreateToolbarButton(string text, Action action)
        {
            Button button = new Button(action) { text = text };
            button.style.minWidth = 96f;
            return button;
        }

        private void LoadCatalog()
        {
            catalog.Clear();
            BoardController board = FindBoardController();
            if (board != null)
            {
                catalog.AddRange(board.TileCatalog);
                return;
            }

            TilePool fallbackPool = new TilePool();
            fallbackPool.EnsureCompleteCatalog();
            catalog.AddRange(fallbackPool.Entries);
        }

        private void RefreshLevels()
        {
            allLevels.Clear();
            levels.Clear();
            string[] guids = AssetDatabase.FindAssets("t:LevelData", new[] { LevelFolder });
            foreach (string guid in guids)
            {
                LevelData level = AssetDatabase.LoadAssetAtPath<LevelData>(AssetDatabase.GUIDToAssetPath(guid));
                if (level != null)
                {
                    allLevels.Add(level);
                    if (level.Mode == activeMode)
                    {
                        levels.Add(level);
                    }
                }
            }

            levels.Sort((first, second) => first.LevelNumber.CompareTo(second.LevelNumber));
            SyncRuntimeCatalog(allLevels);
            levelList.itemsSource = levels;
            levelList.Rebuild();
            levelCountLabel.text = levels.Count == 1 ? "1 level" : $"{levels.Count} levels";
            UpdateModeButtons();
        }

        private void SwitchMode(LevelMode mode)
        {
            if (mode == activeMode)
            {
                return;
            }

            if (!CanLeaveCurrentLevel())
            {
                return;
            }

            activeMode = mode;
            currentAsset = null;
            draft = null;
            isDirty = false;
            activeTab = 0;
            RefreshLevels();
            levelList.ClearSelection();
            ShowEmptyState();
        }

        private void UpdateModeButtons()
        {
            int classicCount = allLevels.Count(level => level.Mode == LevelMode.Classic);
            int pveCount = allLevels.Count(level => level.Mode == LevelMode.Pve);
            if (classicModeButton != null)
            {
                classicModeButton.text = $"CLASSIC ({classicCount})";
                classicModeButton.style.backgroundColor = activeMode == LevelMode.Classic
                    ? new Color(0.25f, 0.48f, 0.72f)
                    : StyleKeyword.Null;
                classicModeButton.style.unityFontStyleAndWeight = activeMode == LevelMode.Classic
                    ? FontStyle.Bold
                    : FontStyle.Normal;
            }

            if (pveModeButton != null)
            {
                pveModeButton.text = $"PVE ({pveCount})";
                pveModeButton.style.backgroundColor = activeMode == LevelMode.Pve
                    ? new Color(0.25f, 0.48f, 0.72f)
                    : StyleKeyword.Null;
                pveModeButton.style.unityFontStyleAndWeight = activeMode == LevelMode.Pve
                    ? FontStyle.Bold
                    : FontStyle.Normal;
            }

            if (listTitle != null)
            {
                listTitle.text = activeMode == LevelMode.Classic ? "CLASSIC LEVELS" : "PVE LEVELS";
            }
        }

        private void BindLevelListItem(VisualElement element, int index)
        {
            Label label = (Label)element;
            LevelData level = levels[index];
            label.userData = level;
            label.text = $"Level {level.LevelNumber:000}     {level.BoardWidth}×{level.BoardHeight}";
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
            label.style.paddingLeft = 8f;
        }

        private VisualElement CreateLevelListItem()
        {
            Label label = new Label();
            label.RegisterCallback<PointerDownEvent>(pointerEvent =>
            {
                if (pointerEvent.button == 0 && label.userData is LevelData level)
                {
                    if (level == currentAsset)
                    {
                        SelectLevel(level);
                    }
                    else if (currentAsset == null && draft == null)
                    {
                        SelectLevel(level);
                        int index = levels.IndexOf(level);
                        if (index >= 0)
                        {
                            suppressSelection = true;
                            levelList.SetSelectionWithoutNotify(new[] { index });
                            suppressSelection = false;
                        }

                        pointerEvent.StopImmediatePropagation();
                    }
                }
            });
            return label;
        }

        private void OnLevelSelectionChanged(IEnumerable<object> selection)
        {
            if (suppressSelection)
            {
                return;
            }

            LevelData selected = selection.FirstOrDefault() as LevelData;
            if (selected == null || selected == currentAsset)
            {
                return;
            }

            SelectLevel(selected);
        }

        private void SelectLevel(LevelData selected)
        {
            if (selected == null)
            {
                return;
            }

            if (selected == currentAsset)
            {
                if (draft == null)
                {
                    LoadLevel(selected);
                }
                else
                {
                    BuildDetails();
                    UpdateActionButtons();
                }

                return;
            }

            if (!CanLeaveCurrentLevel())
            {
                RestoreCurrentSelection();
                return;
            }

            LoadLevel(selected);
        }

        private void RestoreAssignedLevel()
        {
            BoardController board = FindBoardController();
            LevelData assignedLevel = board != null ? board.AssignedLevelData : null;
            int index = assignedLevel != null ? levels.IndexOf(assignedLevel) : -1;
            if (index < 0)
            {
                ShowEmptyState();
                return;
            }

            LoadLevel(assignedLevel);
            suppressSelection = true;
            levelList.SetSelectionWithoutNotify(new[] { index });
            suppressSelection = false;
        }

        private void RestoreCurrentSelection()
        {
            suppressSelection = true;
            int index = currentAsset != null ? levels.IndexOf(currentAsset) : -1;
            if (index >= 0)
            {
                levelList.SetSelectionWithoutNotify(new[] { index });
            }
            else
            {
                levelList.ClearSelection();
            }

            suppressSelection = false;
        }

        private void LoadLevel(LevelData level)
        {
            activeMode = level.Mode;
            currentAsset = level;
            draft = LevelDraft.FromAsset(level);
            isDirty = false;
            activeTab = 0;
            BuildDetails();
            UpdateActionButtons();
            UpdateModeButtons();
        }

        private void AddLevel()
        {
            if (!CanLeaveCurrentLevel())
            {
                return;
            }

            currentAsset = null;
            draft = LevelDraft.CreateDefault(GetNextLevelNumber(), catalog, activeMode);
            isDirty = true;
            activeTab = 0;
            suppressSelection = true;
            levelList.ClearSelection();
            suppressSelection = false;
            BuildDetails();
            UpdateActionButtons();
        }

        private void DeleteLevel()
        {
            if (draft == null)
            {
                return;
            }

            if (!CanLeaveCurrentLevel())
            {
                return;
            }

            if (currentAsset == null)
            {
                draft = null;
                isDirty = false;
                ShowEmptyState();
                return;
            }

            bool confirmed = EditorUtility.DisplayDialog(
                "Delete Level",
                $"Delete Level {currentAsset.LevelNumber:000}? The asset will be moved to the system Trash.",
                "Delete",
                "Cancel");
            if (!confirmed)
            {
                return;
            }

            string path = AssetDatabase.GetAssetPath(currentAsset);
            BoardController board = FindBoardController();
            if (board != null && board.AssignedLevelData == currentAsset)
            {
                Undo.RecordObject(board, "Unassign Deleted Level");
                board.SetLevelData(null);
                EditorUtility.SetDirty(board);
                if (board.gameObject.scene.IsValid())
                {
                    EditorSceneManager.MarkSceneDirty(board.gameObject.scene);
                }
            }

            AssetDatabase.MoveAssetToTrash(path);
            currentAsset = null;
            draft = null;
            isDirty = false;
            RefreshLevels();
            ShowEmptyState();
        }

        private void ResetLevel()
        {
            if (draft == null)
            {
                return;
            }

            draft = LevelDraft.CreateDefault(draft.LevelNumber, catalog, draft.Mode);
            MarkDirty();
            BuildDetails();
        }

        private void ApplyDraftFromButton()
        {
            ApplyDraft();
        }

        private void PlayLevel()
        {
            if (draft == null || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            if (FindBoardController() == null)
            {
                EditorUtility.DisplayDialog(
                    "Cannot Play Level",
                    "The active scene does not contain a BoardController.",
                    "Continue Editing");
                return;
            }

            if (!ApplyDraft())
            {
                return;
            }

            EditorApplication.isPlaying = true;
        }

        private bool ApplyDraft()
        {
            if (draft == null)
            {
                return true;
            }

            List<string> errors = ValidateDraft();
            if (errors.Count > 0)
            {
                EditorUtility.DisplayDialog(
                    "Cannot Apply Level",
                    string.Join("\n", errors.Select(error => "• " + error)),
                    "Continue Editing");
                return false;
            }

            EnsureLevelFolder(draft.Mode);
            List<LevelObjectSpawn> spawns = draft.SelectedTypes
                .Select(typeId => new LevelObjectSpawn(typeId, draft.Weights[typeId]))
                .ToList();

            if (currentAsset == null)
            {
                currentAsset = CreateInstance<LevelData>();
                currentAsset.Configure(
                    draft.Mode,
                    draft.LevelNumber,
                    draft.Width,
                    draft.Height,
                    spawns,
                    draft.Moves,
                    draft.TargetScore,
                    draft.PlayerMaxHealth,
                    draft.BotMaxHealth,
                    draft.ScoreToDamageMultiplier,
                    draft.BotDifficulty,
                    draft.BotThinkingTimeMin,
                    draft.BotThinkingTimeMax,
                    draft.BotMoveTieBreak,
                    draft.PlayerRefillDirection,
                    draft.BotRefillDirection);
                string assetPath = AssetDatabase.GenerateUniqueAssetPath(
                    $"{GetModeFolder(draft.Mode)}/Level_{draft.LevelNumber:000}.asset");
                AssetDatabase.CreateAsset(currentAsset, assetPath);
            }
            else
            {
                Undo.RecordObject(currentAsset, "Apply Level Configuration");
                currentAsset.Configure(
                    draft.Mode,
                    draft.LevelNumber,
                    draft.Width,
                    draft.Height,
                    spawns,
                    draft.Moves,
                    draft.TargetScore,
                    draft.PlayerMaxHealth,
                    draft.BotMaxHealth,
                    draft.ScoreToDamageMultiplier,
                    draft.BotDifficulty,
                    draft.BotThinkingTimeMin,
                    draft.BotThinkingTimeMax,
                    draft.BotMoveTieBreak,
                    draft.PlayerRefillDirection,
                    draft.BotRefillDirection);
                EditorUtility.SetDirty(currentAsset);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            AssignLevelToBoard(currentAsset);
            isDirty = false;
            RefreshLevels();
            RestoreCurrentSelection();
            UpdateActionButtons();
            ShowNotification(new GUIContent($"Level {currentAsset.LevelNumber:000} applied"));
            return true;
        }

        private void AssignLevelToBoard(LevelData level)
        {
            BoardController board = FindBoardController();
            if (board == null)
            {
                return;
            }

            Undo.RecordObject(board, "Assign Level Data");
            board.SetLevelData(level);
            EditorUtility.SetDirty(board);
            if (board.gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(board.gameObject.scene);
            }

            SceneView.RepaintAll();
        }

        private bool CanLeaveCurrentLevel()
        {
            if (!isDirty || draft == null)
            {
                return true;
            }

            int choice = EditorUtility.DisplayDialogComplex(
                "Unsaved Level",
                $"Level {draft.LevelNumber:000} has unapplied changes. Do you want to save them?",
                "Có",
                "Không",
                "Sửa tiếp");
            if (choice == 0)
            {
                return ApplyDraft();
            }

            return choice == 1;
        }

        private List<string> ValidateDraft()
        {
            List<string> errors = new List<string>();
            if (draft.Width < 5 || draft.Width > 12 || draft.Height < 5 || draft.Height > 12)
            {
                errors.Add("Board Width and Height must be between 5 and 12.");
            }

            if (draft.SelectedTypes.Count < 2)
            {
                errors.Add("Select at least 2 objects so the board can generate playable matches.");
            }

            foreach (TileTypeId typeId in draft.SelectedTypes)
            {
                if (!draft.Weights.TryGetValue(typeId, out float weight) || weight <= 0f)
                {
                    errors.Add($"{typeId} must have a spawn ratio greater than 0.");
                }
            }

            if (draft.Mode == LevelMode.Classic && draft.Moves <= 0)
            {
                errors.Add("Moves must be a positive integer.");
            }

            if (draft.Mode == LevelMode.Classic && draft.TargetScore <= 0)
            {
                errors.Add("Target Score must be a positive integer.");
            }

            if (draft.Mode == LevelMode.Pve)
            {
                if (draft.PlayerMaxHealth <= 0 || draft.BotMaxHealth <= 0)
                {
                    errors.Add("Player HP and Bot HP must be positive integers.");
                }

                if (draft.ScoreToDamageMultiplier <= 0f)
                {
                    errors.Add("Score To Damage Multiplier must be greater than 0.");
                }

                if (draft.BotThinkingTimeMin < 0f || draft.BotThinkingTimeMax < draft.BotThinkingTimeMin)
                {
                    errors.Add("Bot Thinking Max must be greater than or equal to Min, and both must be non-negative.");
                }
            }

            return errors;
        }

        private void BuildDetails()
        {
            isBuildingUi = true;
            detailsRoot.Clear();

            Label heading = new Label(currentAsset == null
                ? $"{GetModeLabel(draft.Mode)} — New Level {draft.LevelNumber:000}"
                : $"{GetModeLabel(draft.Mode)} — Level {draft.LevelNumber:000}");
            heading.style.fontSize = 22f;
            heading.style.unityFontStyleAndWeight = FontStyle.Bold;
            heading.style.marginBottom = 8f;
            detailsRoot.Add(heading);

            VisualElement tabs = new VisualElement();
            tabs.style.flexDirection = FlexDirection.Row;
            tabs.style.marginBottom = 8f;
            tabs.Add(CreateTabButton("Board Setting", 0));
            tabs.Add(CreateTabButton("Mechanics", 1));
            tabs.Add(CreateTabButton("Challenge", 2));
            detailsRoot.Add(tabs);

            tabContent = new ScrollView();
            tabContent.style.flexGrow = 1f;
            detailsRoot.Add(tabContent);
            ShowActiveTab();
            isBuildingUi = false;
        }

        private Button CreateTabButton(string label, int tabIndex)
        {
            Button button = new Button(() =>
            {
                activeTab = tabIndex;
                BuildDetails();
            }) { text = label };
            button.style.flexGrow = 1f;
            button.style.height = 32f;
            if (activeTab == tabIndex)
            {
                button.style.backgroundColor = new Color(0.25f, 0.48f, 0.72f);
                button.style.unityFontStyleAndWeight = FontStyle.Bold;
            }

            return button;
        }

        private void ShowActiveTab()
        {
            switch (activeTab)
            {
                case 1:
                    BuildMechanicsTab();
                    break;
                case 2:
                    BuildChallengeTab();
                    break;
                default:
                    BuildBoardTab();
                    break;
            }
        }

        private void BuildBoardTab()
        {
            tabContent.Add(CreateSectionTitle("Board Size"));
            IntegerField widthField = CreateIntegerField("Width", draft.Width, value => draft.Width = value);
            IntegerField heightField = CreateIntegerField("Height", draft.Height, value => draft.Height = value);
            tabContent.Add(widthField);
            tabContent.Add(heightField);

            tabContent.Add(CreateSectionTitle("Objects"));
            Label selectedLabel = new Label($"Available Objects — selected {draft.SelectedTypes.Count}");
            selectedLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            selectedLabel.style.marginTop = 8f;
            tabContent.Add(selectedLabel);

            foreach (TilePoolEntry entry in catalog)
            {
                VisualElement row = CreateObjectRow(entry);
                Toggle toggle = new Toggle(entry.TypeId.ToString());
                toggle.SetValueWithoutNotify(draft.SelectedTypes.Contains(entry.TypeId));
                toggle.style.flexGrow = 1f;
                toggle.RegisterValueChangedCallback(change =>
                {
                    if (change.newValue)
                    {
                        draft.SelectedTypes.Add(entry.TypeId);
                        if (!draft.Weights.ContainsKey(entry.TypeId))
                        {
                            draft.Weights[entry.TypeId] = 1f;
                        }
                    }
                    else
                    {
                        draft.SelectedTypes.Remove(entry.TypeId);
                    }

                    MarkDirty();
                    BuildDetails();
                });
                row.Add(toggle);
                tabContent.Add(row);
            }

            tabContent.Add(CreateSectionTitle("Spawn Ratios"));
            if (draft.SelectedTypes.Count == 0)
            {
                tabContent.Add(new HelpBox("Select objects to configure their spawn ratios.", HelpBoxMessageType.Info));
            }
            else
            {
                foreach (TileTypeId typeId in draft.SelectedTypes)
                {
                    TilePoolEntry entry = catalog.First(item => item.TypeId == typeId);
                    VisualElement row = CreateObjectRow(entry);
                    FloatField weightField = new FloatField("Ratio") { isDelayed = true };
                    weightField.style.flexGrow = 1f;
                    weightField.SetValueWithoutNotify(draft.Weights[typeId]);
                    weightField.RegisterValueChangedCallback(change =>
                    {
                        draft.Weights[typeId] = change.newValue;
                        MarkDirty();
                    });
                    row.Add(weightField);
                    tabContent.Add(row);
                }
            }
        }

        private void BuildMechanicsTab()
        {
            tabContent.Add(CreateSectionTitle("Mechanics"));
            tabContent.Add(new HelpBox(
                "No configurable mechanics are available yet. This tab is reserved for future mechanics.",
                HelpBoxMessageType.Info));
        }

        private void BuildChallengeTab()
        {
            tabContent.Add(CreateSectionTitle("Challenge"));
            if (draft.Mode == LevelMode.Classic)
            {
                tabContent.Add(CreateIntegerField("Moves", draft.Moves, value => draft.Moves = value));
                tabContent.Add(CreateIntegerField("Target Score", draft.TargetScore, value => draft.TargetScore = value));
                return;
            }

            tabContent.Add(CreateIntegerField("Player Max HP", draft.PlayerMaxHealth, value => draft.PlayerMaxHealth = value));
            tabContent.Add(CreateIntegerField("Bot Max HP", draft.BotMaxHealth, value => draft.BotMaxHealth = value));
            tabContent.Add(CreateFloatField(
                "Score To Damage Multiplier",
                draft.ScoreToDamageMultiplier,
                value => draft.ScoreToDamageMultiplier = value));
            tabContent.Add(CreateEnumField(
                "Bot Difficulty",
                draft.BotDifficulty,
                value => draft.BotDifficulty = (PveBotDifficulty)value));
            tabContent.Add(CreateFloatField(
                "Thinking Time Min",
                draft.BotThinkingTimeMin,
                value => draft.BotThinkingTimeMin = value));
            tabContent.Add(CreateFloatField(
                "Thinking Time Max",
                draft.BotThinkingTimeMax,
                value => draft.BotThinkingTimeMax = value));
            tabContent.Add(CreateEnumField(
                "Equal Move Tie Break",
                draft.BotMoveTieBreak,
                value => draft.BotMoveTieBreak = (PveMoveTieBreak)value));
            tabContent.Add(CreateEnumField(
                "Player Refill Direction",
                draft.PlayerRefillDirection,
                value => draft.PlayerRefillDirection = (PveRefillDirection)value));
            tabContent.Add(CreateEnumField(
                "Bot Refill Direction",
                draft.BotRefillDirection,
                value => draft.BotRefillDirection = (PveRefillDirection)value));
        }

        private IntegerField CreateIntegerField(string label, int value, Action<int> setter)
        {
            IntegerField field = new IntegerField(label) { isDelayed = true };
            field.SetValueWithoutNotify(value);
            field.RegisterValueChangedCallback(change =>
            {
                setter(change.newValue);
                MarkDirty();
            });
            return field;
        }

        private FloatField CreateFloatField(string label, float value, Action<float> setter)
        {
            FloatField field = new FloatField(label) { isDelayed = true };
            field.SetValueWithoutNotify(value);
            field.RegisterValueChangedCallback(change =>
            {
                setter(change.newValue);
                MarkDirty();
            });
            return field;
        }

        private EnumField CreateEnumField(string label, Enum value, Action<Enum> setter)
        {
            EnumField field = new EnumField(label, value);
            field.RegisterValueChangedCallback(change =>
            {
                setter(change.newValue);
                MarkDirty();
            });
            return field;
        }

        private static Label CreateSectionTitle(string text)
        {
            Label label = new Label(text);
            label.style.fontSize = 15f;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.marginTop = 8f;
            label.style.marginBottom = 6f;
            return label;
        }

        private static VisualElement CreateObjectRow(TilePoolEntry entry)
        {
            VisualElement row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.height = 36f;
            row.style.marginBottom = 3f;
            row.style.paddingLeft = 5f;
            row.style.paddingRight = 5f;
            row.style.backgroundColor = new Color(0.17f, 0.18f, 0.21f);

            VisualElement preview = new VisualElement();
            preview.style.width = 26f;
            preview.style.height = 26f;
            preview.style.marginRight = 8f;
            preview.style.backgroundColor = entry.PrototypeColor;
            if (entry.Sprite != null)
            {
                preview.style.backgroundImage = new StyleBackground(entry.Sprite);
                preview.style.unityBackgroundImageTintColor = Color.white;
            }

            row.Add(preview);
            return row;
        }

        private void MarkDirty()
        {
            if (isBuildingUi)
            {
                return;
            }

            isDirty = true;
            UpdateActionButtons();
        }

        private void UpdateActionButtons()
        {
            bool hasDraft = draft != null;
            deleteButton?.SetEnabled(hasDraft);
            resetButton?.SetEnabled(hasDraft);
            applyButton?.SetEnabled(hasDraft && isDirty);
            playButton?.SetEnabled(hasDraft && !EditorApplication.isPlayingOrWillChangePlaymode);
        }

        private void ShowEmptyState()
        {
            detailsRoot.Clear();
            Label message = new Label(levels.Count == 0
                ? "No levels yet. Click Add Level to create the first one."
                : "Select a level from the list to edit it.");
            message.style.unityTextAlign = TextAnchor.MiddleCenter;
            message.style.flexGrow = 1f;
            message.style.fontSize = 16f;
            message.style.color = new Color(0.65f, 0.68f, 0.72f);
            detailsRoot.Add(message);
            UpdateActionButtons();
        }

        private int GetNextLevelNumber()
        {
            return levels.Count == 0 ? 1 : levels.Max(level => level.LevelNumber) + 1;
        }

        private static BoardController FindBoardController()
        {
            return UnityEngine.Object.FindObjectOfType<BoardController>();
        }

        private static string GetModeLabel(LevelMode mode)
        {
            return mode == LevelMode.Classic ? "Classic" : "PVE";
        }

        private static string GetModeFolder(LevelMode mode)
        {
            return $"{LevelFolder}/{GetModeLabel(mode)}";
        }

        private static void EnsureLevelFolder(LevelMode mode)
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project"))
            {
                AssetDatabase.CreateFolder("Assets", "_Project");
            }

            if (!AssetDatabase.IsValidFolder(LevelFolder))
            {
                AssetDatabase.CreateFolder("Assets/_Project", "Levels");
            }

            string modeFolder = GetModeFolder(mode);
            if (!AssetDatabase.IsValidFolder(modeFolder))
            {
                AssetDatabase.CreateFolder(LevelFolder, GetModeLabel(mode));
            }
        }

        private static void SyncRuntimeCatalogFromAssets()
        {
            string[] guids = AssetDatabase.FindAssets("t:LevelData", new[] { LevelFolder });
            List<LevelData> assets = new List<LevelData>();
            foreach (string guid in guids)
            {
                LevelData level = AssetDatabase.LoadAssetAtPath<LevelData>(
                    AssetDatabase.GUIDToAssetPath(guid));
                if (level != null)
                {
                    assets.Add(level);
                }
            }

            SyncRuntimeCatalog(assets);
        }

        private static void SyncRuntimeCatalog(IEnumerable<LevelData> assets)
        {
            if (!AssetDatabase.IsValidFolder(ResourcesFolder))
            {
                AssetDatabase.CreateFolder("Assets/_Project", "Resources");
            }

            LevelCatalog runtimeCatalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(RuntimeCatalogPath);
            if (runtimeCatalog == null)
            {
                runtimeCatalog = CreateInstance<LevelCatalog>();
                AssetDatabase.CreateAsset(runtimeCatalog, RuntimeCatalogPath);
            }

            runtimeCatalog.Configure(assets);
            EditorUtility.SetDirty(runtimeCatalog);
            AssetDatabase.SaveAssetIfDirty(runtimeCatalog);
        }

        private sealed class LevelDraft
        {
            public LevelMode Mode;
            public int LevelNumber;
            public int Width;
            public int Height;
            public readonly List<TileTypeId> SelectedTypes = new List<TileTypeId>();
            public readonly Dictionary<TileTypeId, float> Weights = new Dictionary<TileTypeId, float>();
            public int Moves;
            public int TargetScore;
            public int PlayerMaxHealth;
            public int BotMaxHealth;
            public float ScoreToDamageMultiplier;
            public PveBotDifficulty BotDifficulty;
            public float BotThinkingTimeMin;
            public float BotThinkingTimeMax;
            public PveMoveTieBreak BotMoveTieBreak;
            public PveRefillDirection PlayerRefillDirection;
            public PveRefillDirection BotRefillDirection;

            public static LevelDraft CreateDefault(
                int levelNumber,
                IReadOnlyList<TilePoolEntry> entries,
                LevelMode mode)
            {
                LevelDraft result = new LevelDraft
                {
                    Mode = mode,
                    LevelNumber = levelNumber,
                    Width = LevelData.DefaultBoardWidth,
                    Height = LevelData.DefaultBoardHeight,
                    Moves = LevelData.DefaultMoves,
                    TargetScore = LevelData.DefaultTargetScore,
                    PlayerMaxHealth = LevelData.DefaultPveHealth,
                    BotMaxHealth = LevelData.DefaultPveHealth,
                    ScoreToDamageMultiplier = 1f,
                    BotDifficulty = PveBotDifficulty.FirstValidMove,
                    BotThinkingTimeMin = 0.5f,
                    BotThinkingTimeMax = 1.2f,
                    BotMoveTieBreak = PveMoveTieBreak.RandomAmongBest,
                    PlayerRefillDirection = PveRefillDirection.FromTop,
                    BotRefillDirection = PveRefillDirection.FromBottom
                };

                int initialObjectCount = Mathf.Min(LevelData.DefaultObjectCount, entries.Count);
                for (int i = 0; i < initialObjectCount; i++)
                {
                    result.SelectedTypes.Add(entries[i].TypeId);
                    result.Weights[entries[i].TypeId] = 1f;
                }

                return result;
            }

            public static LevelDraft FromAsset(LevelData level)
            {
                LevelDraft result = new LevelDraft
                {
                    Mode = level.Mode,
                    LevelNumber = level.LevelNumber,
                    Width = level.BoardWidth,
                    Height = level.BoardHeight,
                    Moves = level.StartingMoves,
                    TargetScore = level.TargetScore,
                    PlayerMaxHealth = level.PlayerMaxHealth,
                    BotMaxHealth = level.BotMaxHealth,
                    ScoreToDamageMultiplier = level.ScoreToDamageMultiplier,
                    BotDifficulty = level.BotSettings.Difficulty,
                    BotThinkingTimeMin = level.BotSettings.ThinkingTimeMin,
                    BotThinkingTimeMax = level.BotSettings.ThinkingTimeMax,
                    BotMoveTieBreak = level.BotSettings.MoveTieBreak,
                    PlayerRefillDirection = level.PlayerRefillDirection,
                    BotRefillDirection = level.BotRefillDirection
                };

                foreach (LevelObjectSpawn entry in level.Objects)
                {
                    if (!result.SelectedTypes.Contains(entry.TypeId))
                    {
                        result.SelectedTypes.Add(entry.TypeId);
                        result.Weights[entry.TypeId] = entry.SpawnWeight;
                    }
                }

                return result;
            }
        }
    }
}
