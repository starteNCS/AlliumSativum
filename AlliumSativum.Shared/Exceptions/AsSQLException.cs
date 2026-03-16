namespace AlliumSativum.Shared.Exceptions;

public class AsSqlException : Exception
{
    public AsSqlException(string asMessage)
    {
        AsMessage = asMessage;
    }

    public string AsMessage { get; set; }

    public override string ToString()
    {
        return AsMessage;
    }
}