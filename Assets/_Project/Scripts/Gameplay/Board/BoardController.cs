using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameMatch3.Gameplay.Board
{
    [ExecuteAlways]
    public sealed class BoardController : MonoBehaviour
    {
        [Header("Board Size")]
        [SerializeField, Range(6, 8)] private int width = 6;
        [SerializeField, Range(6, 8)] private int height = 6;
        [SerializeField, Min(0.1f)] private float cellSize = 1f;

        [Header("Presentation")]
        [SerializeField, Min(0.01f)] private float swapDuration = 0.16f;
        [SerializeField, Min(0.01f)] private float clearDuration = 0.16f;
        [SerializeField, Min(0.01f)] private float fallDurationPerCell = 0.08f;
        [SerializeField, Range(0.5f, 0.98f)] private float tileScale = 0.86f;
        [SerializeField, Range(0.01f, 0.25f)] private float dragThreshold = 0.15f;
        [SerializeField, Min(0.1f)] private float cameraPadding = 0.6f;
        [Tooltip("Khoảng trống bên trái dành cho HUD như Target, Moves và Score.")]
        [SerializeField, Min(0f)] private float leftHudReservedWidth = 3f;
        [SerializeField] private int previewSeed = 12345;

        private TileView[,] tiles;
        private Sprite squareSprite;
        private Camera boardCamera;
        private Vector2 pointerStartWorld;
        private Vector2Int pointerStartCell;
        private bool pointerStartedOnBoard;
        private bool isSwapping;
        private float lastCameraAspect = -1f;

        public bool IsSwapping => isSwapping;
        public int Width => width;
        public int Height => height;
        public float CellSize => cellSize;

        private void OnEnable()
        {
            RebuildBoard();
        }

        private void Update()
        {
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
            if (!Application.isPlaying)
            {
                RebuildBoard();
            }
        }

        [ContextMenu("Randomize Board Preview")]
        public void RandomizePreview()
        {
            if (!Application.isPlaying)
            {
                previewSeed = unchecked(previewSeed * 397 + 1);
                RebuildBoard();
            }
        }

        public bool TrySwap(Vector2Int first, Vector2Int second)
        {
            if (isSwapping || !IsInside(first) || !IsInside(second) || !AreAdjacent(first, second))
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
            return tiles != null && IsInside(coordinate) ? tiles[coordinate.x, coordinate.y] : null;
        }

        public Vector3 GetWorldPosition(Vector2Int coordinate)
        {
            float left = -(width * cellSize) * 0.5f + cellSize * 0.5f;
            float bottom = -(height * cellSize) * 0.5f + cellSize * 0.5f;
            return transform.position + new Vector3(
                left + coordinate.x * cellSize,
                bottom + coordinate.y * cellSize,
                0f);
        }

        private void RebuildBoard()
        {
            ClearGeneratedBoard();
            width = Mathf.Clamp(width, 6, 8);
            height = Mathf.Clamp(height, 6, 8);
            cellSize = Mathf.Max(0.1f, cellSize);
            tiles = new TileView[width, height];
            squareSprite = CreateSquareSprite();
            ConfigureCamera();
            CreateBoardBackground();
            PopulateBoard();
        }

        private void PopulateBoard()
        {
            System.Random random = new System.Random(
                Application.isPlaying ? System.Environment.TickCount : previewSeed);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Vector2Int coordinate = new Vector2Int(x, y);
                    TileColor randomColor = GetStartingColor(random, x, y);
                    GameObject tileObject = new GameObject();
                    tileObject.transform.SetParent(transform, false);
                    SetEditorPreviewFlags(tileObject);
                    tileObject.transform.position = GetWorldPosition(coordinate);
                    tileObject.transform.localScale = Vector3.one * (cellSize * tileScale);

                    TileView tile = tileObject.AddComponent<TileView>();
                    tile.Initialize(coordinate, randomColor, squareSprite);
                    tiles[x, y] = tile;
                }
            }
        }

        private TileColor GetStartingColor(System.Random random, int x, int y)
        {
            List<TileColor> candidates = new List<TileColor>
            {
                TileColor.Red,
                TileColor.Yellow,
                TileColor.Green,
                TileColor.Blue
            };

            if (x >= 2
                && tiles[x - 1, y] != null
                && tiles[x - 2, y] != null
                && tiles[x - 1, y].ColorType == tiles[x - 2, y].ColorType)
            {
                candidates.Remove(tiles[x - 1, y].ColorType);
            }

            if (y >= 2
                && tiles[x, y - 1] != null
                && tiles[x, y - 2] != null
                && tiles[x, y - 1].ColorType == tiles[x, y - 2].ColorType)
            {
                candidates.Remove(tiles[x, y - 1].ColorType);
            }

            return candidates[random.Next(candidates.Count)];
        }

        private void CreateBoardBackground()
        {
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
            if (isSwapping)
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

            HashSet<TileView> matches = MatchFinder.FindAll(tiles, width, height);
            if (matches.Count == 0)
            {
                yield return AnimateSwap(first, second);
                SwapTileData(first, second);
                isSwapping = false;
                yield break;
            }

            while (matches.Count > 0)
            {
                yield return ClearMatches(matches);
                yield return ApplyGravity();
                matches = MatchFinder.FindAll(tiles, width, height);
            }

            isSwapping = false;
        }

        private IEnumerator AnimateSwap(Vector2Int first, Vector2Int second)
        {
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

        private IEnumerator ClearMatches(HashSet<TileView> matches)
        {
            foreach (TileView tile in matches)
            {
                Vector2Int coordinate = tile.Coordinate;
                if (IsInside(coordinate) && tiles[coordinate.x, coordinate.y] == tile)
                {
                    tiles[coordinate.x, coordinate.y] = null;
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
                if (tile != null)
                {
                    Destroy(tile.gameObject);
                }
            }
        }

        private IEnumerator ApplyGravity()
        {
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

        private readonly struct FallMove
        {
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
        }

        private static void SetEditorPreviewFlags(GameObject gameObject)
        {
            if (!Application.isPlaying)
            {
                gameObject.hideFlags = HideFlags.DontSaveInEditor;
            }
        }

        private bool TryWorldToCell(Vector2 worldPosition, out Vector2Int coordinate)
        {
            Vector2 local = worldPosition - (Vector2)transform.position;
            int x = Mathf.FloorToInt((local.x + width * cellSize * 0.5f) / cellSize);
            int y = Mathf.FloorToInt((local.y + height * cellSize * 0.5f) / cellSize);
            coordinate = new Vector2Int(x, y);
            return IsInside(coordinate);
        }

        private Vector2 ScreenToWorld(Vector2 screenPosition)
        {
            Vector3 world = boardCamera.ScreenToWorldPoint(
                new Vector3(screenPosition.x, screenPosition.y, -boardCamera.transform.position.z));
            return world;
        }

        private static bool TryGetPointerDown(out Vector2 screenPosition)
        {
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
            return coordinate.x >= 0 && coordinate.x < width
                && coordinate.y >= 0 && coordinate.y < height;
        }

        private static bool AreAdjacent(Vector2Int first, Vector2Int second)
        {
            return Mathf.Abs(first.x - second.x) + Mathf.Abs(first.y - second.y) == 1;
        }

        private static Sprite CreateSquareSprite()
        {
            return Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0f, 0f, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
                new Vector2(0.5f, 0.5f),
                Texture2D.whiteTexture.width);
        }
    }
}
