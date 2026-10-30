using System.Text;

namespace SnakeGame;

/// <summary>Точка на игровом поле (координаты в символах консоли).</summary>
readonly struct Point : IEquatable<Point>
{
    public int X { get; }
    public int Y { get; }

    public Point(int x, int y) { X = x; Y = y; }

    public Point Move(Direction d) => d switch
    {
        Direction.Up    => new Point(X, Y - 1),
        Direction.Down  => new Point(X, Y + 1),
        Direction.Left  => new Point(X - 1, Y),
        Direction.Right => new Point(X + 1, Y),
        _ => this
    };

    public bool Equals(Point other) => X == other.X && Y == other.Y;
    public override bool Equals(object? obj) => obj is Point p && Equals(p);
    public override int GetHashCode() => HashCode.Combine(X, Y);
    public static bool operator ==(Point a, Point b) => a.Equals(b);
    public static bool operator !=(Point a, Point b) => !a.Equals(b);
}

enum Direction { Up, Down, Left, Right }

/// <summary>Консольная игра «Змейка».</summary>
static class Program
{
    // Размер игрового поля в символах.
    const int FieldWidth = 40;
    const int FieldHeight = 20;

    // Символы отрисовки.
    const char WallChar = '#';
    const char HeadChar = '█';
    const char BodyChar = '▓';
    const char FoodChar = '●';

    static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.CursorVisible = false;

        bool playAgain = true;
        while (playAgain)
        {
            int score = RunGame();
            playAgain = ShowGameOver(score);
        }

        Console.Clear();
        Console.SetCursorPosition(0, 0);
        Console.WriteLine("Спасибо за игру!");
    }

    /// <summary>Один раунд игры. Возвращает итоговое количество очков.</summary>
    static int RunGame()
    {
        var random = new Random();

        // Змейка: head — первый элемент списка.
        var snake = new LinkedList<Point>();
        snake.AddFirst(new Point(FieldWidth / 2, FieldHeight / 2));
        snake.AddLast(new Point(FieldWidth / 2 - 1, FieldHeight / 2));
        snake.AddLast(new Point(FieldWidth / 2 - 2, FieldHeight / 2));

        var occupied = new HashSet<Point>(snake);
        Direction direction = Direction.Right;
        Direction nextDirection = Direction.Right;

        Point food = SpawnFood(random, occupied);
        int score = 0;
        int delayMs = 150;              // скорость: пауза между шагами
        const int minDelay = 60;        // предел ускорения

        DrawFrame(snake, food, score);

        while (true)
        {
            PollInput(ref nextDirection);

            // Нельзя развернуться на 180°.
            if (!IsOpposite(direction, nextDirection))
                direction = nextDirection;

            Point newHead = snake.First!.Value.Move(direction);

            // Столкновение со стеной или собственным телом — конец игры.
            bool hitsWall = newHead.X < 0 || newHead.Y < 0 ||
                            newHead.X >= FieldWidth || newHead.Y >= FieldHeight;
            bool hitsSelf = occupied.Contains(newHead) &&
                            !(snake.Last!.Value == newHead && !AteFood(newHead, food));
            // Примечание: хвост сдвинется, если еда не съедена,
            // поэтому попадание в последнюю клетку допустимо.

            if (hitsWall || hitsSelf)
                return score;

            bool ate = newHead == food;

            snake.AddFirst(newHead);
            occupied.Add(newHead);

            if (ate)
            {
                score += 10;
                if (delayMs > minDelay) delayMs -= 4;   // ускоряемся с каждой едой
                food = SpawnFood(random, occupied);
            }
            else
            {
                Point tail = snake.Last!.Value;
                snake.RemoveLast();
                // Клетка хвоста свободна, только если туда не переместилась голова.
                if (!(tail == newHead)) occupied.Remove(tail);
            }

            DrawFrame(snake, food, score);
            Thread.Sleep(delayMs);
        }
    }

    static bool AteFood(Point head, Point food) => head == food;

    static bool IsOpposite(Direction a, Direction b) =>
        (a == Direction.Up && b == Direction.Down) ||
        (a == Direction.Down && b == Direction.Up) ||
        (a == Direction.Left && b == Direction.Right) ||
        (a == Direction.Right && b == Direction.Left);

    /// <summary>Читает все накопленные нажатия клавиш, запоминает последнее подходящее направление.</summary>
    static void PollInput(ref Direction nextDirection)
    {
        while (Console.KeyAvailable)
        {
            var key = Console.ReadKey(intercept: true).Key;
            switch (key)
            {
                case ConsoleKey.UpArrow or ConsoleKey.W:
                    nextDirection = Direction.Up; break;
                case ConsoleKey.DownArrow or ConsoleKey.S:
                    nextDirection = Direction.Down; break;
                case ConsoleKey.LeftArrow or ConsoleKey.A:
                    nextDirection = Direction.Left; break;
                case ConsoleKey.RightArrow or ConsoleKey.D:
                    nextDirection = Direction.Right; break;
                case ConsoleKey.Escape:
                    Environment.Exit(0);
                    break;
            }
        }
    }

    /// <summary>Случайная свободная клетка для еды.</summary>
    static Point SpawnFood(Random random, HashSet<Point> occupied)
    {
        while (true)
        {
            var p = new Point(random.Next(FieldWidth), random.Next(FieldHeight));
            if (!occupied.Contains(p)) return p;
        }
    }

    /// <summary>Полная перерисовка кадра в буферизованной строке (без мерцания).</summary>
    static void DrawFrame(LinkedList<Point> snake, Point food, int score)
    {
        var buffer = new StringBuilder((FieldWidth + 2) * (FieldHeight + 3));

        // Верхняя граница
        buffer.Append(WallChar, FieldWidth + 2).Append('\n');

        var body = new HashSet<Point>(snake);

        for (int y = 0; y < FieldHeight; y++)
        {
            buffer.Append(WallChar);
            for (int x = 0; x < FieldWidth; x++)
            {
                var p = new Point(x, y);
                if (p == snake.First!.Value)      buffer.Append(HeadChar);
                else if (body.Contains(p))        buffer.Append(BodyChar);
                else if (p == food)               buffer.Append(FoodChar);
                else                              buffer.Append(' ');
            }
            buffer.Append(WallChar).Append('\n');
        }

        // Нижняя граница и счёт
        buffer.Append(WallChar, FieldWidth + 2).Append('\n');
        buffer.Append($" Счёт: {score} | Стрелки/WASD — движение, Esc — выход ");

        // Выводим поверх предыдущего кадра, возвращая курсор в начало области.
        Console.SetCursorPosition(0, 0);
        Console.Write(buffer.ToString());
    }

    /// <summary>Экран проигрыша. Возвращает true, если игрок хочет сыграть ещё раз.</summary>
    static bool ShowGameOver(int score)
    {
        Console.WriteLine();
        Console.WriteLine();
        Console.SetCursorPosition(0, FieldHeight + 4);
        Console.WriteLine($"Игра окончена! Ваш счёт: {score}");
        Console.WriteLine("Нажмите [Enter] — сыграть ещё раз, любую другую клавишу — выйти.");

        var key = Console.ReadKey(intercept: true).Key;
        Console.Clear();
        return key == ConsoleKey.Enter;
    }
}
