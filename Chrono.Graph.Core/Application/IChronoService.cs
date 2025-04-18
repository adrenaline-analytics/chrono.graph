using Chrono.Graph.Core.Domain.Chronology;

namespace Chrono.Graph.Core.Application
{
    public interface IChronoService
    {
        long GetElapsedTime(Counter counter);
        long GetElapsedTime(Counter counter, long now);
        long GetRemainingTime(Counter countdown);
        long GetRemainingTime(Counter countdown, long now);
        void Live(ChainOfEvents chain, Action<float, float> action);
        void Live<IO, T>(ChainOfEvents<IO, T> chain, Action<IO, T> action);
        void Glimpse(ChainOfEvents chain, Action<float, float> action, long when);
        void Glimpse<IO, T>(ChainOfEvents<IO, T> chain, Action<IO, T> action, long when);
        void LookBackward<IO, T>(LinkedEvent<IO, T> terminal, Action<long, LinkedEvent<IO, T>> rollback, long? when = null, long resolution = 5);
        void LookForward<IO, T>(LinkedEvent<IO, T> origin, Action<long, LinkedEvent<IO, T>> action, long? when = null, long resolution = 5);
        void PauseCounter(Counter counter);
        void PauseCounter(Counter counter, long now);
        void ResetCounter(Counter counter);
        void ResetCounter(Counter counter, long now);
        void ResumeCounter(Counter counter);
        void ResumeCounter(Counter counter, long now);
        void StartCounter(Counter counter);
        void StartCounter(Counter counter, long now);
        long Now();
    }
}
