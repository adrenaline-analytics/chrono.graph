using Chrono.Graph.Core.Constant;

namespace Chrono.Graph.Core.Domain.Chronology
{
    public static class EventBehaviorBootstrapper
    {
        public static void RegisterDefaults()
        {
            EventBehaviorRegistry<float, float>.Register(EventBehavior.EchoInput, (time, ctrl, io, t) => io);
            EventBehaviorRegistry<float, float>.Register(EventBehavior.Math.Double, (time, ctrl, io, t) => io * 2);
            EventBehaviorRegistry<float, float>.Register(EventBehavior.Math.Increment, (time, ctrl, io, t) => io + 1);
            EventBehaviorRegistry<float, float>.Register(EventBehavior.Physics.Fall, (time, ctrl, io, t) => io - 9.8f * (time / 1000f));
        }
    }
}
