using System;
using System.Text;
using Snake.Domain;

namespace Snake.Application
{
    /// <summary>Отрисовка кадра в консоли. Полный кадр перерисовывается без Console.Clear — так нет мерцания.</summary>
    public class ConsoleRenderer
    {
        private const char HeadChar = '@';
        private const char BodyChar = '█';
        private const char FoodChar = '*';
        private const char ObstacleChar = '▓';
        private const char EmptyChar = ' ';

        private readonly GameSettings _settings;

        public ConsoleRenderer(GameSettings settings)
        {
            _settings = settings;
        }

        public int FrameWidth
        {
            get { return _settings.BoardWidth * 2 + 2; }
        }

        public void Draw(GameEngine engine, int highScore)
        {
            if (Console.IsOutputRedirected)
            {
                return;
            }

            Console.SetCursorPosition(0, 0);
            Console.ForegroundColor = ConsoleColor.White;

            WriteLinePadded("  ЗМЕЙКА — консольная классика");
            WriteLinePadded(" Очки: " + engine.Score + "   Уровень: " + engine.Level + "   Длина: " + engine.Length);
            WriteLinePadded(" Рекорд: " + highScore + "   Скорость: " + engine.CellsPerSecond.ToString("0.0") + " кл/с");
            WriteLinePadded(string.Empty);

            Console.ForegroundColor = ConsoleColor.DarkGray;
            WriteLinePadded("╔" + new string('═', FrameWidth - 2) + "╗");

            for (int y = 0; y < _settings.BoardHeight; y++)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write("║");
                ConsoleColor current = ConsoleColor.DarkGray;

                for (int x = 0; x < _settings.BoardWidth; x++)
                {
                    ConsoleColor color;
                    char symbol = CellChar(engine, new Point(x, y), out color);
                    if (color != current)
                    {
                        Console.ForegroundColor = color;
                        current = color;
                    }

                    Console.Write(symbol);
                    Console.Write(symbol);
                }

                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write("║");
                Console.WriteLine();
            }

            Console.ForegroundColor = ConsoleColor.DarkGray;
            WriteLinePadded("╚" + new string('═', FrameWidth - 2) + "╝");
            WriteLinePadded(string.Empty);

            Console.ForegroundColor = StatusColor(engine.State);
            WriteLinePadded(StatusLine(engine));
            Console.ResetColor();
        }

        public string RenderText(GameEngine engine, int highScore)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("ЗМЕЙКА — консольная классика");
            builder.AppendLine("Очки: " + engine.Score + "   Уровень: " + engine.Level + "   Длина: " + engine.Length);
            builder.AppendLine("Рекорд: " + highScore + "   Скорость: " + engine.CellsPerSecond.ToString("0.0") + " кл/с");
            builder.Append("╔").Append(new string('═', FrameWidth - 2)).AppendLine("╗");

            for (int y = 0; y < _settings.BoardHeight; y++)
            {
                builder.Append("║");
                for (int x = 0; x < _settings.BoardWidth; x++)
                {
                    ConsoleColor color;
                    char symbol = CellChar(engine, new Point(x, y), out color);
                    builder.Append(symbol).Append(symbol);
                }

                builder.AppendLine("║");
            }

            builder.Append("╚").Append(new string('═', FrameWidth - 2)).AppendLine("╝");
            builder.AppendLine(StatusLine(engine));
            return builder.ToString();
        }

        private char CellChar(GameEngine engine, Point cell, out ConsoleColor color)
        {
            color = ConsoleColor.Gray;

            if (cell == engine.Head)
            {
                color = ConsoleColor.Green;
                return HeadChar;
            }

            if (engine.Food.HasValue && engine.Food.Value == cell)
            {
                color = ConsoleColor.Red;
                return FoodChar;
            }

            if (engine.Board.IsObstacle(cell))
            {
                color = ConsoleColor.DarkGray;
                return ObstacleChar;
            }

            if (engine.Snake.Contains(cell))
            {
                color = ConsoleColor.DarkGreen;
                return BodyChar;
            }

            return EmptyChar;
        }

        private string StatusLine(GameEngine engine)
        {
            switch (engine.State)
            {
                case GameState.Paused:
                    return " ПАУЗА — P продолжить, R — заново, Q — выход";
                case GameState.GameOver:
                    return " КОНЕЦ: " + engine.StatusMessage + ". Очки: " + engine.Score + ". R — заново, Q — выход";
                case GameState.Won:
                    return " ПОБЕДА! Очки: " + engine.Score + ". R — заново, Q — выход";
                default:
                    return " Стрелки/WASD — движение, P — пауза, R — заново, Q — выход";
            }
        }

        private static ConsoleColor StatusColor(GameState state)
        {
            switch (state)
            {
                case GameState.GameOver:
                    return ConsoleColor.Red;
                case GameState.Won:
                    return ConsoleColor.Green;
                case GameState.Paused:
                    return ConsoleColor.Yellow;
                default:
                    return ConsoleColor.Gray;
            }
        }

        private void WriteLinePadded(string text)
        {
            if (text.Length > FrameWidth)
            {
                text = text.Substring(0, FrameWidth);
            }

            Console.WriteLine(text.PadRight(FrameWidth));
        }
    }
}
