namespace Chrono.Graph.Core.Domain.Chronology.Scheduling
{
    public class ScheduleEvent<S> : ScheduleEvent<S, float, float>
    {
        public ScheduleEvent() : base() { }
        public ScheduleEvent(float result) : base(result) { }
        public ScheduleEvent(Func<float> action) : base(action) { }
        public ScheduleEvent(Func<float, float> action) : base(action) { }
        public ScheduleEvent(Func<float, float, float> action) : base(action) { }
    }
    public class ScheduleEvent<S, T> : ScheduleEvent<S, T, T>
    {
        public ScheduleEvent() : base() { }
        public ScheduleEvent(T result) : base(result) { }
        public ScheduleEvent(Func<T> action) : base(action) { }
        public ScheduleEvent(Func<T, T> action) : base(action) { }
        public ScheduleEvent(Func<T, T, T> action) : base(action) { }
    }

    public class ScheduleEvent<S, IO, T> : ScopedEvent<S, IO, T>
    {
        public ScheduleEvent() : base() { }
        public ScheduleEvent(IO result) : base(result) { }
        public ScheduleEvent(Func<IO> action) : base(action) { }
        public ScheduleEvent(Func<T, IO> action) : base(action) { }
        public ScheduleEvent(Func<IO, T, IO> action) : base(action) { }
        public ScheduleEvent(Func<long, IO, T, IO> action) : base(action) { }
        public ScheduleEvent(Func<long, TimeController<IO, T>, IO, T, IO> action) : base(action) { }
        public IList<TimeSpan>? Reminders { get; set; }
        public S? Attendees { get; set; }

    }
}
