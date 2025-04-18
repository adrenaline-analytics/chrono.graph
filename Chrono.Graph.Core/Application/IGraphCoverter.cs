using Chrono.Graph.Core.Domain;

namespace Chrono.Graph.Core.Application
{
    public interface IGraphCoverter
    {
        GraphEdgeDetails Convert<T>(T input);

    }
}
