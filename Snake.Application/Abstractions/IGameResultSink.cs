namespace Snake.Application.Abstractions
{
    /// <summary>
    /// Порт записи результата партии. Ядро не знает, куда именно пишутся данные:
    /// реализация живёт в Snake.Persistence (PostgreSQL), а презентер её подставляет.
    /// </summary>
    public interface IGameResultSink
    {
        void Save(GameRunResult result);
    }
}
