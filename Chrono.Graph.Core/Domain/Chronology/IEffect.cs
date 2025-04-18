namespace Chrono.Graph.Core.Domain.Chronology
{
    public interface IEffect : IEffect<float, float> { }
    public interface IEffect<T> : IEffect<T, T> { }
    public interface IEffect<IO, T>
    {
        IList<LinkedEvent<IO, T>> Prior { get; set; }
    }
}
