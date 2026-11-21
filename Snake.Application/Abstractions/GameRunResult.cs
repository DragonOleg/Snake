using System;
using Snake.Domain;

namespace Snake.Application.Abstractions
{
    /// <summary>
    /// Итог одной завершённой партии. Без UI, без БД и без времени процесса —
    /// только данные, которые можно сохранить и показать.
    /// </summary>
    public sealed class GameRunResult
    {
        /// <summary>Момент окончания партии (UTC).</summary>
        public DateTime FinishedAtUtc { get; set; }

        public int Score { get; set; }

        public int Length { get; set; }

        public int Level { get; set; }

        public int Eaten { get; set; }

        public int DurationMs { get; set; }

        public int BoardWidth { get; set; }

        public int BoardHeight { get; set; }

        /// <summary>GameOver или Won.</summary>
        public GameState Outcome { get; set; }

        public string StatusMessage { get; set; }

        public bool IsWin
        {
            get { return Outcome == GameState.Won; }
        }

        public string OutcomeText
        {
            get
            {
                switch (Outcome)
                {
                    case GameState.Won:
                        return "победа";
                    case GameState.GameOver:
                        return "поражение";
                    default:
                        return Outcome.ToString();
                }
            }
        }
    }
}
