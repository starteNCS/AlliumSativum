using AlliumSativum.Parser;
using AlliumSativum.Semantic;
using AlliumSativum.Shared.Models.ExecutionPlan;
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
    
    /// <summary>
    /// Returns a SelectBaseModel for the provided query.
    /// </summary>
    /// /// <remarks>The validity of the model is shown in the respective tests</remarks>
    /// <param name="query"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static SelectDto ToSelectDto(this string query)
    {
        var tokens = Tokenizer.Tokenize(query);
        var parsed = TokenQueryParser.Parse(tokens);
        if(parsed is null) throw new Exception("Failed to parse query");
        
        SemanticTransformer.Transform(parsed);
        return parsed;
    }

    extension(ObjectAssertions assertions)
    {
        public void BeSelectDto(SelectDto selectDto)
        {
            assertions.Subject.ToString().Should().Be(selectDto.ToString());
        }

        public void NotBeSelectDto(SelectDto selectDto)
        {
            assertions.Subject.ToString().Should().NotBe(selectDto.ToString());
        }

        public void BeExpressionNode(ExpressionNode? expected)
        {
            assertions.Subject?.ToString().Should().Be(expected?.ToString());
        }
        
        public void NotBeExpressionNode(ExpressionNode? expected)
        {
            assertions.Subject?.ToString().Should().NotBe(expected?.ToString());
        }

        public void BePop(PlanOperator? other)
        {
            assertions.Subject?.GetType().Should().Be(other?.GetType());
            var pop = (PlanOperator) assertions.Subject;
            pop?.IsEquivalentTo(other).Should().BeTrue();
        }
        
        public void NotBePop(PlanOperator? other)
        {
            var pop = (PlanOperator) assertions.Subject;
            pop?.IsEquivalentTo(other).Should().BeFalse();
        }
    }


    public static void ShouldBeSelect(this SelectDto selectDto, TableSpecifier? from = null,
        List<AttributeSpecifier>? select = null, List<JoinBaseModel>? join = null, ExpressionNode? where = null)
    {
        if (from is not null) selectDto.From.ShouldBeTable(from);

        if (select is not null)
            foreach (var item in select)
                selectDto.Select.ShouldContainAttributeSpecifier(item);

        if (join is not null) selectDto.Join.Should().Contain(join);

        if (where is not null) selectDto.Where.Should().Be(where);
    }

    public static void ShouldContainSelect(this List<SelectDto> selectBaseModels,
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

    public static void ShouldBeEmpty(this SelectDto onPremise)
    {
        // FROM cannot be empty, as the field is not logically nullable
        onPremise.Should().NotBeNull();
        onPremise.Join.Should().BeEmpty();
        onPremise.Select.Should().BeEmpty();
        onPremise.Where.Should().BeNull();
    }
}