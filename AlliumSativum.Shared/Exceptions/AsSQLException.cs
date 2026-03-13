namespace AlliumSativum.Shared.Exceptions;

public class AsSqlException : Exception
{
    public string AsMessage { get; set; }

    public AsSqlException(string asMessage)
    {
        AsMessage = asMessage;
    }

    public override string ToString()
    {
        return AsMessage;
    }
}
