using System.Collections.Generic;

namespace Snake.Domain
{
    public class BaseSnakeBody
    {
        public virtual Point Head
        {
            get;
        }

        public virtual int Length
        {
            get;
        }

        public virtual IReadOnlyList<Point> Segments
        {
            get;
        }

        public virtual IEnumerable<Point> Tail
        {
            get;
        }
    }
}