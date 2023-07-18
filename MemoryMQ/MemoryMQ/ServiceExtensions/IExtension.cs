using Microsoft.Extensions.DependencyInjection;

namespace MemoryMQ.ServiceExtensions;

public interface IExtension
{
    void AddServices(IServiceCollection serviceCollection);
}