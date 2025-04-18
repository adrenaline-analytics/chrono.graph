namespace Chrono.Graph.Core.Domain.Chronology
{
    public class OriginEvent : OriginEvent<float, float>, ICause
    {
        public OriginEvent() : base() { }
        public OriginEvent(float result) : base(result) { }
        public OriginEvent(string behaviorKey, float io) : base(behaviorKey, io) { }
        public OriginEvent(string behaviorKey, float io, float target) : base(behaviorKey, io, target) { }
        public OriginEvent(Func<float> action) : base(action) { }
        public OriginEvent(Func<float, float> action) : base(action) { }
        public OriginEvent(Func<float, float, float> action) : base(action) { }
    }
    public class OriginEvent<T> : OriginEvent<T, T>, ICause<T>
    {
        public OriginEvent() : base() { }
        public OriginEvent(string behaviorKey, T io) : base(behaviorKey, io) { }
        public OriginEvent(string behaviorKey, T io, T target) : base(behaviorKey, io, target) { }
        public OriginEvent(T result) : base(result) { }
        public OriginEvent(Func<T> action) : base(action) { }
        public OriginEvent(Func<T, T> action) : base(action) { }
        public OriginEvent(Func<T, T, T> action) : base(action) { }
    }
    public class OriginEvent<IO, T> : Event<IO, T>, ICause<IO, T>
    {
        public OriginEvent() : base() { }
        public OriginEvent(string behaviorKey, IO io) : base(behaviorKey, io) { }
        public OriginEvent(string behaviorKey, IO io, T target) : base(behaviorKey, io, target) { }
        public OriginEvent(IO result) : base(result) { }
        public OriginEvent(Func<IO> action) : base(action) { }
        public OriginEvent(Func<T, IO> action) : base(action) { }
        public OriginEvent(Func<IO, T, IO> action) : base(action) { }
        public OriginEvent(Func<long, IO, T, IO> action) : base(action) { }
        public OriginEvent(Func<long, TimeController<IO, T>, IO, T, IO> action) : base(action) { }
        public IList<LinkedEvent<IO, T>> Next { get; set; } = [];
    }
}
