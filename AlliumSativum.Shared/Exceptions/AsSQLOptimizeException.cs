namespace AlliumSativum.Shared.Exceptions;

public sealed class AsSqlOptimizeException : AsSqlException
{
    public AsSqlOptimizeException(string message) : base(message)
    {
    }

    public override string ToString()
    {
        return AsMessage;
    }
}