using System;
using Snake.Domain;

namespace Snake.Application
{
    /// <summary>Демо без участия игрока: бот играет заданное число тиков, затем печатается итоговое поле.</summary>
    public static class DemoRunner
    {
        public static int Run(int ticks, int seed)
        {
            GameSettings settings = new GameSettings();
            GameEngine engine = new GameEngine(settings, new Random(seed));
            ConsoleRenderer renderer = new ConsoleRenderer(settings);
            SimpleBot bot = new SimpleBot();

            engine.Start();
            int performed = 0;
            for (int i = 0; i < ticks && engine.State == GameState.Running; i++)
            {
                engine.TryTurn(bot.Choose(engine));
                engine.Tick();
                performed++;
            }

            Console.WriteLine("Демо: тиков " + performed + ", очки " + engine.Score + ", длина " + engine.Length + ", уровень " + engine.Level);
            Console.WriteLine("Состояние: " + engine.State + " (" + engine.StatusMessage + ")");
            Console.WriteLine();
            Console.Write(renderer.RenderText(engine, engine.Score));
            return 0;
        }
    }
}
