using System.Diagnostics;
using System.Text;

namespace Tetris;

internal static class Program
{
    static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.CursorVisible = false;
        Console.Title = "Тетрис";
        Console.Clear();

        var game = new Game();
        game.Restart();

        var clock = Stopwatch.StartNew();
        long lastMs = 0;

        try
        {
            while (true)
            {
                while (Console.KeyAvailable)
                {
                    game.HandleKey(Console.ReadKey(true));
                }

                long now = clock.ElapsedMilliseconds;
                game.Update(now - lastMs);
                lastMs = now;
                game.Draw();
                Thread.Sleep(16);
            }
        }
        finally
        {
            Console.CursorVisible = true;
            Console.ResetColor();
        }
    }
}

internal sealed class Game
{
    const int Cols = 10;
    const int Rows = 20;

    static readonly string[] Types = ["I", "O", "T", "S", "Z", "J", "L"];
    static readonly (int X, int Y)[] Kicks =
    [
        (0, 0), (-1, 0), (1, 0), (0, 1), (-1, 1), (1, 1), (-2, 0), (2, 0), (0, -1)
    ];
    static readonly int[] Gravity = [48, 43, 38, 33, 28, 23, 18, 13, 8, 6, 5, 5, 4, 4, 3, 3, 2, 2, 1];
    static readonly int[] LineScore = [0, 100, 300, 500, 800];

    static readonly Dictionary<string, ConsoleColor> Colors = new()
    {
        ["I"] = ConsoleColor.Cyan,
        ["O"] = ConsoleColor.Yellow,
        ["T"] = ConsoleColor.Magenta,
        ["S"] = ConsoleColor.Green,
        ["Z"] = ConsoleColor.Red,
        ["J"] = ConsoleColor.Blue,
        ["L"] = ConsoleColor.DarkYellow,
    };

    static readonly Dictionary<string, (int X, int Y)[][]> Shapes = new()
    {
        ["I"] =
        [
            [(0, 1), (1, 1), (2, 1), (3, 1)],
            [(2, 0), (2, 1), (2, 2), (2, 3)],
            [(0, 2), (1, 2), (2, 2), (3, 2)],
            [(1, 0), (1, 1), (1, 2), (1, 3)],
        ],
        ["O"] =
        [
            [(1, 0), (2, 0), (1, 1), (2, 1)],
            [(1, 0), (2, 0), (1, 1), (2, 1)],
            [(1, 0), (2, 0), (1, 1), (2, 1)],
            [(1, 0), (2, 0), (1, 1), (2, 1)],
        ],
        ["T"] =
        [
            [(1, 0), (0, 1), (1, 1), (2, 1)],
            [(1, 0), (1, 1), (2, 1), (1, 2)],
            [(0, 1), (1, 1), (2, 1), (1, 2)],
            [(1, 0), (0, 1), (1, 1), (1, 2)],
        ],
        ["S"] =
        [
            [(1, 0), (2, 0), (0, 1), (1, 1)],
            [(1, 0), (1, 1), (2, 1), (2, 2)],
            [(1, 1), (2, 1), (0, 2), (1, 2)],
            [(0, 0), (0, 1), (1, 1), (1, 2)],
        ],
        ["Z"] =
        [
            [(0, 0), (1, 0), (1, 1), (2, 1)],
            [(2, 0), (1, 1), (2, 1), (1, 2)],
            [(0, 1), (1, 1), (1, 2), (2, 2)],
            [(1, 0), (0, 1), (1, 1), (0, 2)],
        ],
        ["J"] =
        [
            [(0, 0), (0, 1), (1, 1), (2, 1)],
            [(1, 0), (2, 0), (1, 1), (1, 2)],
            [(0, 1), (1, 1), (2, 1), (2, 2)],
            [(1, 0), (1, 1), (0, 2), (1, 2)],
        ],
        ["L"] =
        [
            [(2, 0), (0, 1), (1, 1), (2, 1)],
            [(1, 0), (1, 1), (1, 2), (2, 2)],
            [(0, 1), (1, 1), (2, 1), (0, 2)],
            [(0, 0), (1, 0), (1, 1), (1, 2)],
        ],
    };

    readonly string?[,] board = new string?[Cols, Rows];
    readonly Queue<string> bag = new();
    readonly Random random = new();

    Piece active;
    Piece next;
    string status = "playing";
    int score;
    int level = 1;
    int lines;
    double gravityMs;
    HashSet<int> flashRows = [];
    double flashLeft;

    public void Restart()
    {
        Array.Clear(board);
        bag.Clear();
        score = 0;
        level = 1;
        lines = 0;
        status = "playing";
        gravityMs = 0;
        flashRows.Clear();
        next = TakePiece();
        Spawn();
    }

    public void HandleKey(ConsoleKeyInfo key)
    {
        if (status == "over")
        {
            if (key.Key is ConsoleKey.R or ConsoleKey.Enter) Restart();
            return;
        }

        if (key.Key is ConsoleKey.P or ConsoleKey.Escape)
        {
            status = status == "paused" ? "playing" : "paused";
            return;
        }

        if (status != "playing") return;

        switch (key.Key)
        {
            case ConsoleKey.LeftArrow:
            case ConsoleKey.A:
                TryMove(-1, 0);
                break;
            case ConsoleKey.RightArrow:
            case ConsoleKey.D:
                TryMove(1, 0);
                break;
            case ConsoleKey.UpArrow:
            case ConsoleKey.X:
            case ConsoleKey.W:
                TryRotate(1);
                break;
            case ConsoleKey.Z:
                TryRotate(-1);
                break;
            case ConsoleKey.DownArrow:
            case ConsoleKey.S:
                if (TryMove(0, 1)) score += 1;
                else LockPiece();
                gravityMs = 0;
                break;
            case ConsoleKey.Spacebar:
                HardDrop();
                break;
            case ConsoleKey.R:
                Restart();
                break;
        }
    }

    public void Update(long dt)
    {
        if (status == "clearing")
        {
            flashLeft -= dt;
            if (flashLeft <= 0) FinishClear();
            return;
        }

        if (status != "playing") return;

        gravityMs += dt;
        if (gravityMs >= GravityInterval())
        {
            gravityMs = 0;
            if (!TryMove(0, 1)) LockPiece();
        }
    }

    double GravityInterval()
    {
        int index = Math.Clamp(level - 1, 0, Gravity.Length - 1);
        return Gravity[index] / 60.0 * 1000.0;
    }

    public void Draw()
    {
        Console.SetCursorPosition(0, 0);
        Console.WriteLine("  ТЕТРИС");
        Console.WriteLine();

        var occupancy = Occupancy();
        var ghost = GhostCells();
        for (int y = 0; y < Rows; y++)
        {
            Console.Write("  |");
            for (int x = 0; x < Cols; x++)
            {
                string? type = occupancy[x, y];
                if (status == "clearing" && flashRows.Contains(y))
                {
                    WriteBlock(ConsoleColor.White);
                }
                else if (type is not null)
                {
                    WriteBlock(Colors[type]);
                }
                else if (ghost.Contains((x, y)))
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.Write("░░");
                }
                else
                {
                    Console.Write("  ");
                }
            }
            Console.ResetColor();
            Console.Write("|");
            WriteSidebar(y);
            Console.WriteLine();
        }

        Console.Write("  +");
        Console.Write(new string('-', Cols * 2));
        Console.WriteLine("+");
        Console.WriteLine();
        Console.WriteLine($"  Счёт {score:D6}   Уровень {level}   Линии {lines}   ");
        Console.WriteLine("  ← → движение  ↑/X поворот  Z против часовой");
        Console.WriteLine("  ↓ ускорить    Пробел сброс  P пауза  R заново");

        if (status == "paused") Console.WriteLine("  ПАУЗА                         ");
        else if (status == "over") Console.WriteLine("  ИГРА ОКОНЧЕНА  Enter/R — заново");
        else Console.WriteLine("                                 ");
    }

    void WriteSidebar(int y)
    {
        if (y == 1) Console.Write("  СЛЕДУЮЩАЯ");
        else if (y is >= 3 and <= 6)
        {
            Console.Write("  ");
            DrawNextRow(y - 3);
        }
        else Console.Write("            ");
    }

    void DrawNextRow(int localY)
    {
        var cells = Cells(next).Select(c => (c.X - 3, c.Y)).ToArray();
        int minX = cells.Min(c => c.Item1);
        int minY = cells.Min(c => c.Y);
        var set = cells.Select(c => (c.Item1 - minX, c.Y - minY)).ToHashSet();
        for (int x = 0; x < 4; x++)
        {
            if (set.Contains((x, localY))) WriteBlock(Colors[next.Type]);
            else Console.Write("  ");
        }
        Console.ResetColor();
    }

    static void WriteBlock(ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.Write("██");
    }

    string?[,] Occupancy()
    {
        var view = new string?[Cols, Rows];
        for (int y = 0; y < Rows; y++)
        {
            for (int x = 0; x < Cols; x++) view[x, y] = board[x, y];
        }

        if (status is "playing" or "paused")
        {
            foreach (var (x, y) in Cells(active)) view[x, y] = active.Type;
        }

        return view;
    }

    HashSet<(int X, int Y)> GhostCells()
    {
        var cells = new HashSet<(int X, int Y)>();
        if (status is not ("playing" or "paused")) return cells;
        var ghost = active;
        while (Fits(ghost with { Y = ghost.Y + 1 })) ghost = ghost with { Y = ghost.Y + 1 };
        if (ghost.Y == active.Y) return cells;
        foreach (var cell in Cells(ghost)) cells.Add(cell);
        return cells;
    }

    bool TryMove(int dx, int dy)
    {
        var moved = active with { X = active.X + dx, Y = active.Y + dy };
        if (!Fits(moved)) return false;
        active = moved;
        return true;
    }

    void TryRotate(int dir)
    {
        foreach (var kick in Kicks)
        {
            var rotated = active with
            {
                Rotation = (active.Rotation + dir + 4) % 4,
                X = active.X + kick.X,
                Y = active.Y + kick.Y,
            };
            if (Fits(rotated))
            {
                active = rotated;
                return;
            }
        }
    }

    void HardDrop()
    {
        int dropped = 0;
        while (TryMove(0, 1)) dropped++;
        score += dropped * 2;
        LockPiece();
    }

    void LockPiece()
    {
        foreach (var (x, y) in Cells(active)) board[x, y] = active.Type;
        var full = FullRows();
        if (full.Count > 0)
        {
            status = "clearing";
            flashRows = full;
            flashLeft = 180;
            return;
        }
        Spawn();
    }

    void FinishClear()
    {
        int cleared = FullRows().Count;
        var compact = new List<string?[]>();
        for (int y = Rows - 1; y >= 0; y--)
        {
            bool full = true;
            for (int x = 0; x < Cols; x++)
            {
                if (board[x, y] is null) { full = false; break; }
            }
            if (full) continue;
            var row = new string?[Cols];
            for (int x = 0; x < Cols; x++) row[x] = board[x, y];
            compact.Add(row);
        }

        Array.Clear(board);
        for (int i = 0; i < compact.Count; i++)
        {
            int y = Rows - 1 - i;
            for (int x = 0; x < Cols; x++) board[x, y] = compact[i][x];
        }

        lines += cleared;
        score += LineScore[cleared] * level;
        level = 1 + lines / 10;
        flashRows.Clear();
        status = "playing";
        Spawn();
    }

    HashSet<int> FullRows()
    {
        var rows = new HashSet<int>();
        for (int y = 0; y < Rows; y++)
        {
            bool full = true;
            for (int x = 0; x < Cols; x++)
            {
                if (board[x, y] is null) { full = false; break; }
            }
            if (full) rows.Add(y);
        }
        return rows;
    }

    void Spawn()
    {
        active = next with { X = 3, Y = 0, Rotation = 0 };
        next = TakePiece();
        gravityMs = 0;
        if (!Fits(active)) status = "over";
    }

    Piece TakePiece()
    {
        if (bag.Count == 0)
        {
            var list = Types.ToList();
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
            foreach (var type in list) bag.Enqueue(type);
        }

        return new Piece(bag.Dequeue(), 3, 0, 0);
    }

    bool Fits(Piece piece)
    {
        foreach (var (x, y) in Cells(piece))
        {
            if (x < 0 || x >= Cols || y < 0 || y >= Rows || board[x, y] is not null) return false;
        }
        return true;
    }

    static List<(int X, int Y)> Cells(Piece piece)
    {
        var result = new List<(int X, int Y)>(4);
        foreach (var (x, y) in Shapes[piece.Type][piece.Rotation])
        {
            result.Add((x + piece.X, y + piece.Y));
        }
        return result;
    }
}

internal readonly record struct Piece(string Type, int X, int Y, int Rotation);
