using System.Collections;
using System.Linq.Expressions;
using AlliumSativum.Shared.Exceptions;
using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.IntermediateModels;
using AlliumSativum.Shared.Models.IntermediateModels.Expressions;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;
using AlliumSativum.Shared.Utils;

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

    /// <summary>
    /// Constructing all joins that need to be executed on Premise, 
    /// this might return a heavily one-sided tree, but since we later do the Join Order Optimization
    /// this is negligible
    ///
    /// returns a tree in some: join(join(join(T0, T1), T2), T3)
    /// </summary>
    /// <param name="select"></param>
    /// <returns></returns>
    private (IIntermediateJoinNode? mixedJoinTree, List<AttributeSpecifier> selectNeeded) ConstructOnPremiseJoin(SelectBaseModel select)
    {
        // TODO: JOIN ORDER OPTIMIZATION???? kann durch constaint auf nur inner join auch später noch gemischt werden
        var mixedJoins = GetOnlyMixedJoins(select);
        List<JoinBaseModel> joins = [..mixedJoins];
        var joinsCount = joins.Count;
        if (joinsCount == 0)
        {
            return (null, []);
        }
        
        // create a join Lookup to search for a join given the table of the join expression
        var joinLookup = new Dictionary<TableSpecifier, List<JoinBaseModel>>();
        foreach (var j in joins)
        {
            var joinExpressionTable = GetJoinExpressionTable(j);
            if (joinLookup.TryGetValue(joinExpressionTable, out var joinExpressions))
            {
                joinExpressions.Add(j);
                continue;
            }
            
            joinLookup.Add(joinExpressionTable, [j]);
        }

        var join = joins.GetFirstAndRemove();
        var root = new IntermediateJoinNode
        {
            Left = IntermediateJoinTreeTableSpecifier.FromTableSpecifier(join.Inner),
            Expression = join.Expression,
            Right = IntermediateJoinTreeTableSpecifier.FromTableSpecifier(GetJoinExpressionTable(join)),
        };

        List<IntermediateJoinTreeTableSpecifier> alreadyJoinedRelations = [
            (IntermediateJoinTreeTableSpecifier)root.Left,
            (IntermediateJoinTreeTableSpecifier)root.Right
        ];

        int continued = 0;
        while (joins.Count > 0)
        {
            join = joins.GetAndRemove(x => alreadyJoinedRelations.Contains(GetJoinExpressionTable(x)));
            if (join is null)
            {
                continued++;
                if (continued > joinsCount)
                {
                    throw new AsSqlOptimizeException($"Unable to join the join into the tree");
                }
                continue;
            }
            continued = 0;

            root = new IntermediateJoinNode()
            {
                Left = root,
                Expression = join.Expression,
                Right = IntermediateJoinTreeTableSpecifier.FromTableSpecifier(join.Inner)
            };
            select.Join.Remove(join);
        }

        
        return (root, mixedJoins.SelectMany(x => GetAttributesOfExpression(x.Expression)).ToList());
    }
    
    /// <summary>
    /// Returns the "other" table from a join (the table needed for the expression, rather than the newly joined table)
    /// </summary>
    /// <param name="join"></param>
    /// <returns></returns>
    private static TableSpecifier GetJoinExpressionTable(JoinBaseModel join)
    {
        return GetAttributesOfExpression(join.Expression)
            .Select(x => new TableSpecifier(x.DataSourceName, x.TableName))
            .Where(x => !x.Equals(join.Inner))
            .Distinct()
            .Single();
    } 
    
    /// <summary>
    /// Returns a list of all joins, where the tables reside in different data sources
    /// </summary>
    /// <param name="select"></param>
    /// <returns></returns>
    private static List<JoinBaseModel> GetOnlyMixedJoins(SelectBaseModel select) =>
        select.Join
            .Where(join => GetJoinExpressionTable(join).DataSourceName != join.Inner.DataSourceName)
            .ToList();
}
