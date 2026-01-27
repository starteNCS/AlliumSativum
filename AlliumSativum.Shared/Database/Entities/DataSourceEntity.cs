using AlliumSativum.Shared.Enums;

namespace AlliumSativum.Shared.Database.Entities;

public sealed class DataSourceEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ConnectorType Connector { get; set; }
    public string ConnectionString { get; set; } = string.Empty;
}
