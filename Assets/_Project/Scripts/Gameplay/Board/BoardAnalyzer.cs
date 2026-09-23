using UnityEngine;

namespace GameMatch3.Gameplay.Board
{
    // Cac phep kiem tra board khong phu thuoc vao GameObject hay animation.
    public static class BoardAnalyzer
    {
        public static bool HasAnyMatch(TileTypeId[,] layout)
        {
            int width = layout.GetLength(0);
            int height = layout.GetLength(1);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (HasMatchAt(layout, x, y))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static int CountValidMoves(TileTypeId[,] layout, int stopAt = int.MaxValue)
        {
            int width = layout.GetLength(0);
            int height = layout.GetLength(1);
            int validMoves = 0;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (x + 1 < width && IsValidSwap(layout, x, y, x + 1, y))
                    {
                        validMoves++;
                        if (validMoves >= stopAt)
                        {
                            return validMoves;
                        }
                    }

                    if (y + 1 < height && IsValidSwap(layout, x, y, x, y + 1))
                    {
                        validMoves++;
                        if (validMoves >= stopAt)
                        {
                            return validMoves;
                        }
                    }
                }
            }

            return validMoves;
        }

        private static bool IsValidSwap(
            TileTypeId[,] layout,
            int firstX,
            int firstY,
            int secondX,
            int secondY)
        {
            if (layout[firstX, firstY] == layout[secondX, secondY])
            {
                return false;
            }

            TileTypeId firstType = layout[firstX, firstY];
            layout[firstX, firstY] = layout[secondX, secondY];
            layout[secondX, secondY] = firstType;

            bool createsMatch = HasMatchAt(layout, firstX, firstY)
                || HasMatchAt(layout, secondX, secondY);

            firstType = layout[firstX, firstY];
            layout[firstX, firstY] = layout[secondX, secondY];
            layout[secondX, secondY] = firstType;
            return createsMatch;
        }

        private static bool HasMatchAt(TileTypeId[,] layout, int x, int y)
        {
            TileTypeId typeId = layout[x, y];
            int width = layout.GetLength(0);
            int height = layout.GetLength(1);

            int horizontalCount = 1;
            for (int checkX = x - 1; checkX >= 0 && layout[checkX, y] == typeId; checkX--)
            {
                horizontalCount++;
            }

            for (int checkX = x + 1; checkX < width && layout[checkX, y] == typeId; checkX++)
            {
                horizontalCount++;
            }

            if (horizontalCount >= 3)
            {
                return true;
            }

            int verticalCount = 1;
            for (int checkY = y - 1; checkY >= 0 && layout[x, checkY] == typeId; checkY--)
            {
                verticalCount++;
            }

            for (int checkY = y + 1; checkY < height && layout[x, checkY] == typeId; checkY++)
            {
                verticalCount++;
            }

            if (verticalCount >= 3)
            {
                return true;
            }

            for (int anchorX = x - 1; anchorX <= x; anchorX++)
            {
                for (int anchorY = y - 1; anchorY <= y; anchorY++)
                {
                    if (anchorX >= 0
                        && anchorY >= 0
                        && anchorX + 1 < width
                        && anchorY + 1 < height
                        && layout[anchorX, anchorY] == typeId
                        && layout[anchorX + 1, anchorY] == typeId
                        && layout[anchorX, anchorY + 1] == typeId
                        && layout[anchorX + 1, anchorY + 1] == typeId)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
