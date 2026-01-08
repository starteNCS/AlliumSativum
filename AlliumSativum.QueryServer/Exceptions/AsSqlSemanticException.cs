namespace AlliumSativum.Exceptions;

public sealed class AsSqlSemanticException : AsSqlException
{
    public AsSqlSemanticException(string message)
    {
        AsMessage = message;
    }
}
