using Serilog;
using MediatR;
using System.Threading.Tasks;
using System.Threading;

namespace Snake.Application.Common.Behaviors
{
    public class LoggingBehavior<TRequest, TResponse>
        : IPipelineBehavior<TRequest, TResponse> where TRequest
        : IRequest<TResponse>
    {
        public LoggingBehavior()
        {
        }

        public async Task<TResponse> Handle(TRequest request,
            CancellationToken cancellationToken,
            RequestHandlerDelegate<TResponse> next)
        {
            var requestName = typeof(TRequest).Name;
            Log.Information("Request: {Name} {@Request}",
                requestName, request);
            var response = await next();
            return response;
        }
    }
}