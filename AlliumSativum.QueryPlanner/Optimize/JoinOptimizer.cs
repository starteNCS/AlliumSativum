using AlliumSativum.Shared.Exceptions;
using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators;
using AlliumSativum.Shared.Models.IntermediateModels;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;
using AlliumSativum.Shared.Utils;

namespace AlliumSativum.Optimize;

public sealed class JoinOptimizer
{
    private readonly ExpressionNodeOptimizer _expressionNodeOptimizer;

    public JoinOptimizer(ExpressionNodeOptimizer expressionNodeOptimizer)
    {
        _expressionNodeOptimizer = expressionNodeOptimizer;
    }
    
    /// <summary>
    /// Constructs the "real" Join-POP tree from the intermediate model 
    /// </summary>
    /// <param name="intermediateJoinTree"></param>
    /// <param name="popLookupTable"></param>
    public PlanOperator ConstructJoinPopTreeFromIntermediateJoinTree(IIntermediateJoinNode? intermediateJoinTree,
        PopLookupTable popLookupTable)
    {
        if (intermediateJoinTree is null && popLookupTable.Count == 1)
        {
            return popLookupTable.Single();
        }

        if (intermediateJoinTree is null)
        {
            throw new AsSqlOptimizeException("Expected a intermediate join tree, as there are more than one plans");
        }

        return CloneTransformJoinTree(intermediateJoinTree, null, ref popLookupTable);
    }

    public PlanOperator CloneTransformJoinTree(IIntermediateJoinNode? node, PlanOperator? pop, ref PopLookupTable popLookupTable)
    {
        if (pop is not null && node is null)
        {
            return pop;
        }

        // 2. "Process" the current node (The "Pre" in Pre-Order)
        if (node is IntermediateJoinNode joinNode)
        {
            var left = CloneTransformJoinTree(joinNode.Left, pop, ref popLookupTable);
            var right = CloneTransformJoinTree(joinNode.Right, pop, ref popLookupTable);
            
            return new JoinPlanOperator(left, joinNode.Expression, right);
        }
    
        if (node is IntermediateJoinTreeTableSpecifier tableNode)
        {
            var planOperator = popLookupTable.GetAndRemove(tableNode.ToTableSpecifier());
            return planOperator ?? throw new AsSqlOptimizeException("Expected to find plan for table specifier, but found null");
        }

        throw new InvalidOperationException("Unknown node type.");
    }
    
    public (List<JoinBaseModel> joinsLeft, List<SelectBaseModel> joinedTablePlans) CombineTablesByJoinPushDown(List<JoinBaseModel> joins, List<SelectBaseModel> tablePlans)
    {
        var joinsLeft = new List<JoinBaseModel>();
        
        foreach (var join in joins)
        {
            var joinTables = _expressionNodeOptimizer.GetTablesOfExpression(join.Expression);

            var joinSelects = tablePlans
                .Where(x => joinTables.Contains(x.From!))
                .ToList();

            if (joinSelects.Exists(x => x.From!.DataSourceName != joinSelects[0].From!.DataSourceName))
            {
                // At least one of the plans used for the join stems from another data source
                joinsLeft.Add(join);
                continue;
            }

            if (joinTables.Count != 2)
            {
                throw new AsSqlOptimizeException("Currently, only joins with parameters from exactly two tables are supported");
            }
            
            if (joinSelects.Count != 2)
            {
                throw new AsSqlOptimizeException("At least one of the join plans are missing");
            }

            var proposal = new SelectBaseModel()
            {
                From = GetFromForJoin(join, joinSelects),
                Where = _expressionNodeOptimizer.MergeCnfExpressions(joinSelects[0].Where, joinSelects[1].Where),
                Select = [..joinSelects[0].Select, ..joinSelects[1].Select],
                Join = [..joinSelects[0].Join, ..joinSelects[1].Join,  join]
            };
            tablePlans.Remove(joinSelects[0]);
            tablePlans.Remove(joinSelects[1]);
            tablePlans.Add(proposal);
        }
        
        return (joinsLeft, tablePlans);
    }

    public TableSpecifier GetFromForJoin(JoinBaseModel join, List<SelectBaseModel> joinSelects)
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
    public (IIntermediateJoinNode? mixedJoinTree, List<AttributeSpecifier> selectNeeded) ConstructOnPremiseJoin(SelectBaseModel select)
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

        
        return (root, mixedJoins.SelectMany(x => _expressionNodeOptimizer.GetAttributesOfExpression(x.Expression)).ToList());
    }
    
    /// <summary>
    /// Returns the "other" table from a join (the table needed for the expression, rather than the newly joined table)
    /// </summary>
    /// <param name="join"></param>
    /// <returns></returns>
    public TableSpecifier GetJoinExpressionTable(JoinBaseModel join)
    {
        return _expressionNodeOptimizer.GetAttributesOfExpression(join.Expression)
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
    public List<JoinBaseModel> GetOnlyMixedJoins(SelectBaseModel select) =>
        select.Join
            .Where(join => GetJoinExpressionTable(join).DataSourceName != join.Inner.DataSourceName)
            .ToList();
}
