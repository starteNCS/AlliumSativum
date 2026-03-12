using System.Numerics;
using AlliumSativum.Shared.Costs;
using AlliumSativum.Shared.Exceptions;
using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators.Join;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators.Models;
using AlliumSativum.Shared.Models.IntermediateModels;
using AlliumSativum.Shared.Models.IntermediateModels.Expressions;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;

namespace AlliumSativum.Optimize;

public sealed class JoinOptimizer
{
    private readonly ExpressionNodeOptimizer _expressionNodeOptimizer;
    private readonly ICostModel _costModel;

    public JoinOptimizer(
        ExpressionNodeOptimizer expressionNodeOptimizer,
        ICostModel costModel)
    {
        _expressionNodeOptimizer = expressionNodeOptimizer;
        _costModel = costModel;
    }
    
    /// <summary>
    /// Constructs the "real" Join-POP tree from the intermediate model 
    /// </summary>
    /// <param name="joins"></param>
    /// <param name="popLookupTable"></param>
    public async Task<List<PlanOperator>> ConstructJoinPopTreeFromIntermediateJoinTreeAsync(List<JoinBaseModel> joins, PopLookupTable popLookupTable)
    {
        if(joins.Count == 0)
        {
            return [popLookupTable.Single()];
        }
        
        // 1. Identify all unique tables involved
        var allTables = joins.SelectMany(j => j.AffectedTables).Distinct().ToList();

        // Memoization: Map a bitmask of tables to all possible PlanOperators for that set
        var memo = new Dictionary<int, List<PlanOperator>>();

        return await BuildSubtreesAsync((1 << allTables.Count) - 1, allTables, joins, memo, popLookupTable);
    }

    private async Task<List<PlanOperator>> BuildSubtreesAsync(int mask, List<TableSpecifier> tables, List<JoinBaseModel> joins, Dictionary<int, List<PlanOperator>> memo, PopLookupTable popLookupTable)
{
    // 1. Check Memoization Table
    if (memo.TryGetValue(mask, out var subtrees))
    {
        return subtrees;
    }

    var results = new List<PlanOperator>();

    // 2. Base Case: Only one table in the set, return the access plan
    if ((mask & (mask - 1)) == 0) 
    {
        int index = BitOperations.TrailingZeroCount(mask);
        results.Add(popLookupTable.Get(tables[index]));
        memo[mask] = results; // Memoize the base case too
        return results;
    }

    // --- PRUNING STATE ---
    // Track the absolute best plan we can find for this specific subset of tables
    PlanOperator cheapestPlanForMask = null;
    double minCost = double.MaxValue;

    // 3. Iterate through all possible binary splits of the current mask
    for (int submask = (mask - 1) & mask; submask > 0; submask = (submask - 1) & mask)
    {
        int leftMask = submask;
        int rightMask = mask ^ submask;

        var leftPlans = await BuildSubtreesAsync(leftMask, tables, joins, memo, popLookupTable);
        var rightPlans = await BuildSubtreesAsync(rightMask, tables, joins, memo, popLookupTable);

        foreach (var left in leftPlans)
        {
            foreach (var right in rightPlans)
            {
                // Find the expression that connects the 'left' set and 'right' set
                var expression = FindExpressionForSets(leftMask, rightMask, tables, joins);
                
                if (expression != null)
                {
                    // Merge distribution data
                    var distributions = ((List<Dictionary<AttributeSpecifier, PlanOperatorDistributionData>>)[left.DistributionData, right.DistributionData])
                        .SelectMany(d => d)
                        .ToDictionary(kv => kv.Key, kv => kv.Value);
                    
                    var distributionData = await _costModel.GetDistributionOfExpressionAsync((BinaryOperatorExpressionNode) expression, distributions, [left, right]);

                    // --- Evaluate Nested Loop Join ---
                    var nlj = new NestedLoopJoinPlanOperator(left, expression, right)
                    {
                        DistributionData = distributionData.Distribution,
                        Selectivity = distributionData.Selectivity,
                        ExpectedCardinality = distributionData.Cardinality
                    };
                    nlj.Cost = _costModel.CalculateCost(nlj);
                    
                    if (nlj.Cost < minCost)
                    {
                        minCost = nlj.Cost;
                        cheapestPlanForMask = nlj;
                    }

                    if (expression.IsEquiJoin())
                    {
                        // --- Evaluate Hash Join ---
                        var hj = new HashJoinPlanOperator(left, expression, right)
                        {
                            DistributionData = distributionData.Distribution,
                            Selectivity = distributionData.Selectivity,
                            ExpectedCardinality = distributionData.Cardinality
                        };
                        hj.Cost = _costModel.CalculateCost(hj);
                        
                        if (hj.Cost < minCost)
                        {
                            minCost = hj.Cost;
                            cheapestPlanForMask = hj;
                        }

                        // --- Evaluate Merge Sort Join ---
                        var msj = new MergeSortJoinPlanOperator(left, expression, right)
                        {
                            DistributionData = distributionData.Distribution,
                            Selectivity = distributionData.Selectivity,
                            ExpectedCardinality = distributionData.Cardinality
                        };
                        msj.Cost = _costModel.CalculateCost(msj);
                        
                        if (msj.Cost < minCost)
                        {
                            minCost = msj.Cost;
                            cheapestPlanForMask = msj;
                        }
                    }
                }
            }
        }
    }

    // 4. Finalize and Memoize
    // Instead of adding dozens of permutations, we only add the absolute cheapest one
    if (cheapestPlanForMask != null)
    {
        results.Add(cheapestPlanForMask);
    }

    memo[mask] = results;
    return results;
}

    private ExpressionNode? FindExpressionForSets(int mask1, int mask2, List<TableSpecifier> tables, List<JoinBaseModel> joins)
    {
        // Identify which tables are in each mask and find the join spec that connects them
        // This logic assumes a join exists; in a cross-product scenario, this would return a Const(true)
        return joins.FirstOrDefault(j => 
            (TableInMask(j.Inner, mask1, tables) && TableInMask(j.GetJoinExpressionTable(), mask2, tables)) ||
            (TableInMask(j.Inner, mask2, tables) && TableInMask(j.GetJoinExpressionTable(), mask1, tables))
        )?.Expression;
    }
    
    private bool TableInMask(TableSpecifier table, int mask, List<TableSpecifier> allTables)
    {
        // 1. Find the position (index) of the table in our master list
        int index = allTables.IndexOf(table);
    
        // 2. Create a bit for that index (1 << index) 
        // and check if it exists in the mask using bitwise AND
        if (index == -1) return false;
        return (mask & (1 << index)) != 0;
    }
    
    public (List<JoinBaseModel> joinsLeft, List<SelectBaseModel> joinedTablePlans) CombineTablesByJoinPushDown(List<JoinBaseModel> joins, List<SelectBaseModel> tablePlans)
    {
        var joinsLeft = new List<JoinBaseModel>();
        
        foreach (var join in joins)
        {
            var joinSelects = tablePlans
                .Where(x =>  join.AffectedTables.Any(at => x.AffectedTables.Contains(at)))
                .ToList();

            if (joinSelects.Exists(x => x.From!.DataSourceName != joinSelects[0].From!.DataSourceName))
            {
                // At least one of the plans used for the join stems from another data source
                joinsLeft.Add(join);
                continue;
            }

            if (join.AffectedTables.Count != 2)
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
    public (List<JoinBaseModel> onPremiseJoins, List<AttributeSpecifier> selectNeeded) ConstructOnPremiseJoin(SelectBaseModel select)
    {
        var mixedJoins = GetOnlyMixedJoins(select);
        return (mixedJoins, mixedJoins.SelectMany(x => x.Expression.GetAttributesOfExpression()).ToList());
    }
    
    public (List<JoinBaseModel> onPremiseJoins, List<AttributeSpecifier> selectNeeded) AddJoinToIntermediateJoinTree(List<JoinBaseModel> root, JoinBaseModel join)
    {
        root.Add(join);
        return (root, join.Expression.GetAttributesOfExpression());
    }
    

    
    /// <summary>
    /// Returns a list of all joins, where the tables reside in different data sources
    /// </summary>
    /// <param name="select"></param>
    /// <returns></returns>
    public List<JoinBaseModel> GetOnlyMixedJoins(SelectBaseModel select) =>
        select.Join
            .Where(join => join.GetJoinExpressionTable().DataSourceName != join.Inner.DataSourceName)
            .ToList();
}
