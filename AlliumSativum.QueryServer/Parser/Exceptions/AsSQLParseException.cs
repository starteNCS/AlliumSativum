namespace AlliumSativum.Parser.Exceptions;

public sealed class AsSqlParseException : AsSqlException
{
    public string ParseContent { get; set; }

    public AsSqlParseException(string parseContent, string message)
    {
        ParseContent = parseContent;
        AsMessage = message;
    }

    public override string ToString()
    {
        return $"Tried to parse: {ParseContent}{Environment.NewLine}Message: {AsMessage}";
    }
}
