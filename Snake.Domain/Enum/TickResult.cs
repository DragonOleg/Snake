namespace Snake.Domain
{
    public enum TickResult
    {
        Moved,
        Ate,
        HitWall,
        HitObstacle,
        HitSelf,
        Won
    }
}