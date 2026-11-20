using System;

namespace Snake.Domain
{
    public class UnauthorizedException : Exception
    {
        public UnauthorizedException(string name) :
            base($"Пользователь {name} не авторизован.") { }
    }
}
