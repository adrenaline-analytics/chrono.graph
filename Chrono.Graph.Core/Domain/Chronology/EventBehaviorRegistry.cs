namespace Chrono.Graph.Core.Domain.Chronology
{
    public static class EventBehaviorRegistry<IO, T>
    {
        private static readonly Dictionary<string, Func<long, TimeController<IO, T>, IO, T, IO>> _map = new();

        public static void Add(string key, Func<long, TimeController<IO, T>, IO, T, IO> func)
        {
            _map[key] = func;
        }

        public static Func<long, TimeController<IO, T>, IO, T, IO>? Get(string key)
        {
            return _map.TryGetValue(key, out var f) ? f : null;
        }
    }
}
