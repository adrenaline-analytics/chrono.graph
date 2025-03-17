namespace Chrono.Graph.Core.Domain
{
    public class Clause
    {
        public object? Operand { get; set; } = string.Empty;
        public string Operator { get; set; } = string.Empty;
        public override bool Equals(object? obj) => obj?.GetHashCode() == GetHashCode();
        public override int GetHashCode() => new { val = Operand ?? "null" }.GetHashCode();
    }
}
