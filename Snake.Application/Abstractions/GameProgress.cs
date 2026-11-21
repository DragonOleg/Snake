using System;

namespace Snake.Application.Abstractions
{
    /// <summary>Сводный прогресс игрока по всем сохранённым партиям.</summary>
    public sealed class GameProgress
    {
        public int Games { get; set; }

        public int BestScore { get; set; }

        public double AverageScore { get; set; }

        public int BestLength { get; set; }

        public int BestLevel { get; set; }

        public int TotalEaten { get; set; }

        public DateTime? FirstPlayedUtc { get; set; }

        /// <summary>Последняя завершённая партия или null, если игр ещё не было.</summary>
        public GameRunResult Last { get; set; }

        public bool HasGames
        {
            get { return Games > 0; }
        }
    }
}
