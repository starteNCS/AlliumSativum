using System.Numerics;
using AlliumSativum.Optimize.Interfaces;
using AlliumSativum.Shared.Costs;
using AlliumSativum.Shared.Exceptions;
using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators.Join;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators.Models;
using AlliumSativum.Shared.Models.IntermediateModels;
using AlliumSativum.Shared.Models.IntermediateModels.Expressions;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;

namespace AlliumSativum.Optimize;

public sealed class JoinOptimizer : IJoinOptimizer
{
    private readonly ICostModel _costModel;
    private readonly IExpressionNodeOptimizer _expressionNodeOptimizer;

    public JoinOptimizer(
        IExpressionNodeOptimizer expressionNodeOptimizer,
        ICostModel costModel)
    {
        _expressionNodeOptimizer = expressionNodeOptimizer;
        _costModel = costModel;
    }


    /// <inheritdoc />
    public async Task<List<PlanOperator>> EnumerateBushyJoinsAsync(List<JoinBaseModel> joins,
        PopLookupTable popLookupTable, bool prune = true)
    {
        if (joins.Count == 0) return [popLookupTable.Single()];

        var allTables = joins.SelectMany(j => j.AffectedTables).Distinct().ToList();
        var memo = new Dictionary<int, List<PlanOperator>>();

        // (1 << allTables.Count) - 1 fills all bits up to the number of tables with a 1
        return await BuildSubtreesAsync((1 << allTables.Count) - 1, allTables, joins, memo, popLookupTable, prune);
    }

    /// <inheritdoc />
    public (List<JoinBaseModel> joinsLeft, List<SelectDto> joinedTablePlans) CombineTableSplitsByJoinPushDown(
        List<JoinBaseModel> joins, List<SelectDto> tableSplits)
    {
        var joinsLeft = new List<JoinBaseModel>();

        foreach (var join in joins)
        {
            var joinSelects = tableSplits
                .Where(x => join.AffectedTables.Any(at => x.AffectedTables.Contains(at)))
                .ToList();

            if (joinSelects.Exists(x => x.From!.DataSourceName != joinSelects[0].From!.DataSourceName))
            {
                // At least one of the plans used for the join stems from another data source
                joinsLeft.Add(join);
                continue;
            }

            if (join.AffectedTables.Count != 2)
                throw new AsSqlOptimizeException(
                    "Currently, only joins with parameters from exactly two tables are supported");

            if (joinSelects.Count != 2) throw new AsSqlOptimizeException("At least one of the join plans are missing");

            var proposal = new SelectDto
            {
                From = GetFromForJoin(join, joinSelects),
                Where = _expressionNodeOptimizer.MergeCnfExpressions(joinSelects[0].Where, joinSelects[1].Where),
                Select = [..joinSelects[0].Select, ..joinSelects[1].Select],
                Join = [..joinSelects[0].Join, ..joinSelects[1].Join, join]
            };
            tableSplits.Remove(joinSelects[0]);
            tableSplits.Remove(joinSelects[1]);
            tableSplits.Add(proposal);
        }

        return (joinsLeft, tableSplits);
    }

    /// <inheritdoc />
    public (List<JoinBaseModel> onPremiseJoins, List<AttributeSpecifier> selectNeeded) ExtractOnPremiseJoins(
        SelectDto select)
    {
        var mixedJoins = GetOnlyMixedJoins(select);
        return (mixedJoins, mixedJoins.SelectMany(x => x.Expression.GetAttributesOfExpression()).ToList());
    }

    /// <summary>
    ///     Dynamic programming approach to build all possible join trees for a given set of tables and joins.
    ///     Using a bitmask to represent subsets of tables and memoization to avoid redundant calculations.
    ///     🤖 Developed iteratively with the help of Google Gemini
    /// </summary>
    /// <param name="mask">The mask of the current subtree</param>
    /// <param name="tables">All tables of the selectDto</param>
    /// <param name="joins">All joins that should be enumerated</param>
    /// <param name="memo">The momoization dictionary</param>
    /// <param name="popLookupTable">A lookup table for table specifier -> access POP</param>
    /// <param name="prune">If worse plans should be discared</param>
    /// <returns>(All / most optimal) POP for this join, depending on prune</returns>
    private async Task<List<PlanOperator>> BuildSubtreesAsync(int mask, List<TableSpecifier> tables,
        List<JoinBaseModel> joins, Dictionary<int, List<PlanOperator>> memo, PopLookupTable popLookupTable, bool prune)
    {
        // 1. Check Memoization Table
        if (memo.TryGetValue(mask, out var subtrees)) return subtrees;

        var results = new List<PlanOperator>();

        // 2. Base Case: Only one table in the set, return the access plan
        if ((mask & (mask - 1)) == 0)
        {
            var index = BitOperations.TrailingZeroCount(mask);
            results.Add(popLookupTable.Get(tables[index]));
            memo[mask] = results;
            return results;
        }

        // State for pruning
        PlanOperator cheapestPlanForMask = null;
        var minCost = double.MaxValue;

        // 3. Iterate through all possible binary splits of the current mask
        for (var submask = (mask - 1) & mask; submask > 0; submask = (submask - 1) & mask)
        {
            var leftMask = submask;
            var rightMask = mask ^ submask;

            // Pass the prune flag down recursively
            var leftPlans = await BuildSubtreesAsync(leftMask, tables, joins, memo, popLookupTable, prune);
            var rightPlans = await BuildSubtreesAsync(rightMask, tables, joins, memo, popLookupTable, prune);

            foreach (var left in leftPlans)
            foreach (var right in rightPlans)
            {
                // Find the expression that connects the 'left' set and 'right' set
                var expression = FindExpressionForSets(leftMask, rightMask, tables, joins);

                if (expression == null) continue;

                // Merge distribution data
                var distributions = ((List<Dictionary<AttributeSpecifier, PlanOperatorDistributionData>>)
                        [left.DistributionData, right.DistributionData])
                    .SelectMany(d => d)
                    .ToDictionary(kv => kv.Key, kv => kv.Value);

                var costData =
                    await _costModel.GetDistributionOfExpressionAsync((BinaryOperatorExpressionNode)expression,
                        distributions,
                        [left, right]);

                // Local helper to cleanly handle the pruning logic for all join types
                void ProcessPlan(PlanOperator plan)
                {
                    plan.Cost = _costModel.CalculateCost(plan);
                    if (!prune)
                    {
                        results.Add(plan);
                    }
                    else if (plan.Cost < minCost)
                    {
                        minCost = plan.Cost;
                        cheapestPlanForMask = plan;
                    }
                }

                // --- Evaluate Nested Loop Join ---
                var nlj = new NestedLoopJoinPlanOperator(left, expression, right)
                {
                    DistributionData = costData.Distribution,
                    Selectivity = costData.Selectivity,
                    ExpectedCardinality = costData.Cardinality,
                    Width = left.Width + right.Width - expression.GetAttributesOfExpression().Count
                };
                ProcessPlan(nlj);

                if (expression.IsEquiJoin())
                {
                    // --- Evaluate Hash Join ---
                    var hj = new HashJoinPlanOperator(left, expression, right)
                    {
                        DistributionData = costData.Distribution,
                        Selectivity = costData.Selectivity,
                        ExpectedCardinality = costData.Cardinality,
                        Width = left.Width + right.Width - expression.GetAttributesOfExpression().Count
                    };
                    ProcessPlan(hj);
                }
            }
        }

        // 4. Finalize and Memoize
        // If we are pruning, add ONLY the cheapest plan we found across all iterations for this mask
        if (prune && cheapestPlanForMask != null) results.Add(cheapestPlanForMask);

        memo[mask] = results;
        return results;
    }

    /// <summary>
    ///     Finds an expression (join), that could join the two masks (table sets)
    /// </summary>
    /// <param name="mask1">First mask</param>
    /// <param name="mask2">Second mask</param>
    /// <param name="tables">All tables</param>
    /// <param name="joins">All joins left</param>
    /// <returns>If available, an expression joining both masks</returns>
    private ExpressionNode? FindExpressionForSets(int mask1, int mask2, List<TableSpecifier> tables,
        List<JoinBaseModel> joins)
    {
        return joins.FirstOrDefault(j =>
            (TableInMask(j.Inner, mask1, tables) && TableInMask(j.GetJoinExpressionTable(), mask2, tables)) ||
            (TableInMask(j.Inner, mask2, tables) && TableInMask(j.GetJoinExpressionTable(), mask1, tables))
        )?.Expression;
    }

    /// <summary>
    ///     Checks if a table is part of the mask, by identifying the index of the table in the list of all tables
    ///     and checking if the corresponding bit is set in the mask.
    /// </summary>
    /// <param name="table">The table to look for</param>
    /// <param name="mask">The mask to check</param>
    /// <param name="allTables">All tables</param>
    /// <returns>True, if table is in mask</returns>
    private static bool TableInMask(TableSpecifier table, int mask, List<TableSpecifier> allTables)
    {
        var index = allTables.IndexOf(table);
        if (index == -1) return false;
        return (mask & (1 << index)) != 0;
    }

    /// <summary>
    ///     Get the "FROM" for the join, that is the newly joined table ("INNER JOIN table ON ...")
    /// </summary>
    /// <param name="join">The join to load the FROM from</param>
    /// <param name="joinSelects">Both sides of the join as a select dto</param>
    /// <returns>The FROM table specifier </returns>
    private static TableSpecifier GetFromForJoin(JoinBaseModel join, List<SelectDto> joinSelects)
    {
        if (join.Inner == joinSelects[0].From) return joinSelects[1].From;

        return joinSelects[0].From;
    }

    /// <summary>
    ///     Returns a list of all joins, where the tables reside in different data sources
    /// </summary>
    /// <param name="select">The select dto to check the joins of</param>
    /// <returns>All of those joins, that cross data source borders</returns>
    public static List<JoinBaseModel> GetOnlyMixedJoins(SelectDto select)
    {
        return select.Join
            .Where(join => join.GetJoinExpressionTable().DataSourceName != join.Inner.DataSourceName)
            .ToList();
    }
}