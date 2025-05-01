namespace Chrono.Graph.Core.Domain.Chronology
{
    public class ChainOfEvents : ChainOfEvents<float, float> {
        public ChainOfEvents(float input, float target, IList<LinkedEvent<float, float>> origin) : base(input, target, origin) { }
    }
    public class ChainOfEvents<T> : ChainOfEvents<T, T> {
        public ChainOfEvents(T input, T target, IList<LinkedEvent<T, T>> origin) : base(input, target, origin) { }
    }
    public class ChainOfEvents<IO, T> : Event<IO, T>
    {
        public ChainOfEvents(IO input, T target, IList<LinkedEvent<IO, T>> origin)
        {
            Input = input;
            Target = target;
            Origin = origin;
        }

        public IO Input { get; set; }
        public T Target { get; set; }
        public IList<LinkedEvent<IO, T>> Origin { get; set; } = [];
    }

}
