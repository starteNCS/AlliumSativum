namespace AlliumSativum.Shared.Constants;

public static class AsSqlKeywords
{
    // ReSharper disable InconsistentNaming
    public const string SELECT = "SELECT";
    public const string FROM = "FROM";
    public const string WHERE = "WHERE";
    public const string JOIN = "JOIN";
    public const string ON = "ON";

    public static readonly List<string> Keywords = [SELECT, FROM, WHERE, JOIN, JoinType.LEFT, JoinType.RIGHT, JoinType.INNER, JoinType.OUTER];

    public static class BooleanOperators
    {
        public const string AND = "AND";
        public const string OR = "OR";
    }
    
    public static class JoinType
    {
        public const string LEFT = "LEFT";
        public const string RIGHT = "RIGHT";
        public const string INNER = "INNER";
        public const string OUTER = "OUTER";
        
        public static readonly List<string> Types = [LEFT, RIGHT, INNER, OUTER];
    }
}
