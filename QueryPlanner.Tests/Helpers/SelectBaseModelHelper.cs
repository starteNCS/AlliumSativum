using AlliumSativum.Parser;
using AlliumSativum.Semantic;
using AlliumSativum.Shared.Models.IntermediateModels;
using AlliumSativum.Shared.Models.IntermediateModels.Expressions;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;
using AlliumSativum.Token;
using FluentAssertions;
using FluentAssertions.Primitives;

namespace QueryPlanner.Tests.Helpers;

public static class SelectBaseModelHelper
{
    private static readonly Tokenizer Tokenizer = new();
    private static readonly TokenQueryParser TokenQueryParser = new();
    private static readonly SemanticTransformer SemanticTransformer = new();
    
    public static SelectBaseModel ToSelectDto(this string query)
    {
        var tokens = Tokenizer.Tokenize(query);
        var parsed = TokenQueryParser.Parse(tokens);
        if(parsed is null) throw new Exception("Failed to parse query");
        
        SemanticTransformer.Transform(parsed);
        return parsed;
    }

    extension(ObjectAssertions assertions)
    {
        public void BeSelectDto(SelectBaseModel selectBaseModel)
        {
            assertions.Subject.ToString().Should().Be(selectBaseModel.ToString());
        }

        public void NotBeSelectDto(SelectBaseModel selectBaseModel)
        {
            assertions.Subject.ToString().Should().NotBe(selectBaseModel.ToString());
        }

        public void ShouldBeExpressionNode(ExpressionNode expected)
        {
            assertions.Subject?.ToString().Should().Be(expected?.ToString());
        }
        
        public void ShouldNotBeExpressionNode(ExpressionNode? expected)
        {
            assertions.Subject?.ToString().Should().NotBe(expected?.ToString());
        }
    }


    public static void ShouldBeSelect(this SelectBaseModel selectBaseModel, TableSpecifier? from = null,
        List<AttributeSpecifier>? select = null, List<JoinBaseModel>? join = null, ExpressionNode? where = null)
    {
        if (from is not null) selectBaseModel.From.ShouldBeTable(from);

        if (select is not null)
            foreach (var item in select)
                selectBaseModel.Select.ShouldContainAttributeSpecifier(item);

        if (join is not null) selectBaseModel.Join.Should().Contain(join);

        if (where is not null) selectBaseModel.Where.Should().Be(where);
    }

    public static void ShouldContainSelect(this List<SelectBaseModel> selectBaseModels,
        TableSpecifier? expectedFrom = null, List<AttributeSpecifier>? expectedSelect = null,
        List<JoinBaseModel>? expectedJoin = null, ExpressionNode? expectedWhere = null)
    {
        if (expectedFrom is not null)
        {
            selectBaseModels = selectBaseModels.Where(x => x.From.Equals(expectedFrom)).ToList();
            selectBaseModels.Should().NotBeEmpty("could not match FROM");
        }

        if (expectedSelect is not null)
        {
            selectBaseModels = selectBaseModels.Where(model => expectedSelect.TrueForAll(s => model.Select.Exists(x =>
                        x is AttributeSpecifier attr &&
                        attr.DataSourceName == s.DataSourceName &&
                        attr.TableName == s.TableName &&
                        attr.AttributeName == s.AttributeName
                    )
                )
            ).ToList();
            selectBaseModels.Should().NotBeEmpty("could not match SELECT");
        }

        if (expectedJoin is not null)
        {
            selectBaseModels = selectBaseModels.Where(x =>
                expectedJoin.TrueForAll(j =>
                    x.Join.Exists(actual =>
                        actual.Inner.Equals(j.Inner) &&
                        actual.Expression.Equals(j.Expression)
                    )
                )
            ).ToList();
            selectBaseModels.Should().NotBeEmpty("could not match JOIN");
        }

        if (expectedWhere is not null)
        {
            selectBaseModels = selectBaseModels.Where(x => x.Where?.Equals(expectedWhere) ?? false).ToList();
            selectBaseModels.Should().NotBeEmpty("could not match WHERE");
        }

        selectBaseModels.Count.Should().Be(1, "this methods expects exactly one select");
    }

    public static void ShouldBeEmpty(this SelectBaseModel onPremise)
    {
        // FROM cannot be empty, as the field is not logically nullable
        onPremise.Should().NotBeNull();
        onPremise.Join.Should().BeEmpty();
        onPremise.Select.Should().BeEmpty();
        onPremise.Where.Should().BeNull();
    }
}