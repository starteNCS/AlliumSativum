using AlliumSativum.Shared.Constants;

namespace AlliumSativum.Shared.Models.IntermediateModels;

public sealed class RawSelectModel
{
    public string? Select { get; set; }
    public string? From { get; set; }
    public List<string> Where { get; set; } = [];
    public List<string> Join { get; set; } = [];

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
        if (type.Contains(AsSqlKeywords.JOIN))
        {
            Join.Add($"{type} {value}"); // with every operator except join we can infer the type of operator from the field
            return;
        }
        
        switch (type)
        {
            case AsSqlKeywords.SELECT:
                Select = value;
                break;
            case AsSqlKeywords.FROM:
                From = value;
                break;
            case AsSqlKeywords.WHERE:
                Where.Add(value);
                break;
            default:
                throw new NotSupportedException($"{type} not supported");
        }
    }
}
