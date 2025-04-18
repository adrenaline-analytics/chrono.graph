using Chrono.Graph.Core.Notations;

namespace Chrono.Graph.Core.Domain.Chronology
{
    public class DistinctEvent : DistinctEvent<float, float>
    {
        public DistinctEvent() : base() { }
        public DistinctEvent(float result) : base(result) { }
        public DistinctEvent(string behaviorKey, float io) : base(behaviorKey, io) { }
        public DistinctEvent(string behaviorKey, float io, float target) : base(behaviorKey, io, target) { }
        public DistinctEvent(Func<float> action) : base(action) { }
        public DistinctEvent(Func<float, float> action) : base(action) { }
        public DistinctEvent(Func<float, float, float> action) : base(action) { }
    }
    public class DistinctEvent<T> : DistinctEvent<T, T>
    {
        public DistinctEvent() : base() { }
        public DistinctEvent(T result) : base(result) { }
        public DistinctEvent(string behaviorKey, T io) : base(behaviorKey, io) { }
        public DistinctEvent(string behaviorKey, T io, T target) : base(behaviorKey, io, target) { }
        public DistinctEvent(Func<T> action) : base(action) { }
        public DistinctEvent(Func<T, T> action) : base(action) { }
        public DistinctEvent(Func<T, T, T> action) : base(action) { }
    }
    public class DistinctEvent<IO, T> : Event<IO, T>
    {
        public DistinctEvent() : base() { }
        public DistinctEvent(IO result) : base(result) { }
        public DistinctEvent(string behaviorKey, IO io) : base(behaviorKey, io) { }
        public DistinctEvent(string behaviorKey, IO io, T target) : base(behaviorKey, io, target) { }
        public DistinctEvent(Func<IO> action) : base(action) { }
        public DistinctEvent(Func<T, IO> action) : base(action) { }
        public DistinctEvent(Func<IO, T, IO> action) : base(action) { }
        public DistinctEvent(Func<long, IO, T, IO> action) : base(action) { }
        public DistinctEvent(Func<long, TimeController<IO, T>, IO, T, IO> action) : base(action) { }

        [GraphIdentifier]
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public DateTime? Created { get; set; }
        public DateTime? Updated { get; set; }
    }
}
