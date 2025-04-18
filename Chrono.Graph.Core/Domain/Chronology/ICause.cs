namespace Chrono.Graph.Core.Domain.Chronology
{
    public interface ICause : ICause<float, float> { }
    public interface ICause<T> : ICause<T, T> { }
    public interface ICause<IO, T>
    {
        IList<LinkedEvent<IO, T>> Next { get; set; }
    }
}
