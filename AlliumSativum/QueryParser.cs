using SqlParser;

namespace AlliumSativum;

public class QueryParser
{
    public void Parse(string query)
    {
        var parser = new SqlQueryParser().Parse(query);
        
        Console.WriteLine(parser);
    }
}