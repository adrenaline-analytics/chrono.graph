namespace Chrono.Graph.Core.Domain.Chronology
{

    public class TimeSlice : Counter
    {
        public DateTime? Start
        {
            get => RunTime != null
                ? DateTimeOffset.FromUnixTimeMilliseconds(RunTime.Value).UtcDateTime
                : null;
            set => RunTime = value != null
                ? new DateTimeOffset(value.Value).ToUnixTimeMilliseconds()
                : null;
        }
        public DateTime? Finish
        {
            get => RunTime != null && RunTicks != null
                ? DateTimeOffset.FromUnixTimeMilliseconds(Math.Abs(RunTicks.Value) + RunTime.Value).UtcDateTime
                : null;
            set => RunTicks = value != null
                ? new DateTimeOffset(value.Value).ToUnixTimeMilliseconds()
                : null;
        }
    }
}
