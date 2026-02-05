using AlliumSativum.Shared.Exceptions;
using AlliumSativum.Shared.Models.IntermediateModels;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;

namespace AlliumSativum.Optimize;

public partial class Optimizer
{
    private (List<JoinBaseModel> joinsLeft, List<SelectBaseModel> joinedTablePlans) CombineTablesByJoinPushDown(List<JoinBaseModel> joins, List<SelectBaseModel> tablePlans)
    {
        var joinsLeft = new List<JoinBaseModel>();
        var joinedTablePlans = new List<SelectBaseModel>();
        
        // TODO: support multi way join (with 3 or more targets in expression)
        foreach (var join in joins)
        {
            var joinTables = GetTablesOfExpression(join.Expression);

            if (joinTables.Count != 2)
            {
                throw new AsSqlOptimizeException("Currently, only joins with parameters from exactly two tables are supported");
            }

            var joinSelects = tablePlans
                .Where(x => joinTables.Contains(x.From!))
                .ToList();

            if (joinSelects.Exists(x => x.From!.DataSourceName != joinSelects[0].From!.DataSourceName))
            {
                // At least one of the plans used for the join stems from another data source
                joinsLeft.Add(join);
                continue;
            }
            
            if (joinSelects.Count != 2)
            {
                throw new AsSqlOptimizeException("At least one of the join plans are missing");
            }
            
            joinedTablePlans.Add(new SelectBaseModel()
            {
                From = GetFromForJoin(join, joinSelects),
                Where = MergeCnfExpressions(joinSelects[0].Where, joinSelects[1].Where), 
                Select = [..joinSelects[0].Select, ..joinSelects[1].Select],
                Join = [join]
            });
            tablePlans.Remove(joinSelects[0]);
            tablePlans.Remove(joinSelects[1]);
        }
        
        return (joinsLeft, [..joinedTablePlans, ..tablePlans]);
    }

    private TableSpecifier GetFromForJoin(JoinBaseModel join, List<SelectBaseModel> joinSelects)
    {
        if (join.Inner == joinSelects[0].From)
        {
            return joinSelects[1].From;
        }
        
        return joinSelects[0].From;
    }
}
