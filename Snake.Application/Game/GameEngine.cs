using System;
using System.Collections.Generic;
using Snake.Core;
using Snake.Domain;

namespace Snake.Application
{
    /// <summary>
    /// Чистая игровая логика без привязки к консоли: детерминирована при заданном Random,
    /// поэтому её можно проверять без ввода-вывода и запускать в демо-режиме.
    /// </summary>
    public class GameEngine
    {
        private readonly GameSettings _settings;
        private readonly Random _random;
        private readonly SnakeBody _snake = new SnakeBody();

        private Direction _direction;
        private Direction? _pendingDirection;
        private Point? _food;
        private int _eaten;

        public GameEngine(GameSettings settings, Random random)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _random = random ?? throw new ArgumentNullException(nameof(random));
            _settings.Validate();

            Board = new Board(settings.BoardWidth, settings.BoardHeight);
            State = GameState.Ready;
            StatusMessage = "Готов";
        }

        public Board Board { get; }

        public SnakeBody Snake
        {
            get { return _snake; }
        }

        public Point Head
        {
            get { return _snake.Head; }
        }

        public Point? Food
        {
            get { return _food; }
        }

        public int Score { get; private set; }

        public int Eaten
        {
            get { return _eaten; }
        }

        public int Length
        {
            get { return _snake.Length; }
        }

        public int Level
        {
            get { return 1 + _eaten / _settings.FoodPerLevel; }
        }

        public int FrameDelayMs
        {
            get
            {
                int delay = _settings.BaseFrameDelayMs - (Level - 1) * _settings.SpeedStepMs;
                return Math.Max(_settings.MinFrameDelayMs, delay);
            }
        }

        public double CellsPerSecond
        {
            get { return 1000.0 / FrameDelayMs; }
        }

        public GameState State { get; private set; }

        public string StatusMessage { get; private set; }

        public Direction Direction
        {
            get { return _direction; }
        }

        public void Start()
        {
            Board.ClearObstacles();
            Point head = new Point(_settings.BoardWidth / 2, _settings.BoardHeight / 2);
            _snake.Reset(head, Direction.Right, _settings.InitialLength);
            _direction = Direction.Right;
            _pendingDirection = null;
            _food = null;
            Score = 0;
            _eaten = 0;
            State = GameState.Running;
            StatusMessage = "Вперёд!";

            if (!SpawnFood())
            {
                return;
            }
        }

        /// <summary>Ставит желаемое направление в очередь на следующий тик.</summary>
        public bool TryTurn(Direction desired)
        {
            if (State != GameState.Running)
            {
                return false;
            }

            Direction reference = _pendingDirection ?? _direction;
            if (desired == reference || desired.IsOpposite(reference))
            {
                return false;
            }

            _pendingDirection = desired;
            return true;
        }

        public void TogglePause()
        {
            if (State == GameState.Running)
            {
                State = GameState.Paused;
                StatusMessage = "Пауза";
            }
            else if (State == GameState.Paused)
            {
                State = GameState.Running;
                StatusMessage = "Продолжаем";
            }
        }

        public bool IsMoveSafe(Direction direction)
        {
            Point next = Head + direction.Delta();
            if (!Board.IsInside(next) || Board.IsObstacle(next))
            {
                return false;
            }

            bool growing = _food.HasValue && _food.Value == next;
            return !_snake.CollidesAt(next, !growing);
        }

        public TickResult Tick()
        {
            if (State != GameState.Running)
            {
                throw new InvalidOperationException("Тик допустим только в состоянии Running.");
            }

            if (_pendingDirection.HasValue)
            {
                _direction = _pendingDirection.Value;
                _pendingDirection = null;
            }

            Point next = Head + _direction.Delta();

            if (!Board.IsInside(next))
            {
                return Die(TickResult.HitWall, "Удар о стену");
            }

            if (Board.IsObstacle(next))
            {
                return Die(TickResult.HitObstacle, "Удар о препятствие");
            }

            bool growing = _food.HasValue && _food.Value == next;
            if (_snake.CollidesAt(next, !growing))
            {
                return Die(TickResult.HitSelf, "Удар о своё тело");
            }

            _snake.Move(next, growing);

            if (!growing)
            {
                return TickResult.Moved;
            }

            Score += _settings.ScorePerFood;
            _eaten++;
            StatusMessage = "+" + _settings.ScorePerFood + " очков";

            if (!SpawnFood())
            {
                return TickResult.Won;
            }

            if (_eaten % _settings.FoodPerLevel == 0)
            {
                AddObstaclesForLevel();
            }

            return TickResult.Ate;
        }

        private TickResult Die(TickResult result, string reason)
        {
            State = GameState.GameOver;
            StatusMessage = reason;
            return result;
        }

        /// <summary>Возвращает false, если свободных клеток не осталось (победа).</summary>
        private bool SpawnFood()
        {
            List<Point> free = FreeCells();
            if (free.Count == 0)
            {
                _food = null;
                State = GameState.Won;
                StatusMessage = "Победа! Поле заполнено";
                return false;
            }

            _food = free[_random.Next(free.Count)];
            return true;
        }

        private void AddObstaclesForLevel()
        {
            int target = Math.Min(_settings.MaxObstacles, (Level - 1) * _settings.ObstaclesPerLevel);
            int attempts = 0;
            while (Board.ObstacleCount < target && attempts < 400)
            {
                attempts++;
                List<Point> free = FreeCells();
                if (free.Count == 0)
                {
                    return;
                }

                Point candidate = free[_random.Next(free.Count)];
                int distance = Math.Abs(candidate.X - Head.X) + Math.Abs(candidate.Y - Head.Y);
                if (distance <= 2)
                {
                    continue;
                }

                Board.AddObstacle(candidate);
            }
        }

        private List<Point> FreeCells()
        {
            List<Point> free = new List<Point>();
            foreach (Point cell in Board.AllCells())
            {
                if (Board.IsObstacle(cell))
                {
                    continue;
                }

                if (_snake.Contains(cell))
                {
                    continue;
                }

                free.Add(cell);
            }

            return free;
        }

        internal void ForceFood(Point food)
        {
            _food = food;
        }

        internal void ForceSnake(Point head, Direction direction, int length)
        {
            _snake.Reset(head, direction, length);
            _direction = direction;
            _pendingDirection = null;
        }

        internal void ForceState(GameState state)
        {
            State = state;
        }
    }
}
