using System;

namespace Snake.Domain
{
    public class NotFoundException : Exception
    {
        public NotFoundException(string name, object key)
            : base($"Объект \"{name}\" ({key}) не найден.") { }
    }
}
