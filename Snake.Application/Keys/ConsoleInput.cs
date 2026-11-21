using System;
using Snake.Domain;

namespace Snake.Application
{
    /// <summary>
    /// Неблокирующее чтение клавиш. Стрелки на Unix-терминалах приходят
    /// последовательностью ESC [ A/B/C/D, поэтому она собирается по одному символу
    /// между вызовами Poll — без ожиданий и без потери остальных клавиш.
    /// </summary>
    public static class ConsoleInput
    {
        private static int _escapeState;

        public static GameCommand Poll()
        {
            if (Console.IsInputRedirected)
            {
                return GameCommand.None;
            }

            if (!Console.KeyAvailable)
            {
                return GameCommand.None;
            }

            ConsoleKeyInfo key = Console.ReadKey(true);
            char symbol = key.KeyChar;

            if (_escapeState == 0)
            {
                if (key.Key == ConsoleKey.Escape)
                {
                    _escapeState = 1;
                    return GameCommand.None;
                }

                return Map(key.Key);
            }

            if (_escapeState == 1)
            {
                if (symbol == '[' || symbol == 'O')
                {
                    _escapeState = 2;
                    return GameCommand.None;
                }

                _escapeState = 0;
                return Map(key.Key);
            }

            _escapeState = 0;
            switch (symbol)
            {
                case 'A':
                    return GameCommand.Up;
                case 'B':
                    return GameCommand.Down;
                case 'C':
                    return GameCommand.Right;
                case 'D':
                    return GameCommand.Left;
                default:
                    return GameCommand.None;
            }
        }

        private static GameCommand Map(ConsoleKey key)
        {
            switch (key)
            {
                case ConsoleKey.UpArrow:
                case ConsoleKey.W:
                    return GameCommand.Up;
                case ConsoleKey.DownArrow:
                case ConsoleKey.S:
                    return GameCommand.Down;
                case ConsoleKey.LeftArrow:
                case ConsoleKey.A:
                    return GameCommand.Left;
                case ConsoleKey.RightArrow:
                case ConsoleKey.D:
                    return GameCommand.Right;
                case ConsoleKey.P:
                    return GameCommand.Pause;
                case ConsoleKey.R:
                    return GameCommand.Restart;
                case ConsoleKey.Q:
                    return GameCommand.Quit;
                default:
                    return GameCommand.None;
            }
        }
    }
}
