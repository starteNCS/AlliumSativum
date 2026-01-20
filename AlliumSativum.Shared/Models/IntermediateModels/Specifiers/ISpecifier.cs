using System.Text.Json.Serialization;
using AlliumSativum.Shared.Models.IntermediateModels.Expressions;

namespace AlliumSativum.Shared.Models.IntermediateModels.Specifiers;

// fix empty output for ASP.NET endpoints
[JsonPolymorphic(IgnoreUnrecognizedTypeDiscriminators = true)]
[JsonDerivedType(typeof(AttributeSpecifier), typeDiscriminator: "attribute")]
[JsonDerivedType(typeof(VariableMappingSpecifier), typeDiscriminator: "variable")]
[JsonDerivedType(typeof(TableSpecifier), typeDiscriminator: "table")]
[JsonDerivedType(typeof(DataSourceSpecifier), typeDiscriminator: "dataSource")]
public interface ISpecifier
{
    
}
