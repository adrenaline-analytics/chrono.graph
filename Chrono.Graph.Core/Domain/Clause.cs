namespace Chrono.Graph.Core.Domain
{
    public class Clause
    {
        public string PropertyLabel { get; set; } = string.Empty;
        public string Hash { get; set; } = string.Empty;
        public object? Operand { get; set; } = string.Empty;
        public string Operator { get; set; } = string.Empty;
        public bool IsGroupOrExpression { get; set; } = false;
        public override bool Equals(object? obj) => obj?.GetHashCode() == GetHashCode();
        public override int GetHashCode() => new { val = Operand ?? "null" }.GetHashCode();
    }
}
