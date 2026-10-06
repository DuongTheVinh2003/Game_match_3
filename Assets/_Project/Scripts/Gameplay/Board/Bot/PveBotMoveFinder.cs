using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameMatch3.Gameplay.Board.Bot
{
    // Bộ phân tích không thay đổi GameObject và không nhìn trước tile refill ngẫu nhiên.
    public static class PveBotMoveFinder
    {
        private struct SimTile
        {
            public TileTypeId TypeId;
            public bool CanMatch;

            public SimTile(TileTypeId typeId, bool canMatch)
            {
                TypeId = typeId;
                CanMatch = canMatch;
            }
        }

        private sealed class SimGroup
        {
            public readonly List<Vector2Int> Cells = new List<Vector2Int>();
            public SpecialObjectType CreatedSpecial;
        }

        public static bool TryChooseMove(
            TileView[,] tiles,
            PveBotDifficulty difficulty,
            PveMoveTieBreak tieBreak,
            PveRefillDirection refillDirection,
            LevelSettings scoring,
            System.Random random,
            out PveBotMove selected)
        {
            List<PveBotMove> moves = FindMoves(
                tiles,
                refillDirection,
                scoring,
                difficulty == PveBotDifficulty.FirstValidMove);
            if (moves.Count == 0)
            {
                selected = default;
                return false;
            }

            if (difficulty == PveBotDifficulty.FirstValidMove)
            {
                selected = moves[0];
                return true;
            }

            bool requireCascade = difficulty == PveBotDifficulty.CascadePriority
                && moves.Exists(move => move.HasCascade);
            int bestScore = int.MinValue;
            List<PveBotMove> best = new List<PveBotMove>();
            foreach (PveBotMove move in moves)
            {
                if (requireCascade && !move.HasCascade)
                {
                    continue;
                }

                int score = difficulty == PveBotDifficulty.CascadePriority
                    ? move.TotalScore
                    : move.ImmediateScore;
                if (score > bestScore)
                {
                    bestScore = score;
                    best.Clear();
                    best.Add(move);
                }
                else if (score == bestScore)
                {
                    best.Add(move);
                }
            }

            selected = tieBreak == PveMoveTieBreak.RandomAmongBest && best.Count > 1
                ? best[random.Next(best.Count)]
                : best[0];
            return true;
        }

        private static List<PveBotMove> FindMoves(
            TileView[,] tiles,
            PveRefillDirection refillDirection,
            LevelSettings scoring,
            bool stopAfterFirst)
        {
            int width = tiles.GetLength(0);
            int height = tiles.GetLength(1);
            SimTile?[,] layout = CreateLayout(tiles);
            List<PveBotMove> result = new List<PveBotMove>();

            // y=0 là hàng dưới. Chỉ thử phải và lên để mỗi cặp kề được xét đúng một lần.
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector2Int first = new Vector2Int(x, y);
                    if (x + 1 < width)
                    {
                        TryAddMove(layout, first, new Vector2Int(x + 1, y), refillDirection, scoring, result);
                        if (stopAfterFirst && result.Count > 0) return result;
                    }

                    if (y + 1 < height)
                    {
                        TryAddMove(layout, first, new Vector2Int(x, y + 1), refillDirection, scoring, result);
                        if (stopAfterFirst && result.Count > 0) return result;
                    }
                }
            }

            return result;
        }

        private static void TryAddMove(
            SimTile?[,] source,
            Vector2Int first,
            Vector2Int second,
            PveRefillDirection refillDirection,
            LevelSettings scoring,
            List<PveBotMove> result)
        {
            SimTile? firstTile = source[first.x, first.y];
            SimTile? secondTile = source[second.x, second.y];
            if (!firstTile.HasValue
                || !secondTile.HasValue
                || !firstTile.Value.CanMatch
                || !secondTile.Value.CanMatch
                || firstTile.Value.TypeId == secondTile.Value.TypeId)
            {
                return;
            }

            SimTile?[,] layout = Clone(source);
            Swap(layout, first, second);
            List<SimGroup> groups = FindGroups(layout);
            if (groups.Count == 0)
            {
                return;
            }

            int immediateScore = ResolveGroups(layout, groups, second, 1, scoring);
            Collapse(layout, refillDirection);
            bool hasCascade = false;
            int totalScore = immediateScore;
            int resolutionIndex = 1;
            while (true)
            {
                groups = FindGroups(layout);
                if (groups.Count == 0)
                {
                    break;
                }

                hasCascade = true;
                int multiplier = Mathf.Min(resolutionIndex + 1, scoring.MaximumCascadeMultiplier);
                totalScore += ResolveGroups(layout, groups, null, multiplier, scoring);
                Collapse(layout, refillDirection);
                resolutionIndex++;
            }

            result.Add(new PveBotMove(first, second, immediateScore, totalScore, hasCascade));
        }

        private static int ResolveGroups(
            SimTile?[,] layout,
            List<SimGroup> groups,
            Vector2Int? preferredHost,
            int multiplier,
            LevelSettings scoring)
        {
            HashSet<Vector2Int> protectedHosts = new HashSet<Vector2Int>();
            long score = 0;
            foreach (SimGroup group in groups)
            {
                if (group.CreatedSpecial == SpecialObjectType.None)
                {
                    continue;
                }

                Vector2Int host = ChooseHost(group.Cells, preferredHost);
                protectedHosts.Add(host);
                if (group.CreatedSpecial == SpecialObjectType.ColorBomb
                    && layout[host.x, host.y].HasValue)
                {
                    SimTile colorBomb = layout[host.x, host.y].Value;
                    colorBomb.CanMatch = false;
                    layout[host.x, host.y] = colorBomb;
                }

                score += scoring.GetCreationBonus(group.CreatedSpecial);
            }

            int cleared = 0;
            foreach (SimGroup group in groups)
            {
                foreach (Vector2Int cell in group.Cells)
                {
                    if (protectedHosts.Contains(cell))
                    {
                        continue;
                    }

                    if (layout[cell.x, cell.y].HasValue)
                    {
                        layout[cell.x, cell.y] = null;
                        cleared++;
                    }
                }
            }

            score += (long)cleared * scoring.PointsPerClearedTile * multiplier;
            return score > int.MaxValue ? int.MaxValue : (int)score;
        }

        private static Vector2Int ChooseHost(List<Vector2Int> cells, Vector2Int? preferred)
        {
            if (preferred.HasValue && cells.Contains(preferred.Value))
            {
                return preferred.Value;
            }

            Vector2 center = Vector2.zero;
            foreach (Vector2Int cell in cells)
            {
                center += cell;
            }

            center /= cells.Count;
            Vector2Int selected = cells[0];
            float selectedDistance = ((Vector2)selected - center).sqrMagnitude;
            foreach (Vector2Int cell in cells)
            {
                float distance = ((Vector2)cell - center).sqrMagnitude;
                if (cell.y < selected.y || (cell.y == selected.y && distance < selectedDistance))
                {
                    selected = cell;
                    selectedDistance = distance;
                }
            }

            return selected;
        }

        private static List<SimGroup> FindGroups(SimTile?[,] layout)
        {
            int width = layout.GetLength(0);
            int height = layout.GetLength(1);
            bool[,] matched = new bool[width, height];
            bool[,] horizontal = new bool[width, height];
            bool[,] vertical = new bool[width, height];
            bool[,] square = new bool[width, height];

            for (int y = 0; y < height; y++)
            {
                int start = 0;
                while (start < width)
                {
                    int end = start + 1;
                    while (end < width && SameType(layout[start, y], layout[end, y])) end++;
                    if (IsMatchable(layout[start, y]) && end - start >= 3)
                    {
                        for (int x = start; x < end; x++)
                        {
                            matched[x, y] = true;
                            horizontal[x, y] = true;
                        }
                    }

                    start = end;
                }
            }

            for (int x = 0; x < width; x++)
            {
                int start = 0;
                while (start < height)
                {
                    int end = start + 1;
                    while (end < height && SameType(layout[x, start], layout[x, end])) end++;
                    if (IsMatchable(layout[x, start]) && end - start >= 3)
                    {
                        for (int y = start; y < end; y++)
                        {
                            matched[x, y] = true;
                            vertical[x, y] = true;
                        }
                    }

                    start = end;
                }
            }

            for (int x = 0; x < width - 1; x++)
            {
                for (int y = 0; y < height - 1; y++)
                {
                    if (IsMatchable(layout[x, y])
                        && SameType(layout[x, y], layout[x + 1, y])
                        && SameType(layout[x, y], layout[x, y + 1])
                        && SameType(layout[x, y], layout[x + 1, y + 1]))
                    {
                        for (int dx = 0; dx <= 1; dx++)
                        {
                            for (int dy = 0; dy <= 1; dy++)
                            {
                                matched[x + dx, y + dy] = true;
                                square[x + dx, y + dy] = true;
                            }
                        }
                    }
                }
            }

            List<SimGroup> groups = new List<SimGroup>();
            bool[,] visited = new bool[width, height];
            Vector2Int[] directions = { Vector2Int.left, Vector2Int.right, Vector2Int.down, Vector2Int.up };
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (!matched[x, y] || visited[x, y]) continue;
                    SimGroup group = new SimGroup();
                    Queue<Vector2Int> queue = new Queue<Vector2Int>();
                    queue.Enqueue(new Vector2Int(x, y));
                    visited[x, y] = true;
                    while (queue.Count > 0)
                    {
                        Vector2Int cell = queue.Dequeue();
                        group.Cells.Add(cell);
                        foreach (Vector2Int direction in directions)
                        {
                            Vector2Int next = cell + direction;
                            if (next.x < 0 || next.x >= width || next.y < 0 || next.y >= height
                                || visited[next.x, next.y] || !matched[next.x, next.y]
                                || !SameType(layout[cell.x, cell.y], layout[next.x, next.y]))
                            {
                                continue;
                            }

                            visited[next.x, next.y] = true;
                            queue.Enqueue(next);
                        }
                    }

                    group.CreatedSpecial = Classify(group.Cells, horizontal, vertical, square);
                    groups.Add(group);
                }
            }

            return groups;
        }

        private static SpecialObjectType Classify(
            List<Vector2Int> cells,
            bool[,] horizontal,
            bool[,] vertical,
            bool[,] square)
        {
            HashSet<Vector2Int> set = new HashSet<Vector2Int>(cells);
            foreach (Vector2Int cell in cells)
            {
                int horizontalLength = CountRun(set, cell, Vector2Int.left, Vector2Int.right);
                int verticalLength = CountRun(set, cell, Vector2Int.down, Vector2Int.up);
                if (horizontalLength >= 5 || verticalLength >= 5)
                {
                    return SpecialObjectType.ColorBomb;
                }
            }

            foreach (Vector2Int cell in cells)
            {
                if (horizontal[cell.x, cell.y] && vertical[cell.x, cell.y] && cells.Count >= 5)
                {
                    return SpecialObjectType.Wrapped;
                }
            }

            foreach (Vector2Int cell in cells)
            {
                if (square[cell.x, cell.y]) return SpecialObjectType.Striped;
                int horizontalLength = CountRun(set, cell, Vector2Int.left, Vector2Int.right);
                int verticalLength = CountRun(set, cell, Vector2Int.down, Vector2Int.up);
                if (horizontalLength >= 4 || verticalLength >= 4) return SpecialObjectType.Striped;
            }

            return SpecialObjectType.None;
        }

        private static int CountRun(
            HashSet<Vector2Int> cells,
            Vector2Int origin,
            Vector2Int negativeDirection,
            Vector2Int positiveDirection)
        {
            int length = 1;
            Vector2Int current = origin + negativeDirection;
            while (cells.Contains(current))
            {
                length++;
                current += negativeDirection;
            }

            current = origin + positiveDirection;
            while (cells.Contains(current))
            {
                length++;
                current += positiveDirection;
            }

            return length;
        }

        private static void Collapse(SimTile?[,] layout, PveRefillDirection refillDirection)
        {
            int width = layout.GetLength(0);
            int height = layout.GetLength(1);
            bool upward = refillDirection == PveRefillDirection.FromBottom;
            for (int x = 0; x < width; x++)
            {
                int destinationY = upward ? height - 1 : 0;
                int sourceY = upward ? height - 1 : 0;
                while (sourceY >= 0 && sourceY < height)
                {
                    if (layout[x, sourceY].HasValue)
                    {
                        layout[x, destinationY] = layout[x, sourceY];
                        if (sourceY != destinationY) layout[x, sourceY] = null;
                        destinationY += upward ? -1 : 1;
                    }

                    sourceY += upward ? -1 : 1;
                }

                if (upward)
                {
                    for (int y = destinationY; y >= 0; y--) layout[x, y] = null;
                }
                else
                {
                    for (int y = destinationY; y < height; y++) layout[x, y] = null;
                }
            }
        }

        private static SimTile?[,] CreateLayout(TileView[,] tiles)
        {
            int width = tiles.GetLength(0);
            int height = tiles.GetLength(1);
            SimTile?[,] result = new SimTile?[width, height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    TileView tile = tiles[x, y];
                    if (tile != null) result[x, y] = new SimTile(tile.TypeId, tile.CanMatchByType);
                }
            }

            return result;
        }

        private static SimTile?[,] Clone(SimTile?[,] source)
        {
            return (SimTile?[,])source.Clone();
        }

        private static void Swap(SimTile?[,] layout, Vector2Int first, Vector2Int second)
        {
            SimTile? value = layout[first.x, first.y];
            layout[first.x, first.y] = layout[second.x, second.y];
            layout[second.x, second.y] = value;
        }

        private static bool IsMatchable(SimTile? tile)
        {
            return tile.HasValue && tile.Value.CanMatch;
        }

        private static bool SameType(SimTile? first, SimTile? second)
        {
            return IsMatchable(first)
                && IsMatchable(second)
                && first.Value.TypeId == second.Value.TypeId;
        }
    }
}
