namespace GameMatch3.Gameplay.Board
{
    public static class MoveFinder
    {
        public static int CountValidMoves(
            TileView[,] tiles,
            int width,
            int height,
            int stopAt = int.MaxValue)
        {
            int validMoves = 0;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (x + 1 < width && IsValidSwap(tiles, x, y, x + 1, y))
                    {
                        validMoves++;
                        if (validMoves >= stopAt)
                        {
                            return validMoves;
                        }
                    }

                    if (y + 1 < height && IsValidSwap(tiles, x, y, x, y + 1))
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

        public static bool IsColorBombNormalSwap(TileView first, TileView second)
        {
            return first != null
                && second != null
                && ((first.SpecialType == SpecialObjectType.ColorBomb && second.SpecialType == SpecialObjectType.None)
                    || (second.SpecialType == SpecialObjectType.ColorBomb && first.SpecialType == SpecialObjectType.None));
        }

        private static bool IsValidSwap(
            TileView[,] tiles,
            int firstX,
            int firstY,
            int secondX,
            int secondY)
        {
            TileView first = tiles[firstX, firstY];
            TileView second = tiles[secondX, secondY];
            if (first == null || second == null)
            {
                return false;
            }

            if (IsColorBombNormalSwap(first, second))
            {
                return true;
            }

            if (!first.CanMatchByType
                || !second.CanMatchByType
                || first.TypeId == second.TypeId)
            {
                return false;
            }

            tiles[firstX, firstY] = second;
            tiles[secondX, secondY] = first;
            bool createsMatch = MatchFinder.HasMatchAt(tiles, firstX, firstY)
                || MatchFinder.HasMatchAt(tiles, secondX, secondY);
            tiles[firstX, firstY] = first;
            tiles[secondX, secondY] = second;
            return createsMatch;
        }
    }
}
