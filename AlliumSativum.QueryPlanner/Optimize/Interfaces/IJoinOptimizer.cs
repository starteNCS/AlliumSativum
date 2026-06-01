using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.IntermediateModels;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;

namespace AlliumSativum.Optimize.Interfaces;

public interface IJoinOptimizer
{
    /// <summary>
    ///     Constructs the most optimal join order and join types for the given joins.
    ///     This method uses a dynamic programming approach.
    /// </summary>
    /// <param name="joins">Joins to enumerate</param>
    /// <param name="popLookupTable">A lookup table for the table specifier to POP</param>
    /// <param name="prune">If enabled, only the most optimal plan is returned. Otherwise, all plans are returned</param>
    /// <returns>
    ///     A list containing the most optimal join plan, if pruning is enabled. Otherwise, a list of all possible join plans.
    /// </returns>
    Task<List<PlanOperator>> EnumerateBushyJoinsAsync(List<JoinBaseModel> joins,
        PopLookupTable popLookupTable, bool prune = true);

    /// <summary>
    ///     Combines the table splits by pushing down a join, if possible.
    ///     This is only done if both sides of the join are from the same data source
    /// </summary>
    /// <param name="joins">All joins, that are still in the base select dto</param>
    /// <param name="tableSplits">The table splits</param>
    /// <returns></returns>
    (List<JoinBaseModel> joinsLeft, List<SelectDto> joinedTablePlans) CombineTableSplitsByJoinPushDown(
        List<JoinBaseModel> joins, List<SelectDto> tableSplits);

    /// <summary>
    ///     Constructing a list containing all joins that need to be executed on Premise,
    /// </summary>
    /// <param name="select">The base select dto</param>
    /// <returns>
    ///     - onPremiseJoins: A list of joins that need to be executed on premise
    ///     - selectNeeded: A list of attribute specifiers that are needed to execute the on premise joins
    /// </returns>
    (List<JoinBaseModel> onPremiseJoins, List<AttributeSpecifier> selectNeeded) ExtractOnPremiseJoins(
        SelectDto select);
}