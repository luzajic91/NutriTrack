namespace NutriTrack.Core.Common;

public interface IRequest<TResponse> { }

public interface IRequest : IRequest<Unit> { }