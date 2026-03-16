namespace AlliumSativum.Connectors.PostgreSQL.Models.ORM;

public sealed class PostgresColumnsModel
{
    public string TableSchema { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string ColumnName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public long MaximumOctetLength { get; set; }

    public bool IsNummeric =>
        DataType is "smallint" or "integer" or "bigint" or "decimal" or "numeric" or "real" or "double precision";
}