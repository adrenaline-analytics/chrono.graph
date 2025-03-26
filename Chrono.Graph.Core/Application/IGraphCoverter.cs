namespace Chrono.Graph.Notations.Conversion
{
    public interface IGraphCoverter
    {
        GraphEdgeDetails Convert<T>(T input);

    }
}
