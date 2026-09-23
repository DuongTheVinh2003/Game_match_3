using System.Collections.Generic;
using UnityEngine;

namespace GameMatch3.Gameplay.Board
{
    public sealed class MatchGroup
    {
        public TileTypeId TypeId { get; }
        public HashSet<TileView> Tiles { get; }
        public SpecialObjectType CreatedSpecialType { get; internal set; }
        public StripedDirection CascadeStripedDirection { get; internal set; }
        public bool IsSquareOnlyStriped { get; internal set; }

        internal MatchGroup(TileTypeId typeId)
        {
            TypeId = typeId;
            Tiles = new HashSet<TileView>();
            CreatedSpecialType = SpecialObjectType.None;
        }
    }

    public sealed class MatchResult
    {
        public List<MatchGroup> Groups { get; } = new List<MatchGroup>();
        public HashSet<TileView> AllTiles { get; } = new HashSet<TileView>();
        public bool HasMatches => AllTiles.Count > 0;
    }

    internal sealed class MatchRun
    {
        public readonly List<TileView> Tiles = new List<TileView>();
        public readonly bool Horizontal;

        public MatchRun(bool horizontal)
        {
            Horizontal = horizontal;
        }
    }

    // Tim match thuong, match 4, 2x2, L/T/+ va duong 5+ theo Type ID.
    public static class MatchFinder
    {
        public static MatchResult FindAll(TileView[,] tiles, int width, int height)
        {
            List<MatchRun> runs = FindRuns(tiles, width, height);
            List<HashSet<TileView>> squares = FindSquares(tiles, width, height);
            HashSet<TileView> matched = new HashSet<TileView>();

            foreach (MatchRun run in runs)
            {
                matched.UnionWith(run.Tiles);
            }

            foreach (HashSet<TileView> square in squares)
            {
                matched.UnionWith(square);
            }

            MatchResult result = new MatchResult();
            result.AllTiles.UnionWith(matched);
            HashSet<TileView> unvisited = new HashSet<TileView>(matched);
            while (unvisited.Count > 0)
            {
                TileView seed = null;
                foreach (TileView candidate in unvisited)
                {
                    seed = candidate;
                    break;
                }

                MatchGroup group = BuildConnectedGroup(tiles, width, height, seed, unvisited);
                ClassifyGroup(group, runs, squares);
                result.Groups.Add(group);
            }

            return result;
        }

        public static bool HasMatchAt(TileView[,] tiles, int x, int y)
        {
            TileView center = tiles[x, y];
            if (center == null || !center.CanMatchByType)
            {
                return false;
            }

            int width = tiles.GetLength(0);
            int height = tiles.GetLength(1);
            int horizontal = 1;
            for (int checkX = x - 1; checkX >= 0 && SameType(center, tiles[checkX, y]); checkX--)
            {
                horizontal++;
            }

            for (int checkX = x + 1; checkX < width && SameType(center, tiles[checkX, y]); checkX++)
            {
                horizontal++;
            }

            if (horizontal >= 3)
            {
                return true;
            }

            int vertical = 1;
            for (int checkY = y - 1; checkY >= 0 && SameType(center, tiles[x, checkY]); checkY--)
            {
                vertical++;
            }

            for (int checkY = y + 1; checkY < height && SameType(center, tiles[x, checkY]); checkY++)
            {
                vertical++;
            }

            if (vertical >= 3)
            {
                return true;
            }

            for (int anchorX = x - 1; anchorX <= x; anchorX++)
            {
                for (int anchorY = y - 1; anchorY <= y; anchorY++)
                {
                    if (anchorX >= 0 && anchorY >= 0 && anchorX + 1 < width && anchorY + 1 < height
                        && SameType(center, tiles[anchorX, anchorY])
                        && SameType(center, tiles[anchorX + 1, anchorY])
                        && SameType(center, tiles[anchorX, anchorY + 1])
                        && SameType(center, tiles[anchorX + 1, anchorY + 1]))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static List<MatchRun> FindRuns(TileView[,] tiles, int width, int height)
        {
            List<MatchRun> runs = new List<MatchRun>();
            for (int y = 0; y < height; y++)
            {
                int start = 0;
                while (start < width)
                {
                    TileView seed = tiles[start, y];
                    int end = start + 1;
                    while (end < width && SameType(seed, tiles[end, y]))
                    {
                        end++;
                    }

                    if (seed != null && seed.CanMatchByType && end - start >= 3)
                    {
                        MatchRun run = new MatchRun(true);
                        for (int x = start; x < end; x++)
                        {
                            run.Tiles.Add(tiles[x, y]);
                        }

                        runs.Add(run);
                    }

                    start = end;
                }
            }

            for (int x = 0; x < width; x++)
            {
                int start = 0;
                while (start < height)
                {
                    TileView seed = tiles[x, start];
                    int end = start + 1;
                    while (end < height && SameType(seed, tiles[x, end]))
                    {
                        end++;
                    }

                    if (seed != null && seed.CanMatchByType && end - start >= 3)
                    {
                        MatchRun run = new MatchRun(false);
                        for (int y = start; y < end; y++)
                        {
                            run.Tiles.Add(tiles[x, y]);
                        }

                        runs.Add(run);
                    }

                    start = end;
                }
            }

            return runs;
        }

        private static List<HashSet<TileView>> FindSquares(TileView[,] tiles, int width, int height)
        {
            List<HashSet<TileView>> squares = new List<HashSet<TileView>>();
            for (int x = 0; x < width - 1; x++)
            {
                for (int y = 0; y < height - 1; y++)
                {
                    TileView seed = tiles[x, y];
                    if (seed != null
                        && seed.CanMatchByType
                        && SameType(seed, tiles[x + 1, y])
                        && SameType(seed, tiles[x, y + 1])
                        && SameType(seed, tiles[x + 1, y + 1]))
                    {
                        squares.Add(new HashSet<TileView>
                        {
                            seed,
                            tiles[x + 1, y],
                            tiles[x, y + 1],
                            tiles[x + 1, y + 1]
                        });
                    }
                }
            }

            return squares;
        }

        private static MatchGroup BuildConnectedGroup(
            TileView[,] tiles,
            int width,
            int height,
            TileView seed,
            HashSet<TileView> unvisited)
        {
            MatchGroup group = new MatchGroup(seed.TypeId);
            Queue<TileView> pending = new Queue<TileView>();
            pending.Enqueue(seed);
            unvisited.Remove(seed);
            Vector2Int[] directions =
            {
                Vector2Int.left,
                Vector2Int.right,
                Vector2Int.down,
                Vector2Int.up
            };

            while (pending.Count > 0)
            {
                TileView tile = pending.Dequeue();
                group.Tiles.Add(tile);
                foreach (Vector2Int direction in directions)
                {
                    Vector2Int coordinate = tile.Coordinate + direction;
                    if (coordinate.x < 0 || coordinate.x >= width || coordinate.y < 0 || coordinate.y >= height)
                    {
                        continue;
                    }

                    TileView neighbor = tiles[coordinate.x, coordinate.y];
                    if (neighbor != null && neighbor.TypeId == group.TypeId && unvisited.Remove(neighbor))
                    {
                        pending.Enqueue(neighbor);
                    }
                }
            }

            return group;
        }

        private static void ClassifyGroup(
            MatchGroup group,
            List<MatchRun> runs,
            List<HashSet<TileView>> squares)
        {
            bool hasFiveLine = false;
            bool hasFourHorizontal = false;
            bool hasFourVertical = false;
            HashSet<TileView> horizontalTiles = new HashSet<TileView>();
            HashSet<TileView> verticalTiles = new HashSet<TileView>();

            foreach (MatchRun run in runs)
            {
                if (run.Tiles.Count == 0 || !group.Tiles.Contains(run.Tiles[0]))
                {
                    continue;
                }

                hasFiveLine |= run.Tiles.Count >= 5;
                if (run.Horizontal)
                {
                    horizontalTiles.UnionWith(run.Tiles);
                    hasFourHorizontal |= run.Tiles.Count >= 4;
                }
                else
                {
                    verticalTiles.UnionWith(run.Tiles);
                    hasFourVertical |= run.Tiles.Count >= 4;
                }
            }

            bool hasIntersection = false;
            foreach (TileView tile in horizontalTiles)
            {
                if (verticalTiles.Contains(tile))
                {
                    hasIntersection = true;
                    break;
                }
            }

            bool hasSquare = false;
            foreach (HashSet<TileView> square in squares)
            {
                foreach (TileView tile in square)
                {
                    if (group.Tiles.Contains(tile))
                    {
                        hasSquare = true;
                        break;
                    }
                }

                if (hasSquare)
                {
                    break;
                }
            }

            if (hasFiveLine)
            {
                group.CreatedSpecialType = SpecialObjectType.ColorBomb;
            }
            else if (hasIntersection && group.Tiles.Count >= 5)
            {
                group.CreatedSpecialType = SpecialObjectType.Wrapped;
            }
            else if (hasFourHorizontal || hasFourVertical || hasSquare)
            {
                group.CreatedSpecialType = SpecialObjectType.Striped;
                group.IsSquareOnlyStriped = !hasFourHorizontal && !hasFourVertical;
                group.CascadeStripedDirection = hasFourVertical
                    ? StripedDirection.Vertical
                    : StripedDirection.Horizontal;
            }
        }

        private static bool SameType(TileView first, TileView second)
        {
            return first != null
                && second != null
                && first.CanMatchByType
                && second.CanMatchByType
                && first.TypeId == second.TypeId;
        }
    }
}
