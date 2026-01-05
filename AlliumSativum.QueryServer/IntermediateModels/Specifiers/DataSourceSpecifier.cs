namespace AlliumSativum.IntermediateModels;

public class DataSourceSpecifier
{
    public string DataSourceName { get; set; }

    public DataSourceSpecifier(string dataSourceNameName)
    {
        DataSourceName = dataSourceNameName;
    }
}