using System;
using System.Runtime.InteropServices;

namespace Snake.Presentation
{
    /// <summary>
    /// Включает обработку ANSI-последовательностей в Windows-консоли.
    /// На Unix ANSI работает из коробки; если включить не удалось — рендер пойдёт без цвета.
    /// </summary>
    internal static class AnsiConsole
    {
        private const int StdOutputHandle = -11;
        private const uint EnableVirtualTerminalProcessing = 0x0004;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        /// <summary>true — можно писать 24-битные ANSI-цвета.</summary>
        public static bool TryEnable()
        {
            if (Console.IsOutputRedirected)
            {
                return false;
            }

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Linux/macOS: VT включён всегда.
                return true;
            }

            try
            {
                IntPtr handle = GetStdHandle(StdOutputHandle);
                if (handle == IntPtr.Zero || handle == new IntPtr(-1))
                {
                    return false;
                }

                uint mode;
                if (!GetConsoleMode(handle, out mode))
                {
                    return false;
                }

                if ((mode & EnableVirtualTerminalProcessing) != 0)
                {
                    return true;
                }

                return SetConsoleMode(handle, mode | EnableVirtualTerminalProcessing);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
