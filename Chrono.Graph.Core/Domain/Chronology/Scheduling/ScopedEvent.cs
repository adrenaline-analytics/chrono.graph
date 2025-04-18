namespace Chrono.Graph.Core.Domain.Chronology.Scheduling
{
    public class ScopedEvent<S> : ScopedEvent<S, float, float>
    {
        public ScopedEvent() : base() { }
        public ScopedEvent(float result) : base(result) { }
        public ScopedEvent(Func<float> action) : base(action) { }
        public ScopedEvent(Func<float, float> action) : base(action) { }
        public ScopedEvent(Func<float, float, float> action) : base(action) { }
    }
    public class ScopedEvent<S, T> : ScopedEvent<S, T, T>
    {
        public ScopedEvent() : base() { }
        public ScopedEvent(T result) : base(result) { }
        public ScopedEvent(Func<T> action) : base(action) { }
        public ScopedEvent(Func<T, T> action) : base(action) { }
        public ScopedEvent(Func<T, T, T> action) : base(action) { }
    }
    public class ScopedEvent<S, IO, T> : DistinctEvent<IO, T>
    {
        public ScopedEvent() : base() { }
        public ScopedEvent(IO result) : base(result) { }
        public ScopedEvent(Func<IO> action) : base(action) { }
        public ScopedEvent(Func<T, IO> action) : base(action) { }
        public ScopedEvent(Func<IO, T, IO> action) : base(action) { }
        public ScopedEvent(Func<long, IO, T, IO> action) : base(action) { }
        public ScopedEvent(Func<long, TimeController<IO, T>, IO, T, IO> action) : base(action) { }
        public S? AudienceScope { get; set; }

    }
}
