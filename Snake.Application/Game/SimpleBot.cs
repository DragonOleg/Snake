using System;
using Snake.Core;
using Snake.Domain;

namespace Snake.Application
{
    /// <summary>Простой жадный бот: идёт к еде, избегая неизбежной смерти. Нужен для демо-режима.</summary>
    public sealed class SimpleBot
    {
        private static readonly Direction[] AllDirections =
        {
            Direction.Up,
            Direction.Down,
            Direction.Left,
            Direction.Right
        };

        public Direction Choose(GameEngine engine)
        {
            Point target = engine.Food ?? engine.Head;
            Direction best = engine.Direction;
            int bestDistance = int.MaxValue;
            bool found = false;

            foreach (Direction candidate in AllDirections)
            {
                if (candidate.IsOpposite(engine.Direction))
                {
                    continue;
                }

                if (!engine.IsMoveSafe(candidate))
                {
                    continue;
                }

                Point next = engine.Head + candidate.Delta();
                int distance = Math.Abs(next.X - target.X) + Math.Abs(next.Y - target.Y);
                if (!found || distance < bestDistance)
                {
                    bestDistance = distance;
                    best = candidate;
                    found = true;
                }
            }

            if (found)
            {
                return best;
            }

            foreach (Direction candidate in AllDirections)
            {
                if (!candidate.IsOpposite(engine.Direction) && engine.IsMoveSafe(candidate))
                {
                    return candidate;
                }
            }

            return engine.Direction;
        }
    }
}
