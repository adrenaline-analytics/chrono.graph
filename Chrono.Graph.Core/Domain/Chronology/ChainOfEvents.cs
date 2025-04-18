namespace Chrono.Graph.Core.Domain.Chronology
{
    public class ChainOfEvents : ChainOfEvents<float, float> { }
    public class ChainOfEvents<T> : ChainOfEvents<T, T> { }
    public class ChainOfEvents<IO, T> : Event<IO, T>
    {
        public IO Input { get; set; }
        public T Target { get; set; }
        public IList<LinkedEvent<IO, T>> Origin { get; set; } = [];
    }

}
