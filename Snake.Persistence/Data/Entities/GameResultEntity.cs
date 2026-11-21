using System;

namespace Snake.Persistence.Data.Entities
{
    /// <summary>Сохранённый результат одной партии (таблица game_results).</summary>
    public class GameResultEntity
    {
        public long Id { get; set; }

        public DateTime FinishedAtUtc { get; set; }

        public int Score { get; set; }

        public int Length { get; set; }

        public int Level { get; set; }

        public int Eaten { get; set; }

        public int DurationMs { get; set; }

        public int BoardWidth { get; set; }

        public int BoardHeight { get; set; }

        /// <summary>GameOver или Won в виде строки.</summary>
        public string Outcome { get; set; }

        public string StatusMessage { get; set; }
    }
}
