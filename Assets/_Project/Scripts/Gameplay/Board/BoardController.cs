using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameMatch3.Gameplay.Board
{
    // Điều phối toàn bộ board: tạo ô/tile, nhận thao tác swap, giải quyết match và gravity.
    // ExecuteAlways giúp xem board ngay trong Scene View mà không cần nhấn Play.
    [ExecuteAlways]
    public sealed class BoardController : MonoBehaviour
    {
        // Cấu hình kích thước bàn chơi, mỗi cell và tốc độ các hiệu ứng qua Inspector.
        [Header("Board Size")]
        [SerializeField, Range(6, 8)] private int width = 6;
        [SerializeField, Range(6, 8)] private int height = 6;
        [SerializeField, Min(0.1f)] private float cellSize = 1f;

        [Header("Presentation")]
        [SerializeField, Min(0.01f)] private float swapDuration = 0.16f;
        [SerializeField, Min(0.01f)] private float clearDuration = 0.16f;
        [SerializeField, Min(0.01f)] private float fallDurationPerCell = 0.08f;
        [SerializeField, Min(0.01f)] private float shuffleDuration = 0.35f;
        [SerializeField, Range(0.5f, 0.98f)] private float tileScale = 0.86f;
        [SerializeField, Range(0.01f, 0.25f)] private float dragThreshold = 0.15f;
        [SerializeField, Min(0.1f)] private float cameraPadding = 0.6f;
        [Tooltip("Khoảng trống bên trái dành cho HUD như Target, Moves và Score.")]
        [SerializeField, Min(0f)] private float leftHudReservedWidth = 3f;
        [SerializeField] private int previewSeed = 12345;

        [Header("Tile Pool")]
        [SerializeField] private TilePool tilePool = new TilePool();

        [Header("Level")]
        [SerializeField] private LevelSettings levelSettings = new LevelSettings();

        // Mảng logic: tiles[x, y] chứa tile tại tọa độ tương ứng, null nghĩa là ô trống.
        private TileView[,] tiles;
        private Sprite squareSprite;
        private Sprite ellipseSprite;
        private Sprite pentagonSprite;
        private Sprite hexagonSprite;
        private Camera boardCamera;
        private Vector2 pointerStartWorld;
        private Vector2Int pointerStartCell;
        private bool pointerStartedOnBoard;
        private bool isSwapping;
        private float lastCameraAspect = -1f;
        private System.Random random;
        private readonly List<TilePoolEntry> activeTilePool = new List<TilePoolEntry>(10);
        private int nextTileInstanceId;
        private BoardHud boardHud;
        private int currentScore;
        private int remainingMoves;
        private bool isLevelFinished;
        private LevelResult levelResult;

        private const int MinimumStartingMoves = 2;
        private const int LayoutGenerationAttempts = 2048;
        private const int ExactShuffleAttempts = 4096;

        public bool IsSwapping => isSwapping;
        public int Width => width;
        public int Height => height;
        public float CellSize => cellSize;
        public int CurrentScore => currentScore;
        public int RemainingMoves => remainingMoves;
        public bool IsLevelFinished => isLevelFinished;
        public LevelResult Result => levelResult;

        private void OnEnable()
        {
            // Dựng lại board mỗi khi Scene hoặc component được nạp.
            RebuildBoard();
        }

        private void OnValidate()
        {
            if (tilePool == null)
            {
                tilePool = new TilePool();
            }

            if (levelSettings == null)
            {
                levelSettings = new LevelSettings();
            }

            tilePool.EnsureCompleteCatalog();
        }

        private void Update()
        {
            // Game View đổi tỉ lệ màn hình thì tính lại khung hình để board không bị cắt.
            if (boardCamera != null && Mathf.Abs(boardCamera.aspect - lastCameraAspect) > 0.001f)
            {
                ConfigureCamera();
            }

            if (Application.isPlaying)
            {
                ReadPointerInput();
            }
        }

        [ContextMenu("Rebuild Board Preview")]
        public void RebuildPreview()
        {
            // Nút dành cho Editor: dựng lại preview theo các thông số hiện tại.
            if (!Application.isPlaying)
            {
                RebuildBoard();
            }
        }

        [ContextMenu("Randomize Board Preview")]
        public void RandomizePreview()
        {
            // Đổi seed để có bố cục màu mới nhưng vẫn cố định trong lần preview này.
            if (!Application.isPlaying)
            {
                previewSeed = unchecked(previewSeed * 397 + 1);
                RebuildBoard();
            }
        }

        public bool TrySwap(Vector2Int first, Vector2Int second)
        {
            // Chỉ cho phép đổi hai ô có tile, kề nhau theo ngang/dọc, khi board đang rảnh.
            if (isLevelFinished
                || isSwapping
                || !IsInside(first)
                || !IsInside(second)
                || !AreAdjacent(first, second))
            {
                return false;
            }

            if (tiles[first.x, first.y] == null || tiles[second.x, second.y] == null)
            {
                return false;
            }

            StartCoroutine(ResolveSwapRoutine(first, second));
            return true;
        }

        public TileView GetTile(Vector2Int coordinate)
        {
            // Trả về tile tại tọa độ yêu cầu; ô ngoài board hoặc ô trống trả về null.
            return tiles != null && IsInside(coordinate) ? tiles[coordinate.x, coordinate.y] : null;
        }

        public Vector3 GetWorldPosition(Vector2Int coordinate)
        {
            // Chuyển tọa độ nguyên của cell sang tâm ô trong không gian Unity.
            float left = -(width * cellSize) * 0.5f + cellSize * 0.5f;
            float bottom = -(height * cellSize) * 0.5f + cellSize * 0.5f;
            return transform.position + new Vector3(
                left + coordinate.x * cellSize,
                bottom + coordinate.y * cellSize,
                0f);
        }

        private void RebuildBoard()
        {
            // Xóa preview cũ, giới hạn kích thước hợp lệ rồi tạo nền và tile mới.
            ClearGeneratedBoard();
            width = Mathf.Clamp(width, 6, 8);
            height = Mathf.Clamp(height, 6, 8);
            cellSize = Mathf.Max(0.1f, cellSize);
            if (tilePool == null)
            {
                tilePool = new TilePool();
            }

            if (levelSettings == null)
            {
                levelSettings = new LevelSettings();
            }

            tilePool.GetActiveEntries(activeTilePool);
            EnsureAtLeastTwoActiveTypes();
            nextTileInstanceId = 1;
            currentScore = 0;
            remainingMoves = levelSettings.StartingMoves;
            isLevelFinished = false;
            levelResult = LevelResult.InProgress;
            tiles = new TileView[width, height];
            squareSprite = CreateSquareSprite();
            ellipseSprite = CreateEllipseSprite();
            pentagonSprite = CreatePolygonSprite(5);
            hexagonSprite = CreatePolygonSprite(6);
            ConfigureCamera();
            CreateHud();
            CreateBoardBackground();
            PopulateBoard();
        }

        private void CreateHud()
        {
            GameObject hudObject = new GameObject(
                "LevelHUD",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(UnityEngine.UI.CanvasScaler));
            hudObject.transform.SetParent(transform, false);
            SetEditorPreviewFlags(hudObject);
            boardHud = hudObject.AddComponent<BoardHud>();
            boardHud.Build(
                levelSettings.LevelNumber,
                levelSettings.TargetScore,
                remainingMoves,
                currentScore);
        }

        private void PopulateBoard()
        {
            // Editor dùng seed cố định để preview ổn định; Play dùng seed thời gian để ngẫu nhiên.
            random = new System.Random(
                Application.isPlaying ? System.Environment.TickCount : previewSeed);

            if (!TryGeneratePlayableLayout(
                    activeTilePool,
                    MinimumStartingMoves,
                    LayoutGenerationAttempts,
                    out TilePoolEntry[,] layout))
            {
                throw new System.InvalidOperationException(
                    "Could not generate a board without matches and with at least two valid moves.");
            }

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Vector2Int coordinate = new Vector2Int(x, y);
                    TilePoolEntry definition = layout[x, y];
                    tiles[x, y] = CreateTile(coordinate, definition, GetWorldPosition(coordinate));
                }
            }
        }

        private bool TryGeneratePlayableLayout(
            IReadOnlyList<TilePoolEntry> candidates,
            int minimumMoves,
            int maximumAttempts,
            out TilePoolEntry[,] result)
        {
            result = null;
            for (int attempt = 0; attempt < maximumAttempts; attempt++)
            {
                TilePoolEntry[,] layout = new TilePoolEntry[width, height];
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        List<TilePoolEntry> allowed = new List<TilePoolEntry>(candidates);
                        if (x >= 2 && layout[x - 1, y].TypeId == layout[x - 2, y].TypeId)
                        {
                            TileTypeId blockedType = layout[x - 1, y].TypeId;
                            allowed.RemoveAll(entry => entry.TypeId == blockedType);
                        }

                        if (y >= 2 && layout[x, y - 1].TypeId == layout[x, y - 2].TypeId)
                        {
                            TileTypeId blockedType = layout[x, y - 1].TypeId;
                            allowed.RemoveAll(entry => entry.TypeId == blockedType);
                        }

                        if (allowed.Count == 0)
                        {
                            allowed.AddRange(candidates);
                        }

                        layout[x, y] = TilePool.PickRandom(random, allowed);
                    }
                }

                TileTypeId[,] typeLayout = CreateTypeLayout(layout);
                if (!BoardAnalyzer.HasAnyMatch(typeLayout)
                    && BoardAnalyzer.CountValidMoves(typeLayout, minimumMoves) >= minimumMoves)
                {
                    result = layout;
                    return true;
                }
            }

            return false;
        }

        private TileView CreateTile(
            Vector2Int coordinate,
            TilePoolEntry definition,
            Vector3 worldPosition)
        {
            GameObject tileObject = new GameObject();
            tileObject.transform.SetParent(transform, false);
            SetEditorPreviewFlags(tileObject);
            tileObject.transform.position = worldPosition;
            tileObject.transform.localScale = Vector3.one * (cellSize * tileScale);

            TileView tile = tileObject.AddComponent<TileView>();
            Tile tileData = new Tile(nextTileInstanceId++, definition.TypeId, coordinate);
            tile.Initialize(tileData, definition, squareSprite);
            return tile;
        }

        private void CreateBoardBackground()
        {
            // Mỗi cell có nền riêng để nhìn thấy cấu trúc lưới, kể cả khi tile bị xóa.
            GameObject backgroundRoot = new GameObject($"Cells_{width}x{height}");
            backgroundRoot.transform.SetParent(transform, false);
            SetEditorPreviewFlags(backgroundRoot);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Vector2Int coordinate = new Vector2Int(x, y);
                    GameObject cell = new GameObject($"Cell_{x}_{y}");
                    cell.transform.SetParent(backgroundRoot.transform, false);
                    SetEditorPreviewFlags(cell);
                    cell.transform.position = GetWorldPosition(coordinate);
                    cell.transform.localScale = Vector3.one * (cellSize * 0.96f);

                    SpriteRenderer renderer = cell.AddComponent<SpriteRenderer>();
                    renderer.sprite = squareSprite;
                    renderer.color = (x + y) % 2 == 0
                        ? new Color(0.17f, 0.19f, 0.24f)
                        : new Color(0.21f, 0.23f, 0.29f);
                    renderer.sortingOrder = 0;
                }
            }
        }

        private void ReadPointerInput()
        {
            // Ghi nhận ô bắt đầu khi nhấn; khi thả, lấy trục kéo mạnh hơn để chọn ô kề.
            if (isSwapping || isLevelFinished)
            {
                return;
            }

            if (TryGetPointerDown(out Vector2 downScreenPosition))
            {
                pointerStartWorld = ScreenToWorld(downScreenPosition);
                pointerStartedOnBoard = TryWorldToCell(pointerStartWorld, out pointerStartCell);
            }

            if (pointerStartedOnBoard && TryGetPointerUp(out Vector2 upScreenPosition))
            {
                Vector2 endWorld = ScreenToWorld(upScreenPosition);
                Vector2 drag = endWorld - pointerStartWorld;
                pointerStartedOnBoard = false;

                if (drag.magnitude < dragThreshold)
                {
                    return;
                }

                Vector2Int direction = Mathf.Abs(drag.x) > Mathf.Abs(drag.y)
                    ? new Vector2Int(drag.x > 0f ? 1 : -1, 0)
                    : new Vector2Int(0, drag.y > 0f ? 1 : -1);

                TrySwap(pointerStartCell, pointerStartCell + direction);
            }
        }

        private IEnumerator ResolveSwapRoutine(Vector2Int first, Vector2Int second)
        {
            isSwapping = true;
            yield return AnimateSwap(first, second);
            SwapTileData(first, second);

            TileView movedTile = tiles[second.x, second.y];
            TileView otherTile = tiles[first.x, first.y];
            bool colorBombSwap = MoveFinder.IsColorBombNormalSwap(movedTile, otherTile);
            MatchResult matches = MatchFinder.FindAll(tiles, width, height);
            if (!colorBombSwap && !matches.HasMatches)
            {
                yield return AnimateSwap(first, second);
                SwapTileData(first, second);
                isSwapping = false;
                yield break;
            }

            ConsumeMove();
            int resolutionIndex = 0;
            if (colorBombSwap)
            {
                yield return ResolveColorBombSwap(
                    movedTile,
                    otherTile,
                    GetCascadeMultiplier(resolutionIndex));
                resolutionIndex++;
                yield return ApplyGravity();
                yield return RefillBoard();
                matches = MatchFinder.FindAll(tiles, width, height);
            }

            bool isPlayerMatch = !colorBombSwap;
            StripedDirection swapDirection = first.x != second.x
                ? StripedDirection.Horizontal
                : StripedDirection.Vertical;

            while (matches.HasMatches)
            {
                yield return ResolveMatchStep(
                    matches,
                    isPlayerMatch ? (Vector2Int?)second : null,
                    swapDirection,
                    isPlayerMatch,
                    GetCascadeMultiplier(resolutionIndex));
                resolutionIndex++;
                yield return ApplyGravity();
                yield return RefillBoard();
                matches = MatchFinder.FindAll(tiles, width, height);
                isPlayerMatch = false;
            }

            if (TryFinishLevel())
            {
                isSwapping = false;
                yield break;
            }

            if (CountCurrentValidMoves() == 0)
            {
                yield return ShuffleBoard();
            }

            isSwapping = false;
        }

        private IEnumerator ResolveColorBombSwap(
            TileView first,
            TileView second,
            int scoreMultiplier)
        {
            TileView bomb = first.SpecialType == SpecialObjectType.ColorBomb ? first : second;
            TileView normal = bomb == first ? second : first;
            HashSet<TileView> tilesToClear = new HashSet<TileView> { bomb };
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    TileView tile = tiles[x, y];
                    if (tile != null && tile.CanMatchByType && tile.TypeId == normal.TypeId)
                    {
                        tilesToClear.Add(tile);
                    }
                }
            }

            ExpandSpecialEffects(tilesToClear, null, normal.TypeId, out bool hasWrappedActivation);
            yield return ClearMatches(tilesToClear, hasWrappedActivation, scoreMultiplier);
        }

        private IEnumerator ResolveMatchStep(
            MatchResult matchResult,
            Vector2Int? preferredCoordinate,
            StripedDirection playerStripedDirection,
            bool isPlayerMatch,
            int scoreMultiplier)
        {
            List<SpecialCreation> creations = new List<SpecialCreation>();
            HashSet<TileView> protectedHosts = new HashSet<TileView>();
            foreach (MatchGroup group in matchResult.Groups)
            {
                if (group.CreatedSpecialType == SpecialObjectType.None)
                {
                    continue;
                }

                TileView host = ChooseSpecialHost(group, preferredCoordinate);
                if (host == null)
                {
                    continue;
                }

                StripedDirection direction = group.CascadeStripedDirection;
                if (group.CreatedSpecialType == SpecialObjectType.Striped)
                {
                    if (isPlayerMatch)
                    {
                        direction = playerStripedDirection;
                    }
                    else if (group.IsSquareOnlyStriped)
                    {
                        direction = random.Next(2) == 0
                            ? StripedDirection.Horizontal
                            : StripedDirection.Vertical;
                    }
                }

                creations.Add(new SpecialCreation(host, group.CreatedSpecialType, direction));
                protectedHosts.Add(host);
            }

            foreach (SpecialCreation creation in creations)
            {
                AddScore(levelSettings.GetCreationBonus(creation.SpecialType));
            }

            HashSet<TileView> tilesToClear = new HashSet<TileView>(matchResult.AllTiles);
            tilesToClear.ExceptWith(protectedHosts);
            ExpandSpecialEffects(tilesToClear, protectedHosts, null, out bool hasWrappedActivation);

            foreach (SpecialCreation creation in creations)
            {
                creation.Host.SetSpecialObject(
                    creation.SpecialType,
                    creation.Direction,
                    GetSpecialModelSprite(creation.SpecialType));
            }

            yield return ClearMatches(tilesToClear, hasWrappedActivation, scoreMultiplier);
        }

        private TileView ChooseSpecialHost(MatchGroup group, Vector2Int? preferredCoordinate)
        {
            if (preferredCoordinate.HasValue)
            {
                foreach (TileView tile in group.Tiles)
                {
                    if (tile.Coordinate == preferredCoordinate.Value
                        && tile.SpecialType == SpecialObjectType.None)
                    {
                        return tile;
                    }
                }
            }

            Vector2 center = Vector2.zero;
            foreach (TileView tile in group.Tiles)
            {
                center += (Vector2)tile.Coordinate;
            }

            center /= group.Tiles.Count;
            TileView selected = null;
            float selectedDistance = float.MaxValue;
            foreach (TileView tile in group.Tiles)
            {
                if (tile.SpecialType != SpecialObjectType.None)
                {
                    continue;
                }

                float distance = ((Vector2)tile.Coordinate - center).sqrMagnitude;
                if (selected == null
                    || tile.Coordinate.y < selected.Coordinate.y
                    || (tile.Coordinate.y == selected.Coordinate.y && distance < selectedDistance))
                {
                    selected = tile;
                    selectedDistance = distance;
                }
            }

            return selected;
        }

        private void ExpandSpecialEffects(
            HashSet<TileView> tilesToClear,
            HashSet<TileView> protectedTiles,
            TileTypeId? colorBombTarget,
            out bool hasWrappedActivation)
        {
            Queue<TileView> pending = new Queue<TileView>();
            HashSet<TileView> activated = new HashSet<TileView>();
            hasWrappedActivation = false;
            foreach (TileView tile in tilesToClear)
            {
                if (tile != null && tile.SpecialType != SpecialObjectType.None)
                {
                    pending.Enqueue(tile);
                }
            }

            while (pending.Count > 0)
            {
                TileView special = pending.Dequeue();
                if (!activated.Add(special))
                {
                    continue;
                }

                switch (special.SpecialType)
                {
                    case SpecialObjectType.Striped:
                        AddStripedArea(special, tilesToClear, protectedTiles, pending, activated);
                        break;
                    case SpecialObjectType.Wrapped:
                        hasWrappedActivation = true;
                        // Hai vu no cung pham vi; xu ly hai lan de san sang cho blocker nhieu layer sau nay.
                        AddWrappedArea(special, tilesToClear, protectedTiles, pending, activated);
                        AddWrappedArea(special, tilesToClear, protectedTiles, pending, activated);
                        break;
                    case SpecialObjectType.ColorBomb:
                        TileTypeId target = colorBombTarget ?? FindMostCommonTileType();
                        AddColorBombArea(target, tilesToClear, protectedTiles, pending, activated);
                        break;
                }
            }
        }

        private void AddStripedArea(
            TileView special,
            HashSet<TileView> clear,
            HashSet<TileView> protectedTiles,
            Queue<TileView> pending,
            HashSet<TileView> activated)
        {
            if (special.StripedDirection == StripedDirection.Horizontal)
            {
                for (int x = 0; x < width; x++)
                {
                    AddEffectTile(tiles[x, special.Coordinate.y], clear, protectedTiles, pending, activated);
                }
            }
            else
            {
                for (int y = 0; y < height; y++)
                {
                    AddEffectTile(tiles[special.Coordinate.x, y], clear, protectedTiles, pending, activated);
                }
            }
        }

        private void AddWrappedArea(
            TileView special,
            HashSet<TileView> clear,
            HashSet<TileView> protectedTiles,
            Queue<TileView> pending,
            HashSet<TileView> activated)
        {
            for (int x = special.Coordinate.x - 1; x <= special.Coordinate.x + 1; x++)
            {
                for (int y = special.Coordinate.y - 1; y <= special.Coordinate.y + 1; y++)
                {
                    if (x >= 0 && x < width && y >= 0 && y < height)
                    {
                        AddEffectTile(tiles[x, y], clear, protectedTiles, pending, activated);
                    }
                }
            }
        }

        private void AddColorBombArea(
            TileTypeId target,
            HashSet<TileView> clear,
            HashSet<TileView> protectedTiles,
            Queue<TileView> pending,
            HashSet<TileView> activated)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    TileView tile = tiles[x, y];
                    if (tile != null && tile.CanMatchByType && tile.TypeId == target)
                    {
                        AddEffectTile(tile, clear, protectedTiles, pending, activated);
                    }
                }
            }
        }

        private static void AddEffectTile(
            TileView tile,
            HashSet<TileView> clear,
            HashSet<TileView> protectedTiles,
            Queue<TileView> pending,
            HashSet<TileView> activated)
        {
            if (tile == null || (protectedTiles != null && protectedTiles.Contains(tile)))
            {
                return;
            }

            clear.Add(tile);
            if (tile.SpecialType != SpecialObjectType.None && !activated.Contains(tile))
            {
                pending.Enqueue(tile);
            }
        }

        private TileTypeId FindMostCommonTileType()
        {
            Dictionary<TileTypeId, int> counts = new Dictionary<TileTypeId, int>();
            TileTypeId selected = activeTilePool[0].TypeId;
            int maximum = 0;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    TileView tile = tiles[x, y];
                    if (tile == null || !tile.CanMatchByType)
                    {
                        continue;
                    }

                    int count = counts.TryGetValue(tile.TypeId, out int current) ? current + 1 : 1;
                    counts[tile.TypeId] = count;
                    if (count > maximum)
                    {
                        maximum = count;
                        selected = tile.TypeId;
                    }
                }
            }

            return selected;
        }

        private IEnumerator ShuffleBoard()
        {
            TileTypeId[,] shuffledLayout;
            bool preservesAllTypes = TryCreateExactShuffledLayout(out shuffledLayout);

            if (!preservesAllTypes)
            {
                // Fallback hiem gap: tim nhieu layout hop le va chon layout doi it Type ID nhat.
                if (!TryCreateMinimumChangeLayout(out shuffledLayout, out int changedTypeCount))
                {
                    throw new System.InvalidOperationException("Could not create a playable shuffled board.");
                }

                Debug.LogWarning(
                    $"The current tile distribution could not be shuffled into a playable board. " +
                    $"Changed {changedTypeCount} Tile Type ID(s) as a fallback.",
                    this);
            }

            yield return ApplyShuffledLayout(shuffledLayout);
        }

        private bool TryCreateExactShuffledLayout(out TileTypeId[,] result)
        {
            List<TileTypeId> types = new List<TileTypeId>(width * height);
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    types.Add(tiles[x, y].TypeId);
                }
            }

            for (int attempt = 0; attempt < ExactShuffleAttempts; attempt++)
            {
                ShuffleList(types);
                TileTypeId[,] candidate = new TileTypeId[width, height];
                int index = 0;
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        candidate[x, y] = types[index++];
                    }
                }

                if (!BoardAnalyzer.HasAnyMatch(candidate)
                    && BoardAnalyzer.CountValidMoves(candidate, MinimumStartingMoves) >= MinimumStartingMoves)
                {
                    result = candidate;
                    return true;
                }
            }

            result = null;
            return false;
        }

        private bool TryCreateMinimumChangeLayout(
            out TileTypeId[,] bestLayout,
            out int bestChangeCount)
        {
            Dictionary<TileTypeId, int> currentCounts = CountCurrentTileTypes();
            bestLayout = null;
            bestChangeCount = int.MaxValue;

            for (int attempt = 0; attempt < LayoutGenerationAttempts; attempt++)
            {
                if (!TryGeneratePlayableLayout(activeTilePool, MinimumStartingMoves, 1, out TilePoolEntry[,] candidate))
                {
                    continue;
                }

                TileTypeId[,] typeLayout = CreateTypeLayout(candidate);
                int changeCount = CountRequiredTypeChanges(currentCounts, typeLayout);
                if (changeCount < bestChangeCount)
                {
                    bestChangeCount = changeCount;
                    bestLayout = typeLayout;
                    if (bestChangeCount == 0)
                    {
                        return true;
                    }
                }
            }

            return bestLayout != null;
        }

        private IEnumerator ApplyShuffledLayout(TileTypeId[,] targetLayout)
        {
            List<TileView> availableTiles = new List<TileView>(width * height);
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    availableTiles.Add(tiles[x, y]);
                }
            }

            // Gan tile cung Type ID truoc de fallback chi doi dung so ID toi thieu cua layout da chon.
            TileView[,] assignments = new TileView[width, height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    TileTypeId targetType = targetLayout[x, y];
                    int tileIndex = availableTiles.FindIndex(tile => tile.TypeId == targetType);
                    if (tileIndex >= 0)
                    {
                        assignments[x, y] = availableTiles[tileIndex];
                        availableTiles.RemoveAt(tileIndex);
                    }
                }
            }

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (assignments[x, y] == null)
                    {
                        assignments[x, y] = availableTiles[0];
                        availableTiles.RemoveAt(0);
                    }
                }
            }

            List<ShuffleMove> moves = new List<ShuffleMove>(width * height);
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    TileTypeId targetType = targetLayout[x, y];
                    TileView tile = assignments[x, y];
                    Vector2Int destination = new Vector2Int(x, y);
                    moves.Add(new ShuffleMove(
                        tile,
                        tile.transform.position,
                        GetWorldPosition(destination)));

                    if (tile.TypeId != targetType)
                    {
                        tile.SetType(targetType, tilePool.GetEntry(targetType), squareSprite);
                    }

                    tiles[x, y] = tile;
                    tile.SetCoordinate(destination);
                }
            }

            float elapsed = 0f;
            while (elapsed < shuffleDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / shuffleDuration));
                foreach (ShuffleMove move in moves)
                {
                    move.Tile.transform.position = Vector3.Lerp(move.Start, move.End, t);
                }

                yield return null;
            }

            foreach (ShuffleMove move in moves)
            {
                move.Tile.transform.position = move.End;
            }
        }

        private void EnsureAtLeastTwoActiveTypes()
        {
            if (activeTilePool.Count >= 2)
            {
                return;
            }

            foreach (TilePoolEntry entry in tilePool.Entries)
            {
                if (!activeTilePool.Exists(active => active.TypeId == entry.TypeId))
                {
                    activeTilePool.Add(entry);
                    break;
                }
            }

            Debug.LogWarning(
                "A playable level needs at least two Tile Type IDs. " +
                "A second catalog type was enabled for this board as a fallback.",
                this);
        }

        private void ConsumeMove()
        {
            remainingMoves = Mathf.Max(0, remainingMoves - 1);
            if (boardHud != null)
            {
                boardHud.SetMoves(remainingMoves);
            }
        }

        private void AddScore(long points)
        {
            if (points <= 0)
            {
                return;
            }

            long updatedScore = currentScore + points;
            currentScore = updatedScore > int.MaxValue ? int.MaxValue : (int)updatedScore;
            if (boardHud != null)
            {
                boardHud.SetScore(currentScore);
            }
        }

        private int GetCascadeMultiplier(int resolutionIndex)
        {
            return Mathf.Min(resolutionIndex + 1, levelSettings.MaximumCascadeMultiplier);
        }

        private bool TryFinishLevel()
        {
            if (currentScore >= levelSettings.TargetScore)
            {
                isLevelFinished = true;
                levelResult = LevelResult.Won;
                Debug.Log(
                    $"Level {levelSettings.LevelNumber} complete. Score: {currentScore}.",
                    this);
                return true;
            }

            if (remainingMoves <= 0)
            {
                isLevelFinished = true;
                levelResult = LevelResult.Lost;
                Debug.Log(
                    $"Level {levelSettings.LevelNumber} failed. Score: {currentScore}/{levelSettings.TargetScore}.",
                    this);
                return true;
            }

            return false;
        }

        private int CountCurrentValidMoves()
        {
            return MoveFinder.CountValidMoves(tiles, width, height);
        }

        private TileTypeId[,] CreateCurrentTypeLayout()
        {
            TileTypeId[,] result = new TileTypeId[width, height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    result[x, y] = tiles[x, y].TypeId;
                }
            }

            return result;
        }

        private static TileTypeId[,] CreateTypeLayout(TilePoolEntry[,] definitions)
        {
            int layoutWidth = definitions.GetLength(0);
            int layoutHeight = definitions.GetLength(1);
            TileTypeId[,] result = new TileTypeId[layoutWidth, layoutHeight];
            for (int x = 0; x < layoutWidth; x++)
            {
                for (int y = 0; y < layoutHeight; y++)
                {
                    result[x, y] = definitions[x, y].TypeId;
                }
            }

            return result;
        }

        private Dictionary<TileTypeId, int> CountCurrentTileTypes()
        {
            Dictionary<TileTypeId, int> counts = new Dictionary<TileTypeId, int>();
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    TileTypeId typeId = tiles[x, y].TypeId;
                    counts[typeId] = counts.TryGetValue(typeId, out int count) ? count + 1 : 1;
                }
            }

            return counts;
        }

        private static int CountRequiredTypeChanges(
            Dictionary<TileTypeId, int> currentCounts,
            TileTypeId[,] targetLayout)
        {
            Dictionary<TileTypeId, int> targetCounts = new Dictionary<TileTypeId, int>();
            foreach (TileTypeId typeId in targetLayout)
            {
                targetCounts[typeId] = targetCounts.TryGetValue(typeId, out int count) ? count + 1 : 1;
            }

            int unchangedTiles = 0;
            foreach (KeyValuePair<TileTypeId, int> pair in currentCounts)
            {
                if (targetCounts.TryGetValue(pair.Key, out int targetCount))
                {
                    unchangedTiles += Mathf.Min(pair.Value, targetCount);
                }
            }

            return targetLayout.Length - unchangedTiles;
        }

        private void ShuffleList<T>(List<T> values)
        {
            for (int i = values.Count - 1; i > 0; i--)
            {
                int swapIndex = random.Next(i + 1);
                T value = values[i];
                values[i] = values[swapIndex];
                values[swapIndex] = value;
            }
        }

        private IEnumerator AnimateSwap(Vector2Int first, Vector2Int second)
        {
            // Chỉ di chuyển hình ảnh của hai tile; dữ liệu trong mảng được đổi riêng sau animation.
            TileView firstTile = tiles[first.x, first.y];
            TileView secondTile = tiles[second.x, second.y];
            if (firstTile == null || secondTile == null)
            {
                yield break;
            }

            Vector3 firstStart = firstTile.transform.position;
            Vector3 secondStart = secondTile.transform.position;
            float elapsed = 0f;

            while (elapsed < swapDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / swapDuration));
                firstTile.transform.position = Vector3.Lerp(firstStart, secondStart, t);
                secondTile.transform.position = Vector3.Lerp(secondStart, firstStart, t);
                yield return null;
            }

            firstTile.transform.position = secondStart;
            secondTile.transform.position = firstStart;
        }

        private void SwapTileData(Vector2Int first, Vector2Int second)
        {
            // Đồng bộ vị trí tile trong mảng và tọa độ lưu trên từng TileView.
            TileView firstTile = tiles[first.x, first.y];
            TileView secondTile = tiles[second.x, second.y];
            tiles[first.x, first.y] = secondTile;
            tiles[second.x, second.y] = firstTile;

            if (firstTile != null)
            {
                firstTile.SetCoordinate(second);
            }

            if (secondTile != null)
            {
                secondTile.SetCoordinate(first);
            }
        }

        private IEnumerator ClearMatches(
            HashSet<TileView> matches,
            bool hasWrappedActivation,
            int scoreMultiplier)
        {
            AddScore((long)matches.Count * levelSettings.PointsPerClearedTile * scoreMultiplier);

            // Đánh dấu các ô đã xóa là trống trước khi chạy hiệu ứng thu nhỏ.
            foreach (TileView tile in matches)
            {
                Vector2Int coordinate = tile.Coordinate;
                if (IsInside(coordinate) && tiles[coordinate.x, coordinate.y] == tile)
                {
                    tiles[coordinate.x, coordinate.y] = null;
                }
            }

            if (hasWrappedActivation)
            {
                float pulseElapsed = 0f;
                float pulseDuration = clearDuration * 0.75f;
                while (pulseElapsed < pulseDuration)
                {
                    pulseElapsed += Time.deltaTime;
                    float pulse = Mathf.Sin(Mathf.Clamp01(pulseElapsed / pulseDuration) * Mathf.PI);
                    foreach (TileView tile in matches)
                    {
                        if (tile != null)
                        {
                            tile.transform.localScale = Vector3.one
                                * (cellSize * tileScale * (1f - pulse * 0.25f));
                        }
                    }

                    yield return null;
                }
            }

            float elapsed = 0f;
            while (elapsed < clearDuration)
            {
                elapsed += Time.deltaTime;
                float scale = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / clearDuration));
                foreach (TileView tile in matches)
                {
                    if (tile != null)
                    {
                        tile.transform.localScale = Vector3.one * (cellSize * tileScale * scale);
                    }
                }

                yield return null;
            }

            foreach (TileView tile in matches)
            {
                // Hủy GameObject sau hiệu ứng; refill sẽ tạo tile mới sau gravity.
                if (tile != null)
                {
                    Destroy(tile.gameObject);
                }
            }
        }

        private Sprite GetSpecialModelSprite(SpecialObjectType specialType)
        {
            switch (specialType)
            {
                case SpecialObjectType.Striped:
                    return ellipseSprite;
                case SpecialObjectType.Wrapped:
                    return pentagonSprite;
                case SpecialObjectType.ColorBomb:
                    return hexagonSprite;
                default:
                    return squareSprite;
            }
        }

        private IEnumerator ApplyGravity()
        {
            // Duyệt từng cột từ dưới lên và dồn tile về vị trí trống thấp nhất.
            List<FallMove> moves = new List<FallMove>();

            for (int x = 0; x < width; x++)
            {
                int destinationY = 0;
                for (int sourceY = 0; sourceY < height; sourceY++)
                {
                    TileView tile = tiles[x, sourceY];
                    if (tile == null)
                    {
                        continue;
                    }

                    if (sourceY != destinationY)
                    {
                        Vector2Int destination = new Vector2Int(x, destinationY);
                        moves.Add(new FallMove(
                            tile,
                            tile.transform.position,
                            GetWorldPosition(destination),
                            sourceY - destinationY));

                        tiles[x, destinationY] = tile;
                        tiles[x, sourceY] = null;
                        tile.SetCoordinate(destination);
                    }

                    destinationY++;
                }
            }

            if (moves.Count == 0)
            {
                yield break;
            }

            int maximumDistance = 1;
            foreach (FallMove move in moves)
            {
                maximumDistance = Mathf.Max(maximumDistance, move.Distance);
            }

            float duration = fallDurationPerCell * maximumDistance;
            // Di chuyển đồng thời tất cả tile rơi rồi chốt đúng tọa độ đích.
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                foreach (FallMove move in moves)
                {
                    if (move.Tile != null)
                    {
                        move.Tile.transform.position = Vector3.Lerp(move.Start, move.End, t);
                    }
                }

                yield return null;
            }

            foreach (FallMove move in moves)
            {
                if (move.Tile != null)
                {
                    move.Tile.transform.position = move.End;
                }
            }
        }

        private IEnumerator RefillBoard()
        {
            // Sau gravity, ô trống của mỗi cột nằm liên tiếp ở phía trên.
            List<FallMove> moves = new List<FallMove>();
            for (int x = 0; x < width; x++)
            {
                int firstEmptyY = 0;
                while (firstEmptyY < height && tiles[x, firstEmptyY] != null)
                {
                    firstEmptyY++;
                }

                for (int y = firstEmptyY; y < height; y++)
                {
                    Vector2Int coordinate = new Vector2Int(x, y);
                    int spawnY = height + y - firstEmptyY;
                    Vector3 spawnPosition = GetWorldPosition(new Vector2Int(x, spawnY));
                    Vector3 destination = GetWorldPosition(coordinate);
                    TilePoolEntry definition = TilePool.PickRandom(random, activeTilePool);
                    TileView tile = CreateTile(coordinate, definition, spawnPosition);
                    tiles[x, y] = tile;
                    moves.Add(new FallMove(tile, spawnPosition, destination, spawnY - y));
                }
            }

            if (moves.Count == 0)
            {
                yield break;
            }

            int maximumDistance = 1;
            foreach (FallMove move in moves)
            {
                maximumDistance = Mathf.Max(maximumDistance, move.Distance);
            }

            float duration = fallDurationPerCell * maximumDistance;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                foreach (FallMove move in moves)
                {
                    if (move.Tile != null)
                    {
                        move.Tile.transform.position = Vector3.Lerp(move.Start, move.End, t);
                    }
                }

                yield return null;
            }

            foreach (FallMove move in moves)
            {
                if (move.Tile != null)
                {
                    move.Tile.transform.position = move.End;
                }
            }
        }

        private readonly struct ShuffleMove
        {
            public readonly TileView Tile;
            public readonly Vector3 Start;
            public readonly Vector3 End;

            public ShuffleMove(TileView tile, Vector3 start, Vector3 end)
            {
                Tile = tile;
                Start = start;
                End = end;
            }
        }

        private readonly struct SpecialCreation
        {
            public readonly TileView Host;
            public readonly SpecialObjectType SpecialType;
            public readonly StripedDirection Direction;

            public SpecialCreation(
                TileView host,
                SpecialObjectType specialType,
                StripedDirection direction)
            {
                Host = host;
                SpecialType = specialType;
                Direction = direction;
            }
        }

        private readonly struct FallMove
        {
            // Dữ liệu tạm phục vụ animation rơi; không phải dữ liệu save của game.
            public readonly TileView Tile;
            public readonly Vector3 Start;
            public readonly Vector3 End;
            public readonly int Distance;

            public FallMove(TileView tile, Vector3 start, Vector3 end, int distance)
            {
                Tile = tile;
                Start = start;
                End = end;
                Distance = distance;
            }
        }

        private void ConfigureCamera()
        {
            // Căn camera để đủ chiều cao board và đủ chiều rộng gồm board + khoảng HUD bên trái.
            boardCamera = Camera.main;
            if (boardCamera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                boardCamera = cameraObject.AddComponent<Camera>();
            }

            boardCamera.orthographic = true;
            float aspect = boardCamera.aspect > 0f ? boardCamera.aspect : 1f;
            float verticalHalfSize = height * cellSize * 0.5f + cameraPadding;
            float totalLayoutWidth = width * cellSize + leftHudReservedWidth + cameraPadding * 2f;
            float horizontalHalfSize = totalLayoutWidth * 0.5f / aspect;
            boardCamera.orthographicSize = Mathf.Max(verticalHalfSize, horizontalHalfSize);
            boardCamera.transform.position = new Vector3(
                transform.position.x - leftHudReservedWidth * 0.5f,
                transform.position.y,
                -10f);
            boardCamera.backgroundColor = new Color(0.20f, 0.55f, 0.72f);
            boardCamera.clearFlags = CameraClearFlags.SolidColor;
            lastCameraAspect = boardCamera.aspect;
        }

        private void ClearGeneratedBoard()
        {
            // Xóa các object tạo tự động trước khi dựng preview khác, tránh chồng nhiều board.
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = transform.GetChild(i).gameObject;
                if (Application.isPlaying)
                {
                    Destroy(child);
                }
                else
                {
                    DestroyImmediate(child);
                }
            }

            if (tiles != null)
            {
                System.Array.Clear(tiles, 0, tiles.Length);
            }

            if (squareSprite != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(squareSprite);
                }
                else
                {
                    DestroyImmediate(squareSprite);
                }
            }

            DestroyGeneratedSprite(ellipseSprite);
            DestroyGeneratedSprite(pentagonSprite);
            DestroyGeneratedSprite(hexagonSprite);
            ellipseSprite = null;
            pentagonSprite = null;
            hexagonSprite = null;
        }

        private static void SetEditorPreviewFlags(GameObject gameObject)
        {
            // Các ô/tile preview chỉ tồn tại trong Editor, không ghi hàng loạt vào file Scene.
            if (!Application.isPlaying)
            {
                gameObject.hideFlags = HideFlags.DontSaveInEditor;
            }
        }

        private bool TryWorldToCell(Vector2 worldPosition, out Vector2Int coordinate)
        {
            // Quy đổi vị trí chuột/chạm sang chỉ số cell (x, y) và kiểm tra biên board.
            Vector2 local = worldPosition - (Vector2)transform.position;
            int x = Mathf.FloorToInt((local.x + width * cellSize * 0.5f) / cellSize);
            int y = Mathf.FloorToInt((local.y + height * cellSize * 0.5f) / cellSize);
            coordinate = new Vector2Int(x, y);
            return IsInside(coordinate);
        }

        private Vector2 ScreenToWorld(Vector2 screenPosition)
        {
            // Input trả tọa độ màn hình; gameplay cần tọa độ thế giới của camera 2D.
            Vector3 world = boardCamera.ScreenToWorldPoint(
                new Vector3(screenPosition.x, screenPosition.y, -boardCamera.transform.position.z));
            return world;
        }

        private static bool TryGetPointerDown(out Vector2 screenPosition)
        {
            // Ưu tiên cảm ứng trên mobile, sau đó dùng chuột trên PC.
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                screenPosition = Input.GetTouch(0).position;
                return true;
            }

            if (Input.GetMouseButtonDown(0))
            {
                screenPosition = Input.mousePosition;
                return true;
            }

            screenPosition = default;
            return false;
        }

        private static bool TryGetPointerUp(out Vector2 screenPosition)
        {
            // Lấy điểm thả để suy ra hướng vuốt/kéo.
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    screenPosition = touch.position;
                    return true;
                }
            }

            if (Input.GetMouseButtonUp(0))
            {
                screenPosition = Input.mousePosition;
                return true;
            }

            screenPosition = default;
            return false;
        }

        private bool IsInside(Vector2Int coordinate)
        {
            // Chặn mọi thao tác vượt ra ngoài kích thước board hiện tại.
            return coordinate.x >= 0 && coordinate.x < width
                && coordinate.y >= 0 && coordinate.y < height;
        }

        private static bool AreAdjacent(Vector2Int first, Vector2Int second)
        {
            // Khoảng cách Manhattan bằng 1 nghĩa là hai ô kề ngang hoặc dọc, không phải chéo.
            return Mathf.Abs(first.x - second.x) + Mathf.Abs(first.y - second.y) == 1;
        }

        private static Sprite CreateSquareSprite()
        {
            // Tạo sprite hình vuông từ texture trắng có sẵn, không cần asset ảnh bên ngoài.
            return Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0f, 0f, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
                new Vector2(0.5f, 0.5f),
                Texture2D.whiteTexture.width);
        }

        private static Sprite CreateEllipseSprite()
        {
            const int size = 64;
            return CreateShapeSprite(size, (x, y) =>
            {
                float normalizedX = (x + 0.5f) / size * 2f - 1f;
                float normalizedY = (y + 0.5f) / size * 2f - 1f;
                return normalizedX * normalizedX / (0.90f * 0.90f)
                    + normalizedY * normalizedY / (0.62f * 0.62f) <= 1f;
            }, "Special_Ellipse");
        }

        private static Sprite CreatePolygonSprite(int sides)
        {
            const int size = 64;
            Vector2[] vertices = new Vector2[sides];
            for (int i = 0; i < sides; i++)
            {
                float angle = Mathf.PI * 0.5f + i * Mathf.PI * 2f / sides;
                vertices[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 0.9f;
            }

            return CreateShapeSprite(size, (x, y) =>
            {
                Vector2 point = new Vector2(
                    (x + 0.5f) / size * 2f - 1f,
                    (y + 0.5f) / size * 2f - 1f);
                return IsInsidePolygon(point, vertices);
            }, $"Special_{sides}Sides");
        }

        private static Sprite CreateShapeSprite(
            int size,
            System.Func<int, int, bool> containsPixel,
            string spriteName)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = $"{spriteName}_Texture",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = Application.isPlaying ? HideFlags.None : HideFlags.DontSaveInEditor
            };

            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    pixels[y * size + x] = containsPixel(x, y) ? Color.white : Color.clear;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                size);
            sprite.name = spriteName;
            sprite.hideFlags = Application.isPlaying ? HideFlags.None : HideFlags.DontSaveInEditor;
            return sprite;
        }

        private static bool IsInsidePolygon(Vector2 point, Vector2[] vertices)
        {
            bool inside = false;
            for (int i = 0, previous = vertices.Length - 1; i < vertices.Length; previous = i++)
            {
                Vector2 currentVertex = vertices[i];
                Vector2 previousVertex = vertices[previous];
                bool crosses = (currentVertex.y > point.y) != (previousVertex.y > point.y)
                    && point.x < (previousVertex.x - currentVertex.x)
                        * (point.y - currentVertex.y)
                        / (previousVertex.y - currentVertex.y)
                        + currentVertex.x;
                if (crosses)
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        private static void DestroyGeneratedSprite(Sprite sprite)
        {
            if (sprite == null)
            {
                return;
            }

            Texture2D texture = sprite.texture;
            if (Application.isPlaying)
            {
                Destroy(sprite);
                Destroy(texture);
            }
            else
            {
                DestroyImmediate(sprite);
                DestroyImmediate(texture);
            }
        }
    }
}
