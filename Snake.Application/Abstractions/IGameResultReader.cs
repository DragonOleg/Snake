using System.Collections.Generic;

namespace Snake.Application.Abstractions
{
    /// <summary>
    /// Порт чтения сохранённых результатов: прогресс, лучшие и последние партии.
    /// </summary>
    public interface IGameResultReader
    {
        /// <summary>Сводка по всем партиям (счётчики, рекорды, последняя игра).</summary>
        GameProgress GetProgress();

        /// <summary>Лучшие партии по счёту (по убыванию).</summary>
        IReadOnlyList<GameRunResult> GetBest(int count);

        /// <summary>Последние партии по времени (сначала свежие).</summary>
        IReadOnlyList<GameRunResult> GetRecent(int count);
    }
}
