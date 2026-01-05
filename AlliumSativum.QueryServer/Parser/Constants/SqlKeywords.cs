namespace AlliumSativum.Parser.Constants;

public static class SqlKeywords
{
    // ReSharper disable InconsistentNaming
    public const string SELECT = "SELECT";
    public const string FROM = "FROM";
    public const string WHERE = "WHERE";
    public const string JOIN = "JOIN";
    
    public static class JoinType
    {
        public const string LEFT = "LEFT";
        public const string RIGHT = "RIGHT";
        public const string INNER = "INNER";
        public const string OUTER = "OUTER";
    }
}
