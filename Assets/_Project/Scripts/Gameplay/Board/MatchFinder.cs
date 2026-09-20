using System.Collections.Generic;

namespace GameMatch3.Gameplay.Board
{
    public static class MatchFinder
    {
        public static HashSet<TileView> FindAll(TileView[,] tiles, int width, int height)
        {
            HashSet<TileView> matches = new HashSet<TileView>();

            for (int y = 0; y < height; y++)
            {
                int runStart = 0;
                for (int x = 1; x <= width; x++)
                {
                    bool continuesRun = x < width
                        && tiles[x, y] != null
                        && tiles[runStart, y] != null
                        && tiles[x, y].ColorType == tiles[runStart, y].ColorType;

                    if (continuesRun)
                    {
                        continue;
                    }

                    if (tiles[runStart, y] != null && x - runStart >= 3)
                    {
                        for (int matchX = runStart; matchX < x; matchX++)
                        {
                            matches.Add(tiles[matchX, y]);
                        }
                    }

                    runStart = x;
                }
            }

            for (int x = 0; x < width; x++)
            {
                int runStart = 0;
                for (int y = 1; y <= height; y++)
                {
                    bool continuesRun = y < height
                        && tiles[x, y] != null
                        && tiles[x, runStart] != null
                        && tiles[x, y].ColorType == tiles[x, runStart].ColorType;

                    if (continuesRun)
                    {
                        continue;
                    }

                    if (tiles[x, runStart] != null && y - runStart >= 3)
                    {
                        for (int matchY = runStart; matchY < y; matchY++)
                        {
                            matches.Add(tiles[x, matchY]);
                        }
                    }

                    runStart = y;
                }
            }

            return matches;
        }
    }
}
