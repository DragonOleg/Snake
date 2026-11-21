using System;
using Snake.Core;
using Snake.Domain;

namespace Snake.Application
{
    /// <summary>Самопроверка логики без внешних фреймворков: `dotnet run -- --selftest`.</summary>
    public static class SelfTest
    {
        private static int _failed;
        private static int _total;

        public static int Run()
        {
            _failed = 0;
            _total = 0;

            Console.WriteLine("Самопроверка логики змейки");
            Console.WriteLine();

            StartState();
            Movement();
            TurnRules();
            Eating();
            WallCollision();
            ObstacleCollision();
            SelfCollision();
            FoodPlacement();
            SpeedProgression();

            Console.WriteLine();
            Console.WriteLine("Итог: " + _total + " проверок, ошибок: " + _failed);
            return _failed == 0 ? 0 : 1;
        }

        private static void StartState()
        {
            GameEngine engine = CreateEngine(1);
            engine.Start();

            Check("старт: состояние Running", engine.State == GameState.Running);
            Check("старт: длина " + engine.Length, engine.Length == 3);
            Check("старт: счёт 0", engine.Score == 0);
            Check("старт: еда есть", engine.Food.HasValue);
            Check("старт: еда не на змейке", engine.Food.HasValue && !engine.Snake.Contains(engine.Food.Value));
        }

        private static void Movement()
        {
            GameEngine engine = CreateEngine(2);
            engine.Start();
            engine.ForceFood(new Point(0, 0));

            Point before = engine.Head;
            TickResult result = engine.Tick();

            Check("движение вправо", result == TickResult.Moved);
            Check("голова сдвинулась на 1", engine.Head.X == before.X + 1 && engine.Head.Y == before.Y);
            Check("длина не изменилась", engine.Length == 3);
        }

        private static void TurnRules()
        {
            GameEngine engine = CreateEngine(3);
            engine.Start();

            Check("разворот назад запрещён", !engine.TryTurn(Direction.Left));
            Check("поворот вверх разрешён", engine.TryTurn(Direction.Up));
            Check("поворот вниз после вверх запрещён", !engine.TryTurn(Direction.Down));
        }

        private static void Eating()
        {
            GameEngine engine = CreateEngine(4);
            engine.Start();
            engine.ForceFood(engine.Head + Direction.Right.Delta());

            TickResult result = engine.Tick();

            Check("еда: результат Ate", result == TickResult.Ate);
            Check("еда: +10 очков", engine.Score == 10);
            Check("еда: длина 4", engine.Length == 4);
            Check("еда: новая еда есть", engine.Food.HasValue);
        }

        private static void WallCollision()
        {
            GameEngine engine = CreateEngine(5);
            engine.Start();

            TickResult last = TickResult.Moved;
            for (int i = 0; i < 40 && engine.State == GameState.Running; i++)
            {
                engine.ForceFood(new Point(0, 0));
                last = engine.Tick();
            }

            Check("стена: HitWall", last == TickResult.HitWall);
            Check("стена: GameOver", engine.State == GameState.GameOver);
        }

        private static void ObstacleCollision()
        {
            GameEngine engine = CreateEngine(6);
            engine.Start();
            engine.Board.AddObstacle(engine.Head + new Point(3, 0));

            TickResult last = TickResult.Moved;
            for (int i = 0; i < 5 && engine.State == GameState.Running; i++)
            {
                engine.ForceFood(new Point(0, 0));
                last = engine.Tick();
            }

            Check("препятствие: HitObstacle", last == TickResult.HitObstacle);
            Check("препятствие: GameOver", engine.State == GameState.GameOver);
        }

        private static void SelfCollision()
        {
            GameEngine engine = CreateEngine(7);
            engine.Start();

            for (int i = 0; i < 2; i++)
            {
                engine.ForceFood(engine.Head + Direction.Right.Delta());
                engine.Tick();
            }

            engine.ForceFood(new Point(0, 0));
            engine.TryTurn(Direction.Down);
            engine.Tick();
            engine.TryTurn(Direction.Left);
            engine.Tick();
            engine.TryTurn(Direction.Up);
            TickResult result = engine.Tick();

            Check("своё тело: HitSelf", result == TickResult.HitSelf);
            Check("своё тело: GameOver", engine.State == GameState.GameOver);
        }

        private static void FoodPlacement()
        {
            bool valid = true;
            for (int seed = 1; seed <= 30; seed++)
            {
                GameEngine engine = CreateEngine(seed);
                engine.Start();
                if (!engine.Food.HasValue)
                {
                    valid = false;
                    break;
                }

                if (engine.Snake.Contains(engine.Food.Value) || engine.Board.IsObstacle(engine.Food.Value))
                {
                    valid = false;
                    break;
                }
            }

            Check("еда не попадает на змейку/препятствия", valid);
        }

        private static void SpeedProgression()
        {
            GameEngine engine = CreateEngine(8);
            engine.Start();
            int startDelay = engine.FrameDelayMs;

            for (int i = 0; i < 5; i++)
            {
                engine.ForceFood(engine.Head + Direction.Right.Delta());
                engine.Tick();
            }

            Check("уровень вырос до 2", engine.Level == 2);
            Check("скорость выросла", engine.FrameDelayMs < startDelay);
            Check("длина 8 после 5 приёмов пищи", engine.Length == 8);
        }

        private static GameEngine CreateEngine(int seed)
        {
            GameSettings settings = new GameSettings();
            settings.BoardWidth = 30;
            settings.BoardHeight = 20;
            return new GameEngine(settings, new Random(seed));
        }

        private static void Check(string name, bool condition)
        {
            _total++;
            if (!condition)
            {
                _failed++;
            }

            Console.WriteLine((condition ? "  [OK]   " : "  [FAIL] ") + name);
        }
    }
}
