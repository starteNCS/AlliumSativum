using System.Text.Json.Serialization;

namespace AlliumSativum.Shared.Models.IntermediateModels.Specifiers;

// fix empty output for ASP.NET endpoints
[JsonPolymorphic(IgnoreUnrecognizedTypeDiscriminators = true)]
[JsonDerivedType(typeof(AttributeSpecifier), "attribute")]
[JsonDerivedType(typeof(VariableMappingSpecifier), "variable")]
[JsonDerivedType(typeof(TableSpecifier), "table")]
[JsonDerivedType(typeof(DataSourceSpecifier), "dataSource")]
public interface ISpecifier
{
}