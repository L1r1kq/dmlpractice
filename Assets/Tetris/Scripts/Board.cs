using UnityEngine;

namespace TetrisGame
{
    public sealed class Board
    {
        public const int Width = 10;
        public const int Height = 22;
        public const int VisibleHeight = 20;

        readonly TetrominoType?[,] cells = new TetrominoType?[Width, Height];

        public void Clear()
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    cells[x, y] = null;
                }
            }
        }

        public TetrominoType? GetCell(int x, int y)
        {
            if (!IsInside(x, y))
            {
                return null;
            }

            return cells[x, y];
        }

        public bool IsInside(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }

        public bool Fits(Piece piece)
        {
            for (int i = 0; i < Tetromino.CellCount; i++)
            {
                Vector2Int cell = piece.CellAt(i);
                if (!IsInside(cell.x, cell.y) || cells[cell.x, cell.y].HasValue)
                {
                    return false;
                }
            }

            return true;
        }

        public void Lock(Piece piece)
        {
            for (int i = 0; i < Tetromino.CellCount; i++)
            {
                Vector2Int cell = piece.CellAt(i);
                if (IsInside(cell.x, cell.y))
                {
                    cells[cell.x, cell.y] = piece.Type;
                }
            }
        }

        public int ClearFullLines(out bool[] clearedRows)
        {
            clearedRows = new bool[Height];
            int cleared = 0;
            int writeY = 0;

            TetrominoType?[,] compact = new TetrominoType?[Width, Height];

            for (int y = 0; y < Height; y++)
            {
                if (IsRowFull(y))
                {
                    clearedRows[y] = true;
                    cleared++;
                    continue;
                }

                for (int x = 0; x < Width; x++)
                {
                    compact[x, writeY] = cells[x, y];
                }

                writeY++;
            }

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    cells[x, y] = compact[x, y];
                }
            }

            return cleared;
        }

        public bool OccupiesVisibleTop(Piece piece)
        {
            bool anyVisible = false;
            for (int i = 0; i < Tetromino.CellCount; i++)
            {
                if (piece.CellAt(i).y < VisibleHeight)
                {
                    anyVisible = true;
                    break;
                }
            }

            return !anyVisible;
        }

        bool IsRowFull(int y)
        {
            for (int x = 0; x < Width; x++)
            {
                if (!cells[x, y].HasValue)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
