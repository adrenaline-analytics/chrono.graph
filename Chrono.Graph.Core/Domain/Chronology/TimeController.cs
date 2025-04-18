namespace Chrono.Graph.Core.Domain.Chronology
{
    public class TimeController<IO, T>
    {
        private readonly LinkedEvent<IO, T> _event;
        public TimeController(LinkedEvent<IO, T> @event)
        {
            _event = @event;
        }
        public void SetRunTime(long time) { }
        public void SetRunTicks(long ticks) { }
        public void SetPauseTime(long time) { }
        public void SetPauseTicks(long ticks) { }
    }
}
