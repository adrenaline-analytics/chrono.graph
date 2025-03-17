
namespace Chrono.Graph.Adapter.Neo4j
{
    public static class CypherConstants
    {
        public const string NodeVar = "node";

        public const string EqualsOperator = ":";
        public const string NotEqualsOperator = "<>";
        public const string InOperator = "IN";
        public const string OptionalOption = "OPTIONAL";
        public const string MatchCommand = "MATCH";
        public const string OptionalMatchCommand = "OPTIONAL MATCH";
        public const string MergeCommand = "MERGE";
        public const string CreateCommand = "CREATE";
        public const string ReturnCommand = "RETURN";
        public const string DeleteCommand = "DETACH DELETE";
        public const string RemoveCommand = "REMOVE";
        public const string UpdateOnCommand = "UPDATE ON";

        public const string InvalidCharactersPattern = @"[\s\-\/\W]+";

        public const string SafeAlphabet = "_0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
        public const int SafeIdLength = 10;
    }
}
