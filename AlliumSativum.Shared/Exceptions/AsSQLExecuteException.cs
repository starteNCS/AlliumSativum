using AlliumSativum.Shared.Enums;

namespace AlliumSativum.Shared.Exceptions;

public sealed class AsSQLExecuteException : AsSqlException
{
    public ConnectorType? Connector { get; set; }
    
    public AsSQLExecuteException(string message, ConnectorType? connector = null) : base(message)
    {
        Connector = connector;
    }
}
