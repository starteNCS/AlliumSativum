using AlliumSativum.Shared.Models.IntermediateModels;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;

namespace AlliumSativum.Worker.Sdk.Extensions;

public static class ModelExtensions
{
    public static GSelectBaseModel ToGrpcModel(this SelectBaseModel model)
    {
        var payload = new GSelectBaseModel
        {
            From = new GTableSpecifier
            {
                TableName = model.From!.TableName,
                DataSource = model.From!.DataSourceName,
            }
        };
        payload.Select.AddRange(model.Select.Select(s =>
        {
            var aSpec = s as AttributeSpecifier;
            return new GAttributeSpecifier
            {
                Table = new GTableSpecifier
                {
                    TableName = aSpec.TableName,
                    DataSource = aSpec.DataSourceName
                },
                AttributeName = aSpec.AttributeName
            };
        }));

        return payload;
    }

    public static SelectBaseModel FromGrpcModel(this GSelectBaseModel model)
    {
        return new SelectBaseModel
        {
            From = new TableSpecifier(model.From.DataSource, model.From.TableName),
            Select = model.Select.Select(ISpecifier (spec) =>
                new AttributeSpecifier(spec.Table.DataSource, spec.Table.TableName, spec.AttributeName)).ToList()
        };
    }
}
