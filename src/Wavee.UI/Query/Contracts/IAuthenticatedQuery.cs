using Mediator;

namespace Wavee.UI.Query.Contracts;

public interface IAuthenticatedQuery<out T> : IQuery<T>
{
    ProfileContext Profile { get; internal set; }
}