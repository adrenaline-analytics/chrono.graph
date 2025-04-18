namespace Chrono.Graph.Core.Domain.Chronology
{
    public class TerminalEvent : TerminalEvent<float, float>
    {
        public TerminalEvent() : base() { }
        public TerminalEvent(float result) : base(result) { }
        public TerminalEvent(string behaviorKey, float io) : base(behaviorKey, io) { }
        public TerminalEvent(string behaviorKey, float io, float target) : base(behaviorKey, io, target) { }
        public TerminalEvent(Func<float> action) : base(action) { }
        public TerminalEvent(Func<float, float> action) : base(action) { }
        public TerminalEvent(Func<float, float, float> action) : base(action) { }
    }
    public class TerminalEvent<T> : TerminalEvent<T, T>, IEffect<T>
    {
        public TerminalEvent() : base() { }
        public TerminalEvent(T result) : base(result) { }
        public TerminalEvent(string behaviorKey, T io) : base(behaviorKey, io) { }
        public TerminalEvent(string behaviorKey, T io, T target) : base(behaviorKey, io, target) { }
        public TerminalEvent(Func<T> action) : base(action) { }
        public TerminalEvent(Func<T, T> action) : base(action) { }
        public TerminalEvent(Func<T, T, T> action) : base(action) { }
    }
    public class TerminalEvent<IO, T> : Event<IO, T>, IEffect<IO, T>
    {
        public TerminalEvent() : base() { }
        public TerminalEvent(IO result) : base(result) { }
        public TerminalEvent(string behaviorKey, IO io) : base(behaviorKey, io) { }
        public TerminalEvent(string behaviorKey, IO io, T target) : base(behaviorKey, io, target) { }
        public TerminalEvent(Func<IO> action) : base(action) { }
        public TerminalEvent(Func<T, IO> action) : base(action) { }
        public TerminalEvent(Func<IO, T, IO> action) : base(action) { }
        public TerminalEvent(Func<long, IO, T, IO> action) : base(action) { }
        public TerminalEvent(Func<long, TimeController<IO, T>, IO, T, IO> action) : base(action) { }
        public IList<LinkedEvent<IO, T>> Prior { get; set; } = [];
    }
}
