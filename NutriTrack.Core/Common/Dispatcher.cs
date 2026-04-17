namespace NutriTrack.Core.Common;

public class Dispatcher
{
    private readonly IServiceProvider _services;

    public Dispatcher(IServiceProvider services) => _services = services;

    public Task<TResponse> Send<TResponse>(
        IRequest<TResponse> request,
        CancellationToken ct = default)
    {

        var handlerType = typeof(IRequestHandler<,>)
            .MakeGenericType(request.GetType(), typeof(TResponse));

        var handler = _services.GetRequiredService(handlerType);

        var behaviorType = typeof(IPipelineBehavior<,>)
            .MakeGenericType(request.GetType(), typeof(TResponse));

        var behaviors = (_services.GetServices(behaviorType) as IEnumerable<object>)!
            .Reverse()
            .ToList();

        Func<Task<TResponse>> pipeline = () =>
        {
            var handleMethod = handlerType.GetMethod(nameof(IRequestHandler<IRequest<TResponse>, TResponse>.Handle))!;
            return (Task<TResponse>)handleMethod.Invoke(handler, [request, ct])!;
        };

        foreach (var behavior in behaviors)
        {
            var next = pipeline;
            var b = behavior;
            pipeline = () =>
            {
                var handleMethod = b.GetType().GetMethod(nameof(IPipelineBehavior<IRequest<TResponse>, TResponse>.Handle))!;
                return (Task<TResponse>)handleMethod.Invoke(b, [request, next, ct])!;
            };
        }

        return pipeline();
    }

    public Task Send(IRequest request, CancellationToken ct = default)
        => Send<Unit>(request, ct);
}