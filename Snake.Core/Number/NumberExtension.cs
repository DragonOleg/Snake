namespace Snake.Core
{
    public static class NumberExtension
    {
        public static bool ToBool(this int numInt)
        {
            if (numInt == 1)
            {
                return true;
            }

            return false;
        }

        public static bool ToBool(this int? numNullInt)
        {
            if (!numNullInt.HasValue)
            {
                return false;
            }

            return numNullInt.Value.ToBool();
        }
    }
}
