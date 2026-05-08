using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.IntermediateModels;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;

namespace AlliumSativum.Optimize.Interfaces;

public interface IJoinOptimizer
{
    /// <summary>
    ///     Constructs the "real" Join-POP tree from the intermediate model
    /// </summary>
    /// <param name="joins"></param>
    /// <param name="popLookupTable"></param>
    Task<List<PlanOperator>> ConstructJoinPopTreeFromIntermediateJoinTreeAsync(List<JoinBaseModel> joins,
        PopLookupTable popLookupTable, bool prune = true);

    (List<JoinBaseModel> joinsLeft, List<SelectDto> joinedTablePlans) CombineTablesByJoinPushDown(
        List<JoinBaseModel> joins, List<SelectDto> tableSplits);

    /// <summary>
    ///     Constructing all joins that need to be executed on Premise,
    ///     this might return a heavily one-sided tree, but since we later do the Join Order Optimization
    ///     this is negligible
    ///     returns a tree in some: join(join(join(T0, T1), T2), T3)
    /// </summary>
    /// <param name="select"></param>
    /// <returns></returns>
    (List<JoinBaseModel> onPremiseJoins, List<AttributeSpecifier> selectNeeded) ExtractOnPremiseJoins(
        SelectDto select);
}
