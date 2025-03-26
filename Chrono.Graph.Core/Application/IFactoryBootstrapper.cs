using Chrono.Graph.Core.Domain;

namespace Chrono.Graph.Core.Application
{
    public interface IFactoryBootstrapper
    {
        Statement BootstrapWithMatch<T>();
        Statement BootstrapWithMerge<T>(T thing);
    }

}
