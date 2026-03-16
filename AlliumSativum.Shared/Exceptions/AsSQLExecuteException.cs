using AlliumSativum.Shared.Enums;

namespace AlliumSativum.Shared.Exceptions;

public sealed class AsSQLExecuteException : AsSqlException
{
    public AsSQLExecuteException(string message, ConnectorType? connector = null) : base(message)
    {
        Connector = connector;
    }

    public ConnectorType? Connector { get; set; }
}