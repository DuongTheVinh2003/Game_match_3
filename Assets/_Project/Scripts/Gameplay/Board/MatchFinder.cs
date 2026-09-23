using System.Collections.Generic;

namespace GameMatch3.Gameplay.Board
{
    // Thu thap tat ca tile thuoc chuoi tu 3 tile cung Type ID tro len.
    // Lớp này chỉ tìm match, không xóa tile hay thay đổi board.
    public static class MatchFinder
    {
        public static HashSet<TileView> FindAll(TileView[,] tiles, int width, int height)
        {
            // HashSet tránh xử lý trùng tile nằm ở giao điểm của match ngang và dọc.
            HashSet<TileView> matches = new HashSet<TileView>();

            // Quét từng hàng; runStart là vị trí bắt đầu chuỗi màu hiện tại.
            for (int y = 0; y < height; y++)
            {
                int runStart = 0;
                for (int x = 1; x <= width; x++)
                {
                    bool continuesRun = x < width
                        && tiles[x, y] != null
                        && tiles[runStart, y] != null
                        && tiles[x, y].TypeId == tiles[runStart, y].TypeId;

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

            // Quét từng cột với cùng quy tắc; ô trống luôn ngắt chuỗi.
            for (int x = 0; x < width; x++)
            {
                int runStart = 0;
                for (int y = 1; y <= height; y++)
                {
                    bool continuesRun = y < height
                        && tiles[x, y] != null
                        && tiles[x, runStart] != null
                        && tiles[x, y].TypeId == tiles[x, runStart].TypeId;

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
