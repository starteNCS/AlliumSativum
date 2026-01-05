using AlliumSativum.Parser.Constants;

namespace AlliumSativum.Parser.IntermediateModels;

public sealed class RawSelectModel
{
    public string? Select { get; set; }
    public string? From { get; set; }
    public string? Where { get; set; }
    public string? Join { get; set; }

    public bool Validate()
    {
        if (Select == null || From == null)
        {
            return false;
        }

        return true;
    }
    
    public void Add(string type, string value)
    {
        if (type.Contains(SqlKeywords.JOIN))
        {
            Join = $"{type} {Join}"; // with every operator except join we can infer the type of operator from the field
            return;
        }
        
        switch (type)
        {
            case SqlKeywords.SELECT:
                Select = value;
                break;
            case SqlKeywords.FROM:
                From = value;
                break;
            case SqlKeywords.WHERE:
                Where = value;
                break;
            default:
                throw new NotImplementedException($"{type} not implemented");
        }
    }
}
