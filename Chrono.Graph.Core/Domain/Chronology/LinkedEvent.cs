namespace Chrono.Graph.Core.Domain.Chronology
{

    public class LinkedEvent : LinkedEvent<float, float>
    {
        public LinkedEvent() { }
        public LinkedEvent(float result) : base(result) { }
        public LinkedEvent(string behaviorKey, float io) : base(behaviorKey, io) { }
        public LinkedEvent(string behaviorKey, float io, float target) : base(behaviorKey, io, target) { }
        public LinkedEvent(Func<float> action) : base(action) { }
        public LinkedEvent(Func<float, float> action) : base(action) { }
        public LinkedEvent(Func<float, float, float> action) : base(action) { }
    }
    public class LinkedEvent<T> : LinkedEvent<T, T>
    {
        public LinkedEvent() { }
        public LinkedEvent(T result) : base(result) { }
        public LinkedEvent(string behaviorKey, T io) : base(behaviorKey, io) { }
        public LinkedEvent(string behaviorKey, T io, T target) : base(behaviorKey, io, target) { }
        public LinkedEvent(Func<T> action) : base(action) { }
        public LinkedEvent(Func<T, T> action) : base(action) { }
        public LinkedEvent(Func<T, T, T> action) : base(action) { }
    }
    public class LinkedEvent<IO, T> : OriginEvent<IO, T>, ICause<IO, T>, IEffect<IO, T>
    {
        public LinkedEvent() { }
        public LinkedEvent(string behaviorKey, IO io) : base(behaviorKey, io) { }
        public LinkedEvent(string behaviorKey, IO io, T target) : base(behaviorKey, io, target) { }
        public LinkedEvent(IO result) : base(result) { }
        public LinkedEvent(Func<IO> action) : base(action) { }
        public LinkedEvent(Func<T, IO> action) : base(action) { }
        public LinkedEvent(Func<IO, T, IO> action) : base(action) { }
        public LinkedEvent(Func<long, IO, T, IO> action) : base(action) { }
        public LinkedEvent(Func<long, TimeController<IO, T>, IO, T, IO> action) : base(action) { }
        public IO? Input { get; set; }
        public IO? Output { get; set; }
        public IList<LinkedEvent<IO, T>> Prior { get; set; } = [];
    }
}
