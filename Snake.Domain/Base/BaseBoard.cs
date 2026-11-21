using System.Collections.Generic;

namespace Snake.Domain
{
    /// <summary>Игровое поле: размеры и статические препятствия.</summary>
    public class BaseBoard
    {
        private readonly HashSet<Point> _obstacles = new HashSet<Point>();

        public BaseBoard(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public int Width { get; }

        public int Height { get; }

        public int CellCount
        {
            get { return Width * Height; }
        }

        public IEnumerable<Point> Obstacles
        {
            get { return _obstacles; }
        }

        public int ObstacleCount
        {
            get { return _obstacles.Count; }
        }

        public bool IsInside(Point point)
        {
            return point.X >= 0 && point.X < Width && point.Y >= 0 && point.Y < Height;
        }

        public bool IsObstacle(Point point)
        {
            return _obstacles.Contains(point);
        }

        public bool AddObstacle(Point point)
        {
            return IsInside(point) && _obstacles.Add(point);
        }

        public void ClearObstacles()
        {
            _obstacles.Clear();
        }

        public IEnumerable<Point> AllCells()
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    yield return new Point(x, y);
                }
            }
        }
    }
}
