using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Snake.Application.Interfaces;
using Snake.Listener;
using Snake.Persistence.Data;

namespace Cert.Nats.Listener
{
    public class Program
    {
        private static ILogger<Program> _logger;
        private static CancellationTokenSource _cancellationTokenSource;
        private static bool _isRunning = true;
        
        public static async Task Main(string[] args)
        {
            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                Console.Title = @"Snake - Linux Console";
                Console.CursorVisible = false;

                Console.WriteLine(@"╔══════════════════════════════════════════════════════╗");
                Console.WriteLine(@"║            Snake Console Application         ║");
                Console.WriteLine(@"╚══════════════════════════════════════════════════════╝");
                Console.WriteLine();

                // Создаем хост
                var host = CreateHostBuilder(args).Build();

                // Получаем сервисы
                //var messageQueuesQueryRepository = host.Services.GetRequiredService<IMessageQueuesQueryRepository>();
                //var messageQueuesCommandRepository = host.Services.GetRequiredService<IMessageQueuesCommandRepository>();
                //var natsService = host.Services.GetRequiredService<INatsService>();
                //var messageProcessor = host.Services.GetRequiredService<IMessageProcessor>();
                //var config = host.Services.GetRequiredService<NatsConfig>();

                // Получаем логгер для программы
                _logger = host.Services.GetRequiredService<ILogger<Program>>();

                // Настройка обработки Ctrl+C
                SetupConsoleCancellation();

                // Запуск основного цикла
            }
            catch (Exception ex)
            {
                _logger?.LogCritical(ex, "Fatal error occurred");
                Console.WriteLine(@$"FATAL ERROR: {ex.Message}");
                Environment.Exit(1);
            }
            finally
            {
                Console.CursorVisible = true;
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Создание хоста с конфигурацией
        /// </summary>
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.SetBasePath(Directory.GetCurrentDirectory());
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                    config.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json",
                        optional: true);
                    config.AddEnvironmentVariables();
                    config.AddCommandLine(args);

                    // Проверяем наличие конфигурационного файла
                    var configPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
                    if (!File.Exists(configPath))
                    {
                        Console.WriteLine(@"WARNING: appsettings.json not found. Using default configuration.");
                    }
                })
                .ConfigureServices((context, services) =>
                {
                    //var connectionString = context.Configuration.GetConnectionString("Snake");
                    //services.AddDbContext<SnakeDbContext>(options =>
                    //{
                    //    options.UseNpgsql(connectionString);
                    //});
                    
                    //services.AddScoped<ISnakeDbContext>(provider => provider.GetService<SnakeDbContext>());

                    // ═══════════════════════════════════════════════════════
                    // РЕПОЗИТОРИИ (Command)
                    // ═══════════════════════════════════════════════════════
                    
                    // ═══════════════════════════════════════════════════════
                    // РЕПОЗИТОРИИ (Query)
                    // ═══════════════════════════════════════════════════════
                    
                    // ═══════════════════════════════════════════════════════
                    // СЕРВИСЫ
                    // ═══════════════════════════════════════════════════════
                    
                    // ═══════════════════════════════════════════════════════
                    // Фабрики
                    // ═══════════════════════════════════════════════════════
                    
                    // ═══════════════════════════════════════════════════════
                    // Сервис обработки сообщений
                    // ═══════════════════════════════════════════════════════
                    
                    // Фоновый сервис для обработки сообщений (опционально)
                    
                    // Логирование с настройкой для Linux консоли
                    services.AddLogging(logging =>
                    {
                        logging.ClearProviders();

                        // Консольный провайдер с настройками для Linux
                        logging.AddConsole(options =>
                        {
                            options.IncludeScopes = true;
                            options.TimestampFormat = "HH:mm:ss ";
                            options.LogToStandardErrorThreshold = LogLevel.Error;
                        });

                        var date = DateTime.Now;

                        // Файловый лог для Linux
                        logging.AddFile($"logs/snake-listener-{date.ToShortDateString()}.log",
                            isJson: false,
                            minimumLevel: LogLevel.Information,
                            fileSizeLimitBytes: 10_485_760); // 10 MB

                        logging.SetMinimumLevel(LogLevel.Information);
                    });
                })
                .UseConsoleLifetime(options => { options.SuppressStatusMessages = true; });

        /// <summary>
        /// Настройка обработки прерываний
        /// </summary>
        private static void SetupConsoleCancellation()
        {
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                _logger?.LogInformation("SIGINT (Ctrl+C) received. Shutting down gracefully...");
                Console.WriteLine("\n\u001b[33m[WARN] Received SIGINT. Shutting down gracefully...\u001b[0m");

                _isRunning = false;
                eventArgs.Cancel = true; // Предотвращаем немедленное завершение

                // Запускаем грациозное завершение в отдельном потоке
                Task.Run(async () =>
                {
                    await Task.Delay(100); // Даем время для обработки
                    _cancellationTokenSource.Cancel();
                });
            };

            // Для Linux также обрабатываем SIGTERM
            AppDomain.CurrentDomain.ProcessExit += (sender, eventArgs) =>
            {
                if (!_cancellationTokenSource.IsCancellationRequested)
                {
                    _logger?.LogInformation("SIGTERM received. Shutting down...");
                    Console.WriteLine("\n\u001b[33m[WARN] Received SIGTERM. Shutting down...\u001b[0m");
                    _cancellationTokenSource.Cancel();
                }
            };
        }
    }
}
