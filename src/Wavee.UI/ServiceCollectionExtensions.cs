using Microsoft.Extensions.DependencyInjection;

namespace Wavee.UI;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWavee(this IServiceCollection coll)
    {
        return coll;
    }
}