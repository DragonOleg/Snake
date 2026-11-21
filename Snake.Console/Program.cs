using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Snake.Application;
using Snake.Application.Abstractions;
using Snake.Domain;
using Snake.Listener;
using Snake.Persistence;
using Snake.Presentation;

namespace Cert.Nats.Listener
{
    public class Program
    {
        private static ILogger<Program> _logger;
        private static CancellationTokenSource _cancellationTokenSource;

        public static int Main(string[] args)
        {
            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                Console.Title = @"Snake - Linux Console";
                Console.WriteLine(@"╔══════════════════════════════════════════════════════╗");
                Console.WriteLine(@"║            Snake Console Application                 ║");
                Console.WriteLine(@"╚══════════════════════════════════════════════════════╝");
                Console.WriteLine();

                IHost host = CreateHostBuilder(args).Build();
                _logger = host.Services.GetRequiredService<ILogger<Program>>();
                SetupConsoleCancellation();
                Console.OutputEncoding = Encoding.UTF8;

                if (args.Length > 0)
                {
                    string mode = args[0];

                    if (mode == "--selftest")
                    {
                        return SelfTest.Run();
                    }

                    if (mode == "--demo")
                    {
                        int ticks = args.Length > 1 ? ParseInt(args[1], 300) : 300;
                        int seed = args.Length > 2 ? ParseInt(args[2], Environment.TickCount) : Environment.TickCount;
                        return DemoRunner.Run(ticks, seed);
                    }

                    if (mode == "--stats" || mode == "--scoreboard")
                    {
                        return ShowStats(host);
                    }

                    if (mode == "--help" || mode == "-h" || mode == "/?")
                    {
                        PrintHelp();
                        return 0;
                    }

                    Console.WriteLine("Неизвестный режим: " + mode);
                    PrintHelp();
                    return 2;
                }

                if (Console.IsInputRedirected || Console.IsOutputRedirected)
                {
                    Console.WriteLine("Ввод/вывод перенаправлен — интерактивная игра недоступна.");
                    Console.WriteLine("Запускаю демо-режим бота на 300 тиков. Для игры запустите приложение в обычном терминале.");
                    Console.WriteLine();
                    return DemoRunner.Run(300, Environment.TickCount);
                }

                GameSettings settings = CreateSettings();
                GameEngine engine = new GameEngine(settings, new Random());
                ConsoleRenderer renderer = new ConsoleRenderer(settings);

                IGameResultSink sink;
                IGameResultReader reader;
                InitializeDatabase(host.Services, out sink, out reader);

                if (reader != null)
                {
                    bool hasGames;
                    if (PrintScoreboard(reader, 3, 5, out hasGames) && hasGames)
                    {
                        WaitForStart();
                    }
                }

                int bestScore = 0;
                if (reader != null)
                {
                    try
                    {
                        bestScore = reader.GetProgress().BestScore;
                    }
                    catch (Exception ex)
                    {
                        WarnDatabase(ex);
                    }
                }

                GameApp app = new GameApp(engine, renderer, sink, bestScore);

                try
                {
                    Console.CursorVisible = false;
                    return app.Run();
                }
                finally
                {
                    TryRestoreConsole();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogCritical(ex, "Fatal error occurred");
                Console.WriteLine(@"FATAL ERROR: " + ex.Message);
                Environment.Exit(1);
            }
            finally
            {
                TryRestoreConsole();
            }

            return 0;
        }

        /// <summary>Создание хоста с конфигурацией</summary>
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    // Конфиг лежит рядом со сборкой, а не в текущем каталоге:
                    // иначе `dotnet run --project Snake.Console` из корня репозитория его не находит.
                    string baseDirectory = AppContext.BaseDirectory;
                    config.SetBasePath(baseDirectory);
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                    config.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json",
                        optional: true);
                    config.AddEnvironmentVariables();
                    config.AddCommandLine(args);

                    var configPath = Path.Combine(baseDirectory, "appsettings.json");
                    if (!File.Exists(configPath))
                    {
                        Console.WriteLine(@"WARNING: appsettings.json not found. Using default configuration.");
                    }
                })
                .ConfigureServices((context, services) =>
                {
                    // PostgreSQL рядом с проектом: строка подключения "Snake" в appsettings.json.
                    services.AddPersistence(context.Configuration);

                    services.AddLogging(logging =>
                    {
                        logging.ClearProviders();

                        logging.AddConsole(options =>
                        {
                            options.IncludeScopes = true;
                            options.TimestampFormat = "HH:mm:ss ";
                            options.LogToStandardErrorThreshold = LogLevel.Error;
                        });

                        var date = DateTime.Now;
                        string logsDirectory = Path.Combine(AppContext.BaseDirectory, "logs");

                        logging.AddFile(Path.Combine(logsDirectory, $"snake-listener-{date.ToShortDateString()}.log"),
                            isJson: false,
                            minimumLevel: LogLevel.Information,
                            fileSizeLimitBytes: 10_485_760); // 10 MB

                        logging.SetMinimumLevel(LogLevel.Information);
                    });
                })
                .UseConsoleLifetime(options => { options.SuppressStatusMessages = true; });

        /// <summary>Настройка обработки прерываний</summary>
        private static void SetupConsoleCancellation()
        {
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                _logger?.LogInformation("SIGINT (Ctrl+C) received. Shutting down gracefully...");
                Console.WriteLine("\n\u001b[33m[WARN] Received SIGINT. Shutting down gracefully...\u001b[0m");

                eventArgs.Cancel = true;

                Task.Run(async () =>
                {
                    await Task.Delay(100);
                    _cancellationTokenSource.Cancel();
                });
            };

            AppDomain.CurrentDomain.ProcessExit += (sender, eventArgs) =>
            {
                if (!_cancellationTokenSource.IsCancellationRequested)
                {
                    _logger?.LogInformation("SIGTERM received. Shutting down...");
                    _cancellationTokenSource.Cancel();
                }
            };
        }

        /// <summary>Подключает БД: схема, запись и чтение результатов. При ошибке игра продолжается без БД.</summary>
        private static void InitializeDatabase(
            IServiceProvider services,
            out IGameResultSink sink,
            out IGameResultReader reader)
        {
            sink = null;
            reader = null;

            try
            {
                services.GetRequiredService<IDbInitializer>().Initialize();
                sink = services.GetRequiredService<IGameResultSink>();
                reader = services.GetRequiredService<IGameResultReader>();
            }
            catch (Exception ex)
            {
                Console.WriteLine("[БД] PostgreSQL недоступна — результаты не сохранятся.");
                Console.WriteLine("[БД] " + FirstLine(ex.Message));
                Console.WriteLine("[БД] Запустите: Data\\db.ps1 start");
                Console.WriteLine();
                _logger?.LogWarning(ex, "Database unavailable");
            }
        }

        private static int ShowStats(IHost host)
        {
            IGameResultSink sink;
            IGameResultReader reader;
            InitializeDatabase(host.Services, out sink, out reader);

            if (reader == null)
            {
                return 1;
            }

            bool hasGames;
            return PrintScoreboard(reader, 5, 10, out hasGames) ? 0 : 1;
        }

        private static bool PrintScoreboard(IGameResultReader reader, int bestCount, int recentCount, out bool hasGames)
        {
            hasGames = false;

            try
            {
                GameProgress progress = reader.GetProgress();
                hasGames = progress.HasGames;
                IReadOnlyList<GameRunResult> best = reader.GetBest(bestCount);
                IReadOnlyList<GameRunResult> recent = reader.GetRecent(recentCount);
                ScoreboardView.Print(progress, best, recent);
                return true;
            }
            catch (Exception ex)
            {
                WarnDatabase(ex);
                return false;
            }
        }

        private static void WarnDatabase(Exception ex)
        {
            Console.WriteLine("[БД] Ошибка чтения результатов: " + FirstLine(ex.Message));
        }

        /// <summary>
        /// Даёт прочитать экран прогресса: первый кадр игры очищает консоль, поэтому без паузы
        /// сохранённые результаты просто мелькают.
        /// </summary>
        private static void WaitForStart()
        {
            try
            {
                Console.WriteLine("Нажмите любую клавишу, чтобы начать…");
                Console.ReadKey(true);
            }
            catch (Exception)
            {
                // Консоль без ввода — начинаем сразу.
            }
        }

        private static string FirstLine(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            int index = text.IndexOfAny(new[] { '\r', '\n' });
            return index < 0 ? text : text.Substring(0, index);
        }

        private static void TryRestoreConsole()
        {
            try
            {
                Console.CursorVisible = true;
            }
            catch (Exception)
            {
                // В неинтерактивной среде (перенаправленный вывод) курсор недоступен.
            }

            Console.ResetColor();
        }

        private static GameSettings CreateSettings()
        {
            int width = 30;
            int height = 20;

            try
            {
                if (!Console.IsOutputRedirected)
                {
                    width = Math.Min(30, Math.Max(16, (Console.WindowWidth - 2) / 2));
                    height = Math.Min(20, Math.Max(10, Console.WindowHeight - 8));
                }
            }
            catch (System.IO.IOException)
            {
                width = 30;
                height = 20;
            }
            catch (PlatformNotSupportedException)
            {
                width = 30;
                height = 20;
            }

            GameSettings settings = new GameSettings();
            settings.BoardWidth = width;
            settings.BoardHeight = height;
            return settings;
        }

        private static int ParseInt(string text, int fallback)
        {
            int value;
            return int.TryParse(text, out value) ? value : fallback;
        }

        private static void PrintHelp()
        {
            Console.WriteLine("ЗМЕЙКА — консольная классика");
            Console.WriteLine();
            Console.WriteLine("Использование:");
            Console.WriteLine("  dotnet run                 — играть (стрелки/WASD, P — пауза, R — заново, Q — выход)");
            Console.WriteLine("  dotnet run -- --stats      — прогресс, лучшие и последние результаты из БД");
            Console.WriteLine("  dotnet run -- --selftest   — самопроверка игровой логики");
            Console.WriteLine("  dotnet run -- --demo [тики] [seed] — демо-режим бота");
        }
    }
}
