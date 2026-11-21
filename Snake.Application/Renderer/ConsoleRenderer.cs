using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Snake.Domain;

namespace Snake.Application
{
    /// <summary>
    /// Отрисовка кадра в консоли: псевдографика + 24-битные ANSI-цвета.
    /// Кадры одного размера перерисовываются без Console.Clear — так нет мерцания,
    /// но самый первый кадр очищает экран, чтобы не оставалось «хвостов» от экрана прогресса.
    /// </summary>
    public class ConsoleRenderer
    {
        private const char HeadChar = '█';
        private const char BodyChar = '▓';
        private const char FoodChar = '●';
        private const char ObstacleChar = '▒';
        private const string Reset = "\u001b[0m";

        private static readonly Rgb WallColor = new Rgb(86, 156, 214);
        private static readonly Rgb TitleColor = new Rgb(214, 234, 255);
        private static readonly Rgb DimColor = new Rgb(138, 148, 164);
        private static readonly Rgb ValueColor = new Rgb(238, 244, 255);
        private static readonly Rgb GoldColor = new Rgb(255, 205, 90);
        private static readonly Rgb HeadColor = new Rgb(206, 255, 140);
        private static readonly Rgb BodyStartColor = new Rgb(112, 222, 116);
        private static readonly Rgb BodyEndColor = new Rgb(26, 108, 58);
        private static readonly Rgb ObstacleColor = new Rgb(126, 132, 158);

        private readonly GameSettings _settings;
        private readonly bool _color;
        private bool _firstFrame = true;
        private int _tick;

        public ConsoleRenderer(GameSettings settings, bool color = true)
        {
            _settings = settings;
            _color = color;
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

            if (_firstFrame)
            {
                _firstFrame = false;

                // Экран прогресса мог быть шире игрового поля — стираем его целиком,
                // иначе справа от рамки остаются обрывки строк.
                try
                {
                    Console.Clear();
                    Console.CursorVisible = false;
                }
                catch (IOException)
                {
                }
                catch (PlatformNotSupportedException)
                {
                }
            }

            Console.SetCursorPosition(0, 0);
            _tick++;

            Dictionary<Point, int> body = BuildBodyMap(engine);

            WriteLine(BuildTitle());
            WriteLine(BuildStats(engine));
            WriteLine(BuildRecordLine(engine, highScore));
            WriteLine(string.Empty);
            WriteLine(BuildBorder(true));

            Line row = new Line(_color);
            for (int y = 0; y < _settings.BoardHeight; y++)
            {
                row.Clear();
                row.Append("│", WallColor);

                for (int x = 0; x < _settings.BoardWidth; x++)
                {
                    Point cell = new Point(x, y);
                    int index;

                    if (cell == engine.Head)
                    {
                        row.Color(HeadColor);
                        row.Append(TwoChars(HeadChar));
                    }
                    else if (body.TryGetValue(cell, out index))
                    {
                        row.Color(BodyColor(index, body.Count));
                        row.Append(TwoChars(BodyChar));
                    }
                    else if (engine.Food.HasValue && engine.Food.Value == cell)
                    {
                        // Яблоко «дышит»: цвет меняется от тика к тику.
                        row.Color(FoodColor());
                        row.Append(FoodChar).Append(" ");
                    }
                    else if (engine.Board.IsObstacle(cell))
                    {
                        row.Color(ObstacleColor);
                        row.Append(TwoChars(ObstacleChar));
                    }
                    else
                    {
                        // Пустая клетка — без цветовых кодов, чтобы не гонять их зря.
                        row.Append("  ");
                    }
                }

                row.Append("│", WallColor);
                row.Fill(FrameWidth);
                row.Finish();
                Console.WriteLine(row.ToString());
            }

            WriteLine(BuildBorder(false));
            WriteLine(string.Empty);
            WriteLine(BuildStatus(engine));

            if (_color)
            {
                Console.Write(Reset);
            }
        }

        /// <summary>Кадр в виде обычного текста — для демо-режима и файлов.</summary>
        public string RenderText(GameEngine engine, int highScore)
        {
            Dictionary<Point, int> body = BuildBodyMap(engine);
            StringBuilder builder = new StringBuilder();

            builder.AppendLine("ЗМЕЙКА — консольная классика");
            builder.AppendLine("Очки: " + engine.Score + "   Уровень: " + engine.Level + "   Длина: " + engine.Length);
            builder.AppendLine("Рекорд: " + highScore + "   Скорость: " + engine.CellsPerSecond.ToString("0.0") + " кл/с");
            builder.Append('╭').Append(new string('─', FrameWidth - 2)).AppendLine("╮");

            for (int y = 0; y < _settings.BoardHeight; y++)
            {
                builder.Append('│');
                for (int x = 0; x < _settings.BoardWidth; x++)
                {
                    Point cell = new Point(x, y);
                    int index;

                    if (cell == engine.Head)
                    {
                        builder.Append(HeadChar, 2);
                    }
                    else if (body.TryGetValue(cell, out index))
                    {
                        builder.Append(BodyChar, 2);
                    }
                    else if (engine.Food.HasValue && engine.Food.Value == cell)
                    {
                        builder.Append(FoodChar).Append(' ');
                    }
                    else if (engine.Board.IsObstacle(cell))
                    {
                        builder.Append(ObstacleChar, 2);
                    }
                    else
                    {
                        builder.Append("  ");
                    }
                }

                builder.AppendLine("│");
            }

            builder.Append('╰').Append(new string('─', FrameWidth - 2)).AppendLine("╯");
            builder.AppendLine(StatusLine(engine));
            return builder.ToString();
        }

        private Dictionary<Point, int> BuildBodyMap(GameEngine engine)
        {
            IReadOnlyList<Point> segments = engine.Snake.Segments;
            Dictionary<Point, int> map = new Dictionary<Point, int>(segments.Count);
            for (int i = 0; i < segments.Count; i++)
            {
                map[segments[i]] = i;
            }

            return map;
        }

        /// <summary>Градиент от светлой головы к тёмному хвосту.</summary>
        private static Rgb BodyColor(int index, int count)
        {
            double t = count <= 1 ? 0.0 : (double)index / (count - 1);
            return new Rgb(
                (int)(BodyStartColor.R + (BodyEndColor.R - BodyStartColor.R) * t),
                (int)(BodyStartColor.G + (BodyEndColor.G - BodyStartColor.G) * t),
                (int)(BodyStartColor.B + (BodyEndColor.B - BodyStartColor.B) * t));
        }

        private Rgb FoodColor()
        {
            return _tick % 4 < 2
                ? new Rgb(255, 92, 72)
                : new Rgb(255, 168, 96);
        }

        private static string TwoChars(char symbol)
        {
            return new string(symbol, 2);
        }

        private string BuildTitle()
        {
            Line line = new Line(_color);
            line.Append("  ЗМЕЙКА", TitleColor);
            line.Append("  — консольная классика", DimColor);
            line.Fill(FrameWidth);
            line.Finish();
            return line.ToString();
        }

        private string BuildStats(GameEngine engine)
        {
            Line line = new Line(_color);
            line.Append("  Очки: ", DimColor);
            line.Append(engine.Score.ToString(), GoldColor);
            line.Append("   Уровень: ", DimColor);
            line.Append(engine.Level.ToString(), ValueColor);
            line.Append("   Длина: ", DimColor);
            line.Append(engine.Length.ToString(), ValueColor);
            line.Fill(FrameWidth);
            line.Finish();
            return line.ToString();
        }

        private string BuildRecordLine(GameEngine engine, int highScore)
        {
            Line line = new Line(_color);
            line.Append("  Рекорд: ", DimColor);
            line.Append(highScore.ToString(), GoldColor);
            line.Append("   Скорость: ", DimColor);
            line.Append(engine.CellsPerSecond.ToString("0.0") + " кл/с", ValueColor);
            line.Fill(FrameWidth);
            line.Finish();
            return line.ToString();
        }

        private string BuildBorder(bool top)
        {
            Line line = new Line(_color);
            string fill = new string('─', FrameWidth - 2);
            line.Append(top ? "╭" + fill + "╮" : "╰" + fill + "╯", WallColor);
            line.Fill(FrameWidth);
            line.Finish();
            return line.ToString();
        }

        private string BuildStatus(GameEngine engine)
        {
            Line line = new Line(_color);
            line.Append(StatusLine(engine), StatusColor(engine.State));
            line.Fill(FrameWidth);
            line.Finish();
            return line.ToString();
        }

        private static void WriteLine(string text)
        {
            Console.WriteLine(text);
        }

        private static string StatusLine(GameEngine engine)
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

        private static Rgb StatusColor(GameState state)
        {
            switch (state)
            {
                case GameState.GameOver:
                    return new Rgb(255, 110, 110);
                case GameState.Won:
                    return new Rgb(140, 255, 150);
                case GameState.Paused:
                    return new Rgb(255, 205, 90);
                default:
                    return DimColor;
            }
        }

        /// <summary>Цвет в 24-битном ANSI-виде.</summary>
        private readonly struct Rgb : IEquatable<Rgb>
        {
            public Rgb(int r, int g, int b)
            {
                R = r;
                G = g;
                B = b;
            }

            public int R { get; }

            public int G { get; }

            public int B { get; }

            public string ToAnsi()
            {
                return "\u001b[38;2;" + R + ";" + G + ";" + B + "m";
            }

            public bool Equals(Rgb other)
            {
                return R == other.R && G == other.G && B == other.B;
            }

            public override bool Equals(object obj)
            {
                return obj is Rgb other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = R * 397;
                    hash = (hash ^ G) * 397;
                    return hash ^ B;
                }
            }
        }

        /// <summary>Строка кадра: содержимое + цвет, с гарантированной видимой длиной.</summary>
        private sealed class Line
        {
            private readonly StringBuilder _builder = new StringBuilder(512);
            private readonly bool _color;
            private int _visible;
            private bool _colorSet;
            private Rgb _current;

            public Line(bool color)
            {
                _color = color;
            }

            public void Clear()
            {
                _builder.Clear();
                _visible = 0;
                _colorSet = false;
            }

            public void Color(Rgb color)
            {
                if (!_color || (_colorSet && _current.Equals(color)))
                {
                    return;
                }

                _builder.Append(color.ToAnsi());
                _colorSet = true;
                _current = color;
            }

            public Line Append(string text)
            {
                _builder.Append(text);
                _visible += text.Length;
                return this;
            }

            public Line Append(char symbol)
            {
                _builder.Append(symbol);
                _visible++;
                return this;
            }

            public void Append(string text, Rgb color)
            {
                Color(color);
                Append(text);
            }

            public void Fill(int width)
            {
                if (_visible < width)
                {
                    _builder.Append(' ', width - _visible);
                    _visible = width;
                }
            }

            public void Finish()
            {
                if (_color && _colorSet)
                {
                    _builder.Append(Reset);
                }
            }

            public override string ToString()
            {
                return _builder.ToString();
            }
        }
    }
}
