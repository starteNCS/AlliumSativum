using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;

namespace AlliumSativum.Shared.Models.ExecutionPlan;

public sealed class PopLookupTable
{
    private readonly Dictionary<TableSpecifier, PlanOperator> _lookupTable;

    public int Count => _lookupTable.Values.Distinct().Count();
    
    public PopLookupTable()
    {
        _lookupTable = new Dictionary<TableSpecifier, PlanOperator>();
    }

    public void Add(TableSpecifier tableSpecifier, PlanOperator planOperator)
    {
        _lookupTable.Add(tableSpecifier, planOperator);
    }

    public void Add(List<TableSpecifier> tableSpecifiers, PlanOperator planOperator)
    {
        foreach (var tableSpecifier in tableSpecifiers)
        {
            _lookupTable.Add(tableSpecifier, planOperator);
        }
    }

    public PlanOperator? GetAndRemove(TableSpecifier tableSpecifier)
    {
        if (!_lookupTable.TryGetValue(tableSpecifier, out var planOperator))
        {
            return null;
        }
        
        var tablesToRemove = _lookupTable
            .Where(x => x.Value == planOperator)
            .Select(x => x.Key)
            .ToList();
        
        foreach (var key in tablesToRemove)
        {
            _lookupTable.Remove(key);
        }

        return planOperator;
    }
    
    public PlanOperator? Get(TableSpecifier tableSpecifier)
    {
        if (!_lookupTable.TryGetValue(tableSpecifier, out var planOperator))
        {
            return null;
        }

        return planOperator;
    }

    public PlanOperator Single() => _lookupTable.Values.Distinct().Single();
}
