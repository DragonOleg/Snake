using System;
using System.Diagnostics;
using System.Threading;
using Snake.Application.Abstractions;
using Snake.Domain;

namespace Snake.Application
{
    /// <summary>
    /// Игровой цикл: опрос ввода, тики по таймеру, отрисовка кадра и запись результата партии.
    /// Запись идёт через порт <see cref="IGameResultSink"/> — ядро не знает про PostgreSQL.
    /// </summary>
    public sealed class GameApp
    {
        private readonly GameEngine _engine;
        private readonly ConsoleRenderer _renderer;
        private readonly IGameResultSink _sink;
        private int _highScore;
        private DateTime _gameStartedAtUtc;
        private bool _resultSaved;
        private bool _saveErrorReported;
        private int _savedGames;

        public GameApp(
            GameEngine engine,
            ConsoleRenderer renderer,
            IGameResultSink sink = null,
            int initialHighScore = 0)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
            _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
            _sink = sink;
            _highScore = initialHighScore;
        }

        /// <summary>Рекорд сессии с учётом загруженного из БД.</summary>
        public int HighScore
        {
            get { return _highScore; }
        }

        /// <summary>Сколько партий удалось записать в БД за этот запуск.</summary>
        public int SavedGames
        {
            get { return _savedGames; }
        }

        public int Run()
        {
            _engine.Start();
            BeginRun();
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
                    BeginRun();
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

                        SaveFinishedRun();
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

        private void BeginRun()
        {
            _gameStartedAtUtc = DateTime.UtcNow;
            _resultSaved = false;
        }

        /// <summary>Пишет результат, если партия только что закончилась (поражение или победа).</summary>
        private void SaveFinishedRun()
        {
            if (_sink == null || _resultSaved)
            {
                return;
            }

            if (_engine.State != GameState.GameOver && _engine.State != GameState.Won)
            {
                return;
            }

            _resultSaved = true;

            DateTime now = DateTime.UtcNow;
            GameRunResult result = new GameRunResult
            {
                FinishedAtUtc = now,
                Score = _engine.Score,
                Length = _engine.Length,
                Level = _engine.Level,
                Eaten = _engine.Eaten,
                DurationMs = (int)Math.Min(int.MaxValue, (now - _gameStartedAtUtc).TotalMilliseconds),
                BoardWidth = _engine.Board.Width,
                BoardHeight = _engine.Board.Height,
                Outcome = _engine.State,
                StatusMessage = _engine.StatusMessage
            };

            try
            {
                _sink.Save(result);
                _savedGames++;
            }
            catch (Exception ex)
            {
                // Не роняем игру из-за БД: сообщаем один раз и продолжаем.
                if (!_saveErrorReported)
                {
                    _saveErrorReported = true;
                    Console.Error.WriteLine("[БД] Результат не сохранён: " + ex.Message);
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
            if (_sink != null)
            {
                Console.WriteLine("Партий записано в БД за запуск: " + _savedGames);
            }

            return 0;
        }
    }
}
