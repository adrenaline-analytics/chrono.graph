namespace Chrono.Graph.Core.Constant
{
    [Flags]
    public enum GraphPrimitivity
    {
        Object = 1,
        String = 2,
        Bool = 4,
        Int = 8,
        Float = 16,
        Array = 32,
        Dictionary = 64,
        Function = 128
    }

}
