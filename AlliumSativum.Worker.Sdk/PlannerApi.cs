using AlliumSativum.Shared.Models.IntermediateModels;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;

namespace AlliumSativum.Worker.Sdk;

public sealed class PlannerApi
{
    private readonly Planner.PlannerClient _client;

    public PlannerApi(Planner.PlannerClient client)
    {
        _client = client;
    }

    public async Task<object> PlanQueryAsync(SelectBaseModel model)
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
        
        await _client.PlanAsync(payload);
        return null!;
    }
}
