namespace AlliumSativum.Shared.Exceptions;

public sealed class AsSqlOptimizeException : AsSqlException
{
    public AsSqlOptimizeException(string message)
    {
        AsMessage = message;
    }

    public override string ToString()
    {
        return AsMessage;
    }
}
