using AlliumSativum.Shared.Models.IntermediateModels;

namespace AlliumSativum.Optimize;

public sealed class Optimizer
{
    // return qexp
    public object Optimize(SelectBaseModel model)
    {
        if (model.Join.Any())
        {
            return OptimizeJoin(model);
        }

        return OptimizePlain(model);
    }

    private object OptimizeJoin(SelectBaseModel model)
    {
        throw new NotImplementedException();
    }

    private object OptimizePlain(SelectBaseModel model)
    {
        return null!;
    }
}
