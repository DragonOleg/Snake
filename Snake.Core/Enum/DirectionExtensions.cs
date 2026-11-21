using Snake.Domain;

namespace Snake.Core
{
    public static class DirectionExtensions
    {
        public static Point Delta(this Direction direction)
        {
            switch (direction)
            {
                case Direction.Up:
                    return new Point(0, -1);
                case Direction.Down:
                    return new Point(0, 1);
                case Direction.Left:
                    return new Point(-1, 0);
                case Direction.Right:
                    return new Point(1, 0);
                default:
                    return new Point(0, 0);
            }
        }

        public static Direction Opposite(this Direction direction)
        {
            switch (direction)
            {
                case Direction.Up:
                    return Direction.Down;
                case Direction.Down:
                    return Direction.Up;
                case Direction.Left:
                    return Direction.Right;
                case Direction.Right:
                    return Direction.Left;
                default:
                    return direction;
            }
        }

        public static bool IsOpposite(this Direction direction, Direction other)
        {
            return direction.Opposite() == other;
        }
    }
}