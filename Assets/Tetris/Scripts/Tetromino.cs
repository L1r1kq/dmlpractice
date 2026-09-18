using UnityEngine;

namespace TetrisGame
{
    public enum TetrominoType
    {
        I,
        O,
        T,
        S,
        Z,
        J,
        L
    }

    public static class Tetromino
    {
        public const int CellCount = 4;
        public const int RotationCount = 4;

        static readonly Vector2Int[][][] Shapes =
        {
            // I
            new[]
            {
                Cells(0, 1, 1, 1, 2, 1, 3, 1),
                Cells(2, 0, 2, 1, 2, 2, 2, 3),
                Cells(0, 2, 1, 2, 2, 2, 3, 2),
                Cells(1, 0, 1, 1, 1, 2, 1, 3)
            },
            // O
            new[]
            {
                Cells(1, 0, 2, 0, 1, 1, 2, 1),
                Cells(1, 0, 2, 0, 1, 1, 2, 1),
                Cells(1, 0, 2, 0, 1, 1, 2, 1),
                Cells(1, 0, 2, 0, 1, 1, 2, 1)
            },
            // T
            new[]
            {
                Cells(1, 0, 0, 1, 1, 1, 2, 1),
                Cells(1, 0, 1, 1, 2, 1, 1, 2),
                Cells(0, 1, 1, 1, 2, 1, 1, 2),
                Cells(1, 0, 0, 1, 1, 1, 1, 2)
            },
            // S
            new[]
            {
                Cells(1, 0, 2, 0, 0, 1, 1, 1),
                Cells(1, 0, 1, 1, 2, 1, 2, 2),
                Cells(1, 1, 2, 1, 0, 2, 1, 2),
                Cells(0, 0, 0, 1, 1, 1, 1, 2)
            },
            // Z
            new[]
            {
                Cells(0, 0, 1, 0, 1, 1, 2, 1),
                Cells(2, 0, 1, 1, 2, 1, 1, 2),
                Cells(0, 1, 1, 1, 1, 2, 2, 2),
                Cells(1, 0, 0, 1, 1, 1, 0, 2)
            },
            // J
            new[]
            {
                Cells(0, 0, 0, 1, 1, 1, 2, 1),
                Cells(1, 0, 2, 0, 1, 1, 1, 2),
                Cells(0, 1, 1, 1, 2, 1, 2, 2),
                Cells(1, 0, 1, 1, 0, 2, 1, 2)
            },
            // L
            new[]
            {
                Cells(2, 0, 0, 1, 1, 1, 2, 1),
                Cells(1, 0, 1, 1, 1, 2, 2, 2),
                Cells(0, 1, 1, 1, 2, 1, 0, 2),
                Cells(0, 0, 1, 0, 1, 1, 1, 2)
            }
        };

        static readonly Color[] Colors =
        {
            new Color(0.20f, 0.85f, 0.95f),
            new Color(0.98f, 0.86f, 0.22f),
            new Color(0.73f, 0.33f, 0.96f),
            new Color(0.32f, 0.86f, 0.38f),
            new Color(0.94f, 0.28f, 0.30f),
            new Color(0.27f, 0.45f, 0.96f),
            new Color(0.98f, 0.58f, 0.18f)
        };

        static readonly Vector2Int[] KickOffsets =
        {
            Vector2Int.zero,
            Vector2Int.left,
            Vector2Int.right,
            Vector2Int.up,
            new Vector2Int(-1, 1),
            new Vector2Int(1, 1),
            new Vector2Int(-2, 0),
            new Vector2Int(2, 0),
            Vector2Int.down
        };

        public static Vector2Int[] GetCells(TetrominoType type, int rotation)
        {
            return Shapes[(int)type][NormalizeRotation(rotation)];
        }

        public static Color GetColor(TetrominoType type)
        {
            return Colors[(int)type];
        }

        public static Vector2Int[] GetKicks()
        {
            return KickOffsets;
        }

        public static int NormalizeRotation(int rotation)
        {
            return ((rotation % RotationCount) + RotationCount) % RotationCount;
        }

        static Vector2Int[] Cells(int x0, int y0, int x1, int y1, int x2, int y2, int x3, int y3)
        {
            return new[]
            {
                new Vector2Int(x0, y0),
                new Vector2Int(x1, y1),
                new Vector2Int(x2, y2),
                new Vector2Int(x3, y3)
            };
        }
    }

    public struct Piece
    {
        public TetrominoType Type;
        public Vector2Int Position;
        public int Rotation;

        public Vector2Int[] Cells
        {
            get { return Tetromino.GetCells(Type, Rotation); }
        }

        public Color Color
        {
            get { return Tetromino.GetColor(Type); }
        }

        public Vector2Int CellAt(int index)
        {
            return Position + Cells[index];
        }
    }
}
