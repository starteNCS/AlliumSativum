namespace AlliumSativum.Shared.Exceptions;

public sealed class AsSqlParseException : AsSqlException
{
    public string ParseContent { get; set; }

    public AsSqlParseException(string parseContent, string message) : base(message)
    {
        ParseContent = parseContent;
    }

    public override string ToString()
    {
        return $"Tried to parse: {ParseContent}{Environment.NewLine}Message: {AsMessage}";
    }
}
