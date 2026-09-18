using System.Collections.Generic;
using UnityEngine;

namespace TetrisGame
{
    public enum PlayState
    {
        Playing,
        Clearing,
        Paused,
        GameOver
    }

    public sealed class TetrisGame : MonoBehaviour
    {
        public const int LinesPerLevel = 10;

        static readonly int[] GravityFrames =
        {
            48, 43, 38, 33, 28, 23, 18, 13, 8, 6, 5, 5, 4, 4, 3, 3, 2, 2, 1
        };

        readonly Board board = new Board();
        readonly Queue<TetrominoType> bag = new Queue<TetrominoType>();
        readonly List<TetrominoType> bagSource = new List<TetrominoType>(7);

        TetrisView view;
        TetrisUI ui;
        TetrisAudio audioPlayer;

        Piece active;
        Piece next;
        PlayState state = PlayState.Playing;
        PlayState stateBeforePause = PlayState.Playing;

        int score;
        int level = 1;
        int lines;

        float gravityTimer;
        float dasTimer;
        int dasDirection;
        float clearTimer;
        bool[] flashingRows;

        const float DasDelay = 0.17f;
        const float ArrInterval = 0.045f;
        const float SoftDropInterval = 0.04f;
        const float ClearFlashDuration = 0.22f;

        public Board Board { get { return board; } }
        public Piece Active { get { return active; } }
        public Piece Next { get { return next; } }
        public PlayState State { get { return state; } }
        public int Score { get { return score; } }
        public int Level { get { return level; } }
        public int Lines { get { return lines; } }
        public bool[] FlashingRows { get { return flashingRows; } }

        public int GhostY
        {
            get
            {
                Piece ghost = active;
                while (true)
                {
                    ghost.Position += Vector2Int.down;
                    if (!board.Fits(ghost))
                    {
                        return ghost.Position.y + 1;
                    }
                }
            }
        }

        void Awake()
        {
            Application.targetFrameRate = 60;

            view = gameObject.AddComponent<TetrisView>();
            ui = gameObject.AddComponent<TetrisUI>();
            audioPlayer = gameObject.AddComponent<TetrisAudio>();

            view.Build(this);
            ui.Build(this);
        }

        void Start()
        {
            Restart();
        }

        void Update()
        {
            HandleGlobalInput();

            if (state == PlayState.Paused || state == PlayState.GameOver)
            {
                RefreshPresentation();
                return;
            }

            if (state == PlayState.Clearing)
            {
                clearTimer -= Time.deltaTime;
                if (clearTimer <= 0f)
                {
                    FinishLineClear();
                }

                RefreshPresentation();
                return;
            }

            HandlePlayInput();
            StepGravity();
            RefreshPresentation();
        }

        public void Restart()
        {
            board.Clear();
            bag.Clear();
            score = 0;
            level = 1;
            lines = 0;
            gravityTimer = 0f;
            dasTimer = 0f;
            dasDirection = 0;
            flashingRows = null;
            state = PlayState.Playing;
            next = CreatePiece(DrawFromBag());
            SpawnNext();
            RefreshPresentation();
        }

        public void TogglePause()
        {
            if (state == PlayState.GameOver)
            {
                return;
            }

            if (state == PlayState.Paused)
            {
                state = stateBeforePause;
            }
            else
            {
                stateBeforePause = state;
                state = PlayState.Paused;
            }
        }

        void HandleGlobalInput()
        {
            if (state == PlayState.GameOver)
            {
                if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    Restart();
                }

                return;
            }

            if (Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.Escape))
            {
                TogglePause();
            }

            if (Input.GetKeyDown(KeyCode.R) && state == PlayState.Paused)
            {
                Restart();
            }
        }

        void HandlePlayInput()
        {
            int move = 0;
            if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
            {
                move -= 1;
            }

            if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
            {
                move += 1;
            }

            if (move != 0)
            {
                if (move != dasDirection)
                {
                    dasDirection = move;
                    dasTimer = 0f;
                    TryMove(move, 0, true);
                }
                else
                {
                    dasTimer += Time.deltaTime;
                    if (dasTimer >= DasDelay)
                    {
                        float extra = dasTimer - DasDelay;
                        if (extra == 0f || Mathf.FloorToInt(extra / ArrInterval) != Mathf.FloorToInt((extra - Time.deltaTime) / ArrInterval))
                        {
                            TryMove(move, 0, true);
                        }
                    }
                }
            }
            else
            {
                dasDirection = 0;
                dasTimer = 0f;
            }

            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.W))
            {
                TryRotate(1);
            }

            if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.LeftControl))
            {
                TryRotate(-1);
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                HardDrop();
            }
        }

        void StepGravity()
        {
            float interval = Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S)
                ? SoftDropInterval
                : GravityInterval();

            gravityTimer += Time.deltaTime;
            if (gravityTimer < interval)
            {
                return;
            }

            gravityTimer = 0f;
            bool soft = Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S);
            if (!TryMove(0, -1, false))
            {
                LockActive();
            }
            else if (soft)
            {
                score += 1;
            }
        }

        float GravityInterval()
        {
            int index = Mathf.Clamp(level - 1, 0, GravityFrames.Length - 1);
            return GravityFrames[index] / 60f;
        }

        bool TryMove(int dx, int dy, bool playSound)
        {
            Piece moved = active;
            moved.Position += new Vector2Int(dx, dy);
            if (!board.Fits(moved))
            {
                return false;
            }

            active = moved;
            if (playSound)
            {
                audioPlayer.PlayMove();
            }

            return true;
        }

        void TryRotate(int direction)
        {
            Vector2Int[] kicks = Tetromino.GetKicks();
            for (int i = 0; i < kicks.Length; i++)
            {
                Piece rotated = active;
                rotated.Rotation = Tetromino.NormalizeRotation(active.Rotation + direction);
                rotated.Position = active.Position + kicks[i];
                if (board.Fits(rotated))
                {
                    active = rotated;
                    audioPlayer.PlayRotate();
                    return;
                }
            }
        }

        void HardDrop()
        {
            int dropped = 0;
            while (TryMove(0, -1, false))
            {
                dropped++;
            }

            score += dropped * 2;
            audioPlayer.PlayDrop();
            LockActive();
        }

        void LockActive()
        {
            board.Lock(active);

            if (board.OccupiesVisibleTop(active))
            {
                EndGame();
                return;
            }

            int cleared = CountFullLines(out flashingRows);
            if (cleared > 0)
            {
                state = PlayState.Clearing;
                clearTimer = ClearFlashDuration;
                audioPlayer.PlayClear(cleared);
                return;
            }

            if (!SpawnNext())
            {
                EndGame();
            }
        }

        void FinishLineClear()
        {
            int cleared = board.ClearFullLines(out flashingRows);
            flashingRows = null;
            lines += cleared;
            score += LineScore(cleared) * level;
            level = 1 + lines / LinesPerLevel;
            state = PlayState.Playing;
            gravityTimer = 0f;

            if (!SpawnNext())
            {
                EndGame();
            }
        }

        int CountFullLines(out bool[] rows)
        {
            rows = new bool[Board.Height];
            int count = 0;
            for (int y = 0; y < Board.Height; y++)
            {
                bool full = true;
                for (int x = 0; x < Board.Width; x++)
                {
                    if (!board.GetCell(x, y).HasValue)
                    {
                        full = false;
                        break;
                    }
                }

                if (full)
                {
                    rows[y] = true;
                    count++;
                }
            }

            return count;
        }

        static int LineScore(int cleared)
        {
            switch (cleared)
            {
                case 1: return 100;
                case 2: return 300;
                case 3: return 500;
                case 4: return 800;
                default: return 0;
            }
        }

        bool SpawnNext()
        {
            active = next;
            active.Position = SpawnPosition();
            next = CreatePiece(DrawFromBag());
            gravityTimer = 0f;
            return board.Fits(active);
        }

        void EndGame()
        {
            state = PlayState.GameOver;
            audioPlayer.PlayGameOver();
        }

        Piece CreatePiece(TetrominoType type)
        {
            return new Piece
            {
                Type = type,
                Position = SpawnPosition(),
                Rotation = 0
            };
        }

        static Vector2Int SpawnPosition()
        {
            return new Vector2Int(3, 18);
        }

        TetrominoType DrawFromBag()
        {
            if (bag.Count == 0)
            {
                RefillBag();
            }

            return bag.Dequeue();
        }

        void RefillBag()
        {
            bagSource.Clear();
            bagSource.Add(TetrominoType.I);
            bagSource.Add(TetrominoType.O);
            bagSource.Add(TetrominoType.T);
            bagSource.Add(TetrominoType.S);
            bagSource.Add(TetrominoType.Z);
            bagSource.Add(TetrominoType.J);
            bagSource.Add(TetrominoType.L);

            for (int i = bagSource.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                TetrominoType tmp = bagSource[i];
                bagSource[i] = bagSource[j];
                bagSource[j] = tmp;
            }

            for (int i = 0; i < bagSource.Count; i++)
            {
                bag.Enqueue(bagSource[i]);
            }
        }

        void RefreshPresentation()
        {
            view.Render();
            ui.Render();
        }
    }
}
