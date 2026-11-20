using System.Net.Http;
using System.Threading.Tasks;

namespace Snake.Application
{
    public interface IApiWorker
    {
        Task<bool> Ping(string ucUrl);
        Task HandleUnsuccessfulResponse(HttpResponseMessage response, string url);
    }
}