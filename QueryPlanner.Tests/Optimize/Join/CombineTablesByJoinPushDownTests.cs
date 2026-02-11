using AlliumSativum.Optimize;
using AlliumSativum.Shared.Exceptions;
using AlliumSativum.Shared.Models.IntermediateModels;
using AlliumSativum.Shared.Models.IntermediateModels.Expressions;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;
using FluentAssertions;
using QueryPlanner.Tests.Helpers;

namespace QueryPlanner.Tests.Optimize.Join;

public sealed class CombineTablesByJoinPushDownTests
{
    private static readonly ExpressionNodeOptimizer ExpressionOptimizer = new();
    private static readonly JoinOptimizer JoinOptimizer = new(ExpressionOptimizer);
    private static readonly WhereOptimizer WhereOptimizer = new(ExpressionOptimizer);
    private static readonly SelectOptimizer SelectOptimizer = new();

    private static readonly Optimizer Optimizer = new(null!, ExpressionOptimizer, JoinOptimizer, SelectOptimizer, WhereOptimizer);

    [Test]
    public void Should_Combine_Tables_Same_Datasource()
    {
        var select = SelectBaseModelHelper.FromAsSql("""
            SELECT t.subject, tc.body 
            FROM ticket->tickets t 
            INNER JOIN ticket->ticket_comments tc ON tc.ticket_id = t.id
            """);
        var (onPremise, tablePlans) = Optimizer.SplitIntoTables(select);

        var (joinsLeft, joinedTablePlans) = JoinOptimizer.CombineTablesByJoinPushDown(onPremise.Join, tablePlans);

        joinsLeft.Should().BeEmpty();
        joinedTablePlans.Should().HaveCount(1);

        joinedTablePlans.ShouldContainSelect(
            expectedFrom: new TableSpecifier("ticket", "tickets"),
            expectedSelect: [
                new AttributeSpecifier("ticket", "tickets", "subject"),
                new AttributeSpecifier("ticket", "ticket_comments", "body")
            ],
            expectedJoin: [ 
                new JoinBaseModel
                {
                    Inner = new TableSpecifier("ticket", "ticket_comments"),
                    Expression = new BinaryOperatorExpressionNode
                    {
                        Left = new FullySpecifiedColumnExpressionNode { Attribute = new AttributeSpecifier("ticket", "ticket_comments", "ticket_id") },
                        Operation = "=",
                        Right = new FullySpecifiedColumnExpressionNode { Attribute = new AttributeSpecifier("ticket", "tickets", "id") }
                    }
                }
            ]
        );
    }

    [Test]
    public void Should_Not_Combine_Tables_Different_Datasource()
    {
        var select = SelectBaseModelHelper.FromAsSql(@"""
            SELECT t.subject, e.name 
            FROM ticket->tickets t 
            INNER JOIN erp->employee e ON e.id = t.assigned_employee_id
            """);
        var (onPremise, tablePlans) = Optimizer.SplitIntoTables(select);

        var (joinsLeft, joinedTablePlans) = JoinOptimizer.CombineTablesByJoinPushDown(onPremise.Join, tablePlans);

        // Expect join to stay on-premise because tables are in different datasources
        joinsLeft.Should().HaveCount(1);
        joinedTablePlans.Should().HaveCount(2);

        // Both original table plans should still exist
        joinedTablePlans.ShouldContainSelect(
            expectedFrom: new TableSpecifier("ticket", "tickets"),
            expectedSelect: [ new AttributeSpecifier("ticket", "tickets", "subject") ]
        );
        joinedTablePlans.ShouldContainSelect(
            expectedFrom: new TableSpecifier("erp", "employee"),
            expectedSelect: [ new AttributeSpecifier("erp", "employee", "name") ]
        );
    }

    [Test]
    public void Should_Throw_When_JoinExpression_Not_Two_Tables()
    {
        // craft a select where join expression references only one table and a value (invalid for combine)
        var select = SelectBaseModelHelper.FromAsSql(@"""
            SELECT t.subject, e.name 
            FROM ticket->tickets t 
            INNER JOIN erp->employee e ON e.id = '1'
            """);

        var (onPremise, tablePlans) = Optimizer.SplitIntoTables(select);

        var act = () => JoinOptimizer.CombineTablesByJoinPushDown(onPremise.Join, tablePlans);
        act.Should().Throw<AsSqlOptimizeException>();
    }

    [Test]
    public void Should_Throw_When_One_TablePlan_Missing()
    {
        var select = SelectBaseModelHelper.FromAsSql(@"""
            SELECT t.subject, tc.body 
            FROM ticket->tickets t 
            INNER JOIN ticket->ticket_comments tc ON tc.ticket_id = t.id
            """);
        var (onPremise, tablePlans) = Optimizer.SplitIntoTables(select);

        // remove one table plan to simulate missing plan
        tablePlans.RemoveAll(p => p.From.TableName == "ticket_comments");

        var act = () => JoinOptimizer.CombineTablesByJoinPushDown(onPremise.Join, tablePlans);
        act.Should().Throw<AsSqlOptimizeException>();
    }
}
