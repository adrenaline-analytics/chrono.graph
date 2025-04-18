using Chrono.Graph.Core.Constant;

namespace Chrono.Graph.Core.Domain.Chronology
{
    public static class EventRegistrar
    {
        private static Func<long, TimeController<IO, T>, IO, T, IO> GetEcho<IO, T> ()  
        {
            return new Func<long, TimeController<IO, T>, IO, T, IO>((time, ctrl, io, t) => io); 
        }
        public static void Register<T>() => Register<T>(EventBehavior.EchoInput);
        public static void Register<T>(string behavior) => EventBehaviorRegistry<T, T>.Add(behavior, GetEcho<T, T>());
        public static void RegisterDefaults()
        {
            EventBehaviorRegistry<float, float>.Add(EventBehavior.EchoInput, (time, ctrl, io, t) => io);
            EventBehaviorRegistry<float, float>.Add(EventBehavior.Math.Double, (time, ctrl, io, t) => io * 2);
            EventBehaviorRegistry<float, float>.Add(EventBehavior.Math.Increment, (time, ctrl, io, t) => io + 1);
            EventBehaviorRegistry<float, float>.Add(EventBehavior.Physics.Fall, (time, ctrl, io, t) => io - 9.8f * (time / 1000f));
        }
    }
}
