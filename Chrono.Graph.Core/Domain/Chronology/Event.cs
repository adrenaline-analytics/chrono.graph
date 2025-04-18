using Chrono.Graph.Core.Constant;
using Chrono.Graph.Core.Notations;
using System.Text.Json.Serialization;

namespace Chrono.Graph.Core.Domain.Chronology
{
    public class Event : Event<float, float>
    {
        public Event() : base() { }
        public Event(float result) : base(result) { }
        public Event(string behaviorKey, float io) : base(behaviorKey, io) { }
        public Event(string behaviorKey, float io, float target) : base(behaviorKey, io, target) { }
        public Event(Func<float> action) : base(action) { }
        public Event(Func<float, float> action) : base(action) { }
        public Event(Func<float, float, float> action) : base(action) { }
    }
    public class Event<T> : Event<T, T>
    {
        public Event() : base() { }
        public Event(string behaviorKey, T io) : base(behaviorKey, io) { }
        public Event(string behaviorKey, T io, T target) : base(behaviorKey, io, target) { }
        public Event(T result) : base(result) { }
        public Event(Func<T> action) : base(action) { }
        public Event(Func<T, T> action) : base(action) { }
        public Event(Func<T, T, T> action) : base(action) { }
    }
    public class Event<IO, T> : Moment
    {
        private Func<long, TimeController<IO, T>, IO, T, IO>? _activity;

        //time, time control   input, target, input for next event
        public Event() { }
        public Event(string? behaviorKey, IO io)
        {
            BehaviorKey = behaviorKey;
            InputOutput = io;
            ResolveBehaviorFromKey();
        }
        public Event(string? behaviorKey, IO io, T target)
        {
            BehaviorKey = behaviorKey;
            InputOutput = io;
            Target = target;
            ResolveBehaviorFromKey();
        }
        public Event(IO result)
        {
            BehaviorKey = EventBehavior.EchoInput;
            InputOutput = result;
            ResolveBehaviorFromKey();
        }
        public Event(Func<IO> action) { Function = (time, controller, io, target) => action(); }
        public Event(Func<T, IO> action) { Function = (time, controller, io, target) => action(target); }
        public Event(Func<IO, T, IO> action) { Function = (time, controller, io, target) => action(io, target); }
        public Event(Func<long, IO, T, IO> action) { Function = (time, controller, io, target) => action(time, io, target); }
        public Event(Func<long, TimeController<IO, T>, IO, T, IO> action) { Function = action; }

        public string? BehaviorKey { get; set; }
        [JsonIgnore]
        [GraphIgnore]
        public Func<long, TimeController<IO, T>, IO, T, IO> Function
        {
            get
            {
                if (_activity == null && BehaviorKey != null)
                    _activity = EventBehaviorRegistry<IO, T>.Get(BehaviorKey);

                return _activity ?? throw new InvalidOperationException("Unable to function, no acceptable behavior was found.");
            }
            private set => _activity = value;
        }
        public T? Target { get; set; }
        public IO? InputOutput { get; set; }

        public void BindActivity(Func<long, TimeController<IO, T>, IO, T, IO> func) => Function = func;

        public void ResolveBehaviorFromKey()
        {
            if (BehaviorKey != null)
                Function = EventBehaviorRegistry<IO, T>.Get(BehaviorKey) ?? throw new InvalidOperationException($"Unable to function with this behavior {BehaviorKey}.  This is most likely because this behavior has not been registered for type {typeof(IO).FullName}");
        }
    }
}
