using Chrono.Graph.Core.Application;
using Chrono.Graph.Core.Domain.Chronology;

namespace Adrenaline.Service
{
    public class ChronoService : IChronoService
    {
        /// <summary>
        /// Bare metal warning, this function works best on bare metal.  
        /// Avoid virtual machines when possible for most accuracy.
        /// </summary>
        /// <returns>Milliseconds since Epoch 01-01-1970T00:00:00.000</returns>
        public long Now() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        public void StartCounter(Counter counter) 
            => StartCounter(counter, Now());
        public void StartCounter (Counter counter, long now)
        {
            counter.RunTime = now;
            counter.PauseTime = null;
            counter.PauseTicks = null;
        }
        public void PauseCounter (Counter counter)  
            => PauseCounter(counter, Now());
        public void PauseCounter(Counter counter, long now) => counter.PauseTime = now;
        public void ResumeCounter(Counter counter)
            => ResumeCounter(counter, Now());
        public void ResumeCounter(Counter counter, long now) {
            counter.PauseTicks += now - (counter.PauseTime ?? 0);
            counter.PauseTime = null;
        }
        public void ResetCounter(Counter counter)
            => ResetCounter(counter, Now());
        public void ResetCounter(Counter counter, long now)
        {
            counter.RunTime = null;
            counter.PauseTime = null;
            counter.PauseTicks = null;
        }
        public long GetElapsedTime(Counter counter)
            => GetElapsedTime(counter, Now());
        public long GetElapsedTime(Counter counter, long now)
        {
            if (counter.RunTime == null)
                return 0;

            var pausedTicks = counter.PauseTicks ?? 0;
            var referenceTime = counter.PauseTime ?? now;

            return Math.Max(0, referenceTime - counter.RunTime.Value - pausedTicks);
        }

        public long GetRemainingTime(Counter counter) 
            => GetRemainingTime(counter, Now());
        public long GetRemainingTime(Counter counter, long now)
        {
            if (counter.RunTime == null || counter.RunTicks == null)
                return 0;

            var pausedTicks = counter.PauseTicks ?? 0;
            var pauseTime = counter.PauseTime;

            long elapsed = (pauseTime != null ? pauseTime.Value : now) - counter.RunTime.Value - pausedTicks;
            return Math.Max(0, counter.RunTicks.Value - elapsed);
        }



        public void Live(ChainOfEvents chain, Action<float, float> action) => Live<float, float>(chain, action);
        public void Live<IO, T>(ChainOfEvents<IO, T> chain, Action<IO, T> action) => Glimpse(chain, action, Now());
        public void Glimpse(ChainOfEvents chain, Action<float, float> action, long when) => Glimpse<float, float>(chain, action, when);
        public void Glimpse<IO, T>(ChainOfEvents<IO, T> chain, Action<IO, T> action, long when)
        {
            foreach(var origin in chain.Origin ?? [])
                LookForward(origin, (step, l) => {
                    if (l.Function != null)
                        chain.Input = l.Function(step, new TimeController<IO, T>(l), chain.Input, chain.Target);
                });
        }
        public void LookForward<IO, T>(LinkedEvent<IO, T> origin, Action<long, LinkedEvent<IO, T>> action, long? when = null, long resolution = 5)
        {
            var stop = Math.Min(when ?? long.MaxValue, GetEffectiveEndTime(origin) ?? long.MaxValue);
            if (origin.RunTime != null)
            {
                var step = origin.RunTime.Value;
                action(step, origin);
                while ((step += resolution) <= stop)
                    action(step, origin);
            }

            foreach(var nextEvent in (origin.Next ?? []).Where(e => e.PauseTime == null) ?? []) 
                LookForward(nextEvent, action, when, resolution);

        }
        public void LookBackward<IO, T>(LinkedEvent<IO, T> terminal, Action<long, LinkedEvent<IO, T>> rollback, long? when = null, long resolution = 5)
        {

            var stop = Math.Min(when ?? long.MinValue, terminal.RunTime ?? long.MinValue);
            var start = GetEffectiveEndTime(terminal);
            if (start != null)
            {
                var step = start.Value;
                rollback(step, terminal);
                while ((step -= resolution) >= stop)
                    rollback(step, terminal);
            }

            foreach(var priorEvent in (terminal.Prior ?? []).Where(e => e.PauseTime == null) ?? []) 
                LookForward(priorEvent, rollback, when, resolution);
        }
        public long? GetEffectiveEndTime(Counter c) => c.RunTime + c.RunTime + c.PauseTicks;
        public long? GetEffectiveStartTime(Counter c, long now)
        {
            if (c.RunTime == null) return null;

            long pause = c.PauseTicks ?? 0;
            if (c.PauseTime.HasValue)
                pause += now - c.PauseTime.Value;

            return c.RunTime.Value + pause;
        }


    }
}

