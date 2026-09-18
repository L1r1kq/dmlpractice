using UnityEngine;

namespace TetrisGame
{
    public sealed class TetrisView : MonoBehaviour
    {
        const float CellSize = 0.92f;

        static readonly Color Background = new Color(0.07f, 0.08f, 0.12f);
        static readonly Color EmptyCell = new Color(0.12f, 0.13f, 0.20f, 1f);
        static readonly Color Frame = new Color(0.42f, 0.45f, 0.62f);
        static readonly Color Flash = Color.white;

        public static readonly Vector3 NextPieceOrigin = new Vector3(13.35f, 12.15f, 0f);

        TetrisGame game;
        Sprite cellSprite;
        SpriteRenderer[,] boardCells;
        SpriteRenderer[] pieceCells;
        SpriteRenderer[] ghostCells;
        SpriteRenderer[] nextCells;
        Transform root;

        public void Build(TetrisGame tetrisGame)
        {
            game = tetrisGame;
            cellSprite = CreateCellSprite();

            EnsureCamera();

            root = new GameObject("BoardView").transform;
            root.SetParent(transform, false);

            CreateBackdrop();
            CreateFrame();
            CreateBoardCells();
            pieceCells = CreateOverlay("ActivePiece", 4, 8);
            ghostCells = CreateOverlay("GhostPiece", 4, 7);
            nextCells = CreateOverlay("NextPiece", 4, 8);
            CreateNextPanel();
        }

        public void Render()
        {
            if (game == null)
            {
                return;
            }

            bool showActive = game.State == PlayState.Playing || game.State == PlayState.Paused;
            RenderBoard();
            RenderPiece(pieceCells, showActive ? game.Active : default(Piece), showActive, 1f);
            RenderGhost(showActive);
            RenderNext();
        }

        void RenderBoard()
        {
            bool[] flash = game.State == PlayState.Clearing ? game.FlashingRows : null;

            for (int x = 0; x < Board.Width; x++)
            {
                for (int y = 0; y < Board.VisibleHeight; y++)
                {
                    SpriteRenderer renderer = boardCells[x, y];
                    if (flash != null && y < flash.Length && flash[y])
                    {
                        renderer.color = Flash;
                        continue;
                    }

                    TetrominoType? cell = game.Board.GetCell(x, y);
                    renderer.color = cell.HasValue ? Tetromino.GetColor(cell.Value) : EmptyCell;
                }
            }
        }

        void RenderPiece(SpriteRenderer[] renderers, Piece piece, bool visible, float alpha)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                if (!visible)
                {
                    renderers[i].enabled = false;
                    continue;
                }

                Vector2Int cell = piece.CellAt(i);
                if (cell.y < 0 || cell.y >= Board.VisibleHeight || cell.x < 0 || cell.x >= Board.Width)
                {
                    renderers[i].enabled = false;
                    continue;
                }

                renderers[i].enabled = true;
                renderers[i].transform.position = CellPosition(cell.x, cell.y);
                Color color = piece.Color;
                color.a = alpha;
                renderers[i].color = color;
            }
        }

        void RenderGhost(bool showActive)
        {
            if (!showActive)
            {
                for (int i = 0; i < ghostCells.Length; i++)
                {
                    ghostCells[i].enabled = false;
                }

                return;
            }

            Piece ghost = game.Active;
            ghost.Position = new Vector2Int(ghost.Position.x, game.GhostY);
            if (ghost.Position.y == game.Active.Position.y)
            {
                for (int i = 0; i < ghostCells.Length; i++)
                {
                    ghostCells[i].enabled = false;
                }

                return;
            }

            RenderPiece(ghostCells, ghost, true, 0.28f);
        }

        void RenderNext()
        {
            Vector2Int[] cells = game.Next.Cells;
            Vector2 center = GetLocalCenter(cells);

            for (int i = 0; i < nextCells.Length; i++)
            {
                Vector2Int cell = cells[i];
                nextCells[i].enabled = true;
                nextCells[i].transform.position = NextPieceOrigin + new Vector3(cell.x - center.x, cell.y - center.y, 0f);
                nextCells[i].color = game.Next.Color;
            }
        }

        static Vector2 GetLocalCenter(Vector2Int[] cells)
        {
            float minX = cells[0].x;
            float maxX = cells[0].x;
            float minY = cells[0].y;
            float maxY = cells[0].y;
            for (int i = 1; i < cells.Length; i++)
            {
                minX = Mathf.Min(minX, cells[i].x);
                maxX = Mathf.Max(maxX, cells[i].x);
                minY = Mathf.Min(minY, cells[i].y);
                maxY = Mathf.Max(maxY, cells[i].y);
            }

            return new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
        }

        void CreateBoardCells()
        {
            Transform parent = new GameObject("Cells").transform;
            parent.SetParent(root, false);
            boardCells = new SpriteRenderer[Board.Width, Board.VisibleHeight];

            for (int x = 0; x < Board.Width; x++)
            {
                for (int y = 0; y < Board.VisibleHeight; y++)
                {
                    boardCells[x, y] = CreateSprite("Cell_" + x + "_" + y, parent, CellPosition(x, y), 1, EmptyCell, CellSize);
                }
            }
        }

        SpriteRenderer[] CreateOverlay(string name, int count, int sortingOrder)
        {
            Transform parent = new GameObject(name).transform;
            parent.SetParent(root, false);
            var renderers = new SpriteRenderer[count];
            for (int i = 0; i < count; i++)
            {
                renderers[i] = CreateSprite(name + "_" + i, parent, Vector3.zero, sortingOrder, Color.white, CellSize);
                renderers[i].enabled = false;
            }

            return renderers;
        }

        void CreateBackdrop()
        {
            CreateSprite("Backdrop", root, new Vector3(7.4f, 9.5f, 0f), -2, Background, 1f).transform.localScale = new Vector3(28f, 26f, 1f);
        }

        void CreateFrame()
        {
            const float thickness = 0.18f;
            CreateSprite("FrameLeft", root, new Vector3(-0.55f, 9.5f, 0f), 3, Frame, 1f).transform.localScale = new Vector3(thickness, 20.9f, 1f);
            CreateSprite("FrameRight", root, new Vector3(10.05f, 9.5f, 0f), 3, Frame, 1f).transform.localScale = new Vector3(thickness, 20.9f, 1f);
            CreateSprite("FrameBottom", root, new Vector3(4.75f, -0.55f, 0f), 3, Frame, 1f).transform.localScale = new Vector3(11.3f, thickness, 1f);
            CreateSprite("FrameTop", root, new Vector3(4.75f, 20.05f, 0f), 3, Frame, 1f).transform.localScale = new Vector3(11.3f, thickness, 1f);
        }

        void CreateNextPanel()
        {
            CreateSprite("NextBorder", root, NextPieceOrigin, 0, Frame, 1f).transform.localScale = new Vector3(4.9f, 4.4f, 1f);
            CreateSprite("NextPanel", root, NextPieceOrigin, 1, new Color(0.10f, 0.11f, 0.18f), 1f).transform.localScale = new Vector3(4.55f, 4.05f, 1f);
        }

        SpriteRenderer CreateSprite(string name, Transform parent, Vector3 position, int order, Color color, float size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = cellSprite;
            renderer.color = color;
            renderer.sortingOrder = order;
            go.transform.localScale = new Vector3(size, size, 1f);
            return renderer;
        }

        static Vector3 CellPosition(int x, int y)
        {
            return new Vector3(x + 0.5f, y + 0.5f, 0f);
        }

        static void EnsureCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                var go = new GameObject("Main Camera");
                camera = go.AddComponent<Camera>();
                go.tag = "MainCamera";
            }

            if (camera.GetComponent<AudioListener>() == null)
            {
                camera.gameObject.AddComponent<AudioListener>();
            }

            camera.orthographic = true;
            camera.orthographicSize = 11.2f;
            camera.transform.position = new Vector3(7.6f, 9.6f, -10f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Background;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
        }

        static Sprite CreateCellSprite()
        {
            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = x / (size - 1f);
                    float ny = y / (size - 1f);
                    float highlight = Mathf.Lerp(1.12f, 0.82f, ny) * Mathf.Lerp(1.08f, 0.9f, nx);
                    Color pixel = Color.white * highlight;
                    pixel.a = 1f;

                    if (x == 0 || y == 0 || x == size - 1 || y == size - 1)
                    {
                        pixel *= 0.55f;
                        pixel.a = 1f;
                    }

                    texture.SetPixel(x, y, pixel);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
