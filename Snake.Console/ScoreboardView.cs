using System;
using System.Collections.Generic;
using Snake.Application.Abstractions;

namespace Snake.Presentation
{
    /// <summary>Экран прогресса: сводка, лучшие и последние партии.</summary>
    public static class ScoreboardView
    {
        private const int DefaultWidth = 60;

        private static int _width = DefaultWidth;
        private static int _limit = 100;

        public static void Print(
            GameProgress progress,
            IReadOnlyList<GameRunResult> best,
            IReadOnlyList<GameRunResult> recent)
        {
            int console = ConsoleWidth();
            _width = console <= 0 ? DefaultWidth : Math.Max(40, Math.Min(DefaultWidth, console));
            // При перенаправленном выводе обрезка не нужна: переносов там нет.
            _limit = console <= 0 ? int.MaxValue : Math.Max(40, console - 1);

            Header("ПРОГРЕСС");

            if (progress == null || !progress.HasGames)
            {
                Line("  Пока нет сохранённых партий — сыграйте первую!");
                Rule();
                return;
            }

            Row("Партий сыграно", progress.Games.ToString());
            Row("Лучший счёт", progress.BestScore.ToString());
            Row("Средний счёт", progress.AverageScore.ToString("0.0"));
            Row("Рекорд длины", progress.BestLength + " кл.");
            Row("Макс. уровень", progress.BestLevel.ToString());
            Row("Всего съедено", progress.TotalEaten.ToString());
            if (progress.Last != null)
            {
                Row("Последняя", Describe(progress.Last));
            }

            if (best != null && best.Count > 0)
            {
                Header("ЛУЧШИЕ " + best.Count);
                for (int i = 0; i < best.Count; i++)
                {
                    Line("  " + (i + 1) + ". " + Describe(best[i]));
                }
            }

            if (recent != null && recent.Count > 0)
            {
                Header("ПОСЛЕДНИЕ " + recent.Count);
                for (int i = 0; i < recent.Count; i++)
                {
                    Line("  " + Describe(recent[i]));
                }
            }

            Rule();
        }

        public static string Describe(GameRunResult result)
        {
            return Local(result.FinishedAtUtc).ToString("dd.MM.yyyy HH:mm")
                + " — " + result.Score + " очков, длина " + result.Length
                + ", ур. " + result.Level + ", " + result.OutcomeText
                + (string.IsNullOrEmpty(result.StatusMessage) ? string.Empty : " (" + result.StatusMessage + ")");
        }

        /// <summary>Печатает строку, обрезая её по ширине консоли — иначе перенос ломает экран.</summary>
        private static void Line(string text)
        {
            Console.WriteLine(Truncate(text));
        }

        private static void Header(string title)
        {
            string line = "─ " + title + " ";
            Line(line + new string('─', Math.Max(0, _width - line.Length)));
        }

        private static void Rule()
        {
            Line(new string('─', _width));
            Console.WriteLine();
        }

        private static void Row(string label, string value)
        {
            Line("  " + label.PadRight(16) + ": " + value);
        }

        private static string Truncate(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            if (text.Length <= _limit)
            {
                return text;
            }

            return text.Substring(0, Math.Max(0, _limit - 1)) + "…";
        }

        private static int ConsoleWidth()
        {
            try
            {
                // 0 — ширина неизвестна (вывод перенаправлен).
                return Console.IsOutputRedirected ? 0 : Console.WindowWidth;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        private static DateTime Local(DateTime value)
        {
            if (value.Kind == DateTimeKind.Utc)
            {
                return value.ToLocalTime();
            }

            return DateTime.SpecifyKind(value, DateTimeKind.Utc).ToLocalTime();
        }
    }
}
