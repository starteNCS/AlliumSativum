using AlliumSativum.Shared.Enums;

namespace AlliumSativum.Shared.Exceptions;

public sealed class AsSqlExecuteException : AsSqlException
{
    public AsSqlExecuteException(string message, ConnectorType? connector = null) : base(message)
    {
        Connector = connector;
    }

    public ConnectorType? Connector { get; set; }
}