using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Snake.Application
{
    public class ApiWorker : IApiWorker
    {
        private readonly ILogger<ApiWorker> _logger;

        public ApiWorker(ILogger<ApiWorker> logger)
        {
            _logger = logger;
        }

        public async Task<bool> Ping(string ucUrl)
        {
            try
            {
                var uri = new Uri(ucUrl);
                var path = $"{uri.Scheme}{Uri.SchemeDelimiter}{uri.Authority}";

                var request = new HttpRequestMessage(HttpMethod.Head, path);
                var handler = new HttpClientHandler();

                handler.ServerCertificateCustomValidationCallback +=
                    (sender, certificate, chain, errors) => { return true; };
                var client = new HttpClient(handler);

                var response = await client.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }

        public async Task HandleUnsuccessfulResponse(HttpResponseMessage response, string url)
        {
            var statusCode = response.StatusCode;
            string errorContent = null;

            try
            {
                // Пытаемся прочитать тело ошибки
                errorContent = await response.Content.ReadAsStringAsync();
            }
            catch
            {
                errorContent = "Не удалось прочитать тело ошибки";
            }

            // Создаем информативное исключение с деталями ошибки
            var errorMessage =
                $"HTTP запрос завершился с ошибкой: URL: {url}, Status Code: {(int)statusCode} ({statusCode}), Reason: {response.ReasonPhrase}, Error Content: {errorContent}";

            // Обработка специфичных HTTP ошибок
            switch ((int)statusCode)
            {
                case 400:
                    throw new HttpRequestException($"Некорректный запрос.  {errorMessage}");
                case 401:
                    throw new UnauthorizedAccessException($"Неавторизованный доступ к API.  {errorMessage}");
                case 403:
                    throw new UnauthorizedAccessException($"Доступ запрещен.  {errorMessage}");
                case 404:
                    throw new HttpRequestException($"Ресурс не найден.  {errorMessage}");
                case 408:
                    throw new TimeoutException($"Таймаут запроса. {errorMessage}");
                case 409:
                    throw new Exception($"Конфликт данных. {errorMessage}");
                case 429:
                    throw new HttpRequestException($"Слишком много запросов. {errorMessage}");

                case 500:
                    throw new ApplicationException($"Внутренняя ошибка сервера. {errorMessage}");
                case 501:
                    throw new ApplicationException($"Функционал не реализован на сервере. {errorMessage}");
                case 502:
                    throw new HttpRequestException($"Ошибка шлюза (Bad Gateway). {errorMessage}");
                case 503:
                    throw new ApplicationException($"Сервис недоступен. {errorMessage}");
                case 504:
                    throw new TimeoutException($"Таймаут шлюза. {errorMessage}");

                default:
                    throw new HttpRequestException(errorMessage)
                    {
                        Data =
                        {
                            ["StatusCode"] = (int)statusCode,
                            ["Url"] = url,
                            ["Content"] = errorContent
                        }
                    };
            }
        }
    }
}