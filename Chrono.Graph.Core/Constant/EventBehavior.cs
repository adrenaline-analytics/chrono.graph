namespace Chrono.Graph.Core.Constant
{
    public static class EventBehavior
    {
        public const string EchoInput = "echo.input";
        public static class Physics
        {
            public const string Fall = "physics.fall";
            public const string Bounce = "physics.bounce";
            public const string Drag = "physics.drag";
            public const string Thermal = "physics.thermal";
            public const string Throw = "physics.throw";
        }

        public static class Mood
        {
            public const string Smile = "mood.smile";
            public const string Frown = "mood.frown";
        }

        public static class Math
        {
            public const string Increment = "math.increment";
            public const string Double = "math.double";
            public const string Abs = "math.abs";
        }
    }
}
