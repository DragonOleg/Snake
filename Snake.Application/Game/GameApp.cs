using System;
using System.Diagnostics;
using System.Threading;
using Snake.Domain;

namespace Snake.Application
{
    /// <summary>Игровой цикл: опрос ввода, тики по таймеру, отрисовка кадра.</summary>
    public sealed class GameApp
    {
        private readonly GameEngine _engine;
        private readonly ConsoleRenderer _renderer;
        private int _highScore;

        public GameApp(GameEngine engine, ConsoleRenderer renderer)
        {
            _engine = engine;
            _renderer = renderer;
        }

        public int Run()
        {
            _engine.Start();
            _renderer.Draw(_engine, _highScore);
            Stopwatch clock = Stopwatch.StartNew();
            long nextTickAt = clock.ElapsedMilliseconds;

            while (true)
            {
                GameCommand command = ConsoleInput.Poll();

                if (command == GameCommand.Quit)
                {
                    return Finish();
                }

                if (command == GameCommand.Restart)
                {
                    _engine.Start();
                    nextTickAt = clock.ElapsedMilliseconds;
                    _renderer.Draw(_engine, _highScore);
                    continue;
                }

                if (command == GameCommand.Pause)
                {
                    _engine.TogglePause();
                    _renderer.Draw(_engine, _highScore);
                    continue;
                }

                ApplyDirection(command);

                if (_engine.State == GameState.Running)
                {
                    long now = clock.ElapsedMilliseconds;
                    if (now >= nextTickAt)
                    {
                        _engine.Tick();
                        if (_engine.Score > _highScore)
                        {
                            _highScore = _engine.Score;
                        }

                        _renderer.Draw(_engine, _highScore);

                        nextTickAt += _engine.FrameDelayMs;
                        if (nextTickAt < now)
                        {
                            nextTickAt = now + _engine.FrameDelayMs;
                        }
                    }
                    else
                    {
                        long remaining = nextTickAt - now;
                        if (remaining > 1)
                        {
                            Thread.Sleep((int)Math.Min(remaining, 8));
                        }
                    }
                }
                else
                {
                    Thread.Sleep(20);
                }
            }
        }

        private void ApplyDirection(GameCommand command)
        {
            switch (command)
            {
                case GameCommand.Up:
                    _engine.TryTurn(Direction.Up);
                    break;
                case GameCommand.Down:
                    _engine.TryTurn(Direction.Down);
                    break;
                case GameCommand.Left:
                    _engine.TryTurn(Direction.Left);
                    break;
                case GameCommand.Right:
                    _engine.TryTurn(Direction.Right);
                    break;
                default:
                    break;
            }
        }

        private int Finish()
        {
            Console.ResetColor();
            if (!Console.IsOutputRedirected)
            {
                Console.Clear();
            }

            Console.WriteLine("Выход из игры.");
            Console.WriteLine("Очки: " + _engine.Score + ", длина: " + _engine.Length + ", уровень: " + _engine.Level + ", рекорд сессии: " + _highScore);
            return 0;
        }
    }
}
