using AlliumSativum.Optimize;
using AlliumSativum.Shared.Models.IntermediateModels.Expressions;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;
using FluentAssertions;
using QueryPlanner.Tests.Helpers;

namespace QueryPlanner.Tests.Optimize.Where;

public sealed class AssinWhereToJoinedProposalsTest
{
    private static readonly ExpressionNodeOptimizer ExpressionOptimizer = new();
    private static readonly JoinOptimizer JoinOptimizer = new(ExpressionOptimizer);
    private static readonly WhereOptimizer WhereOptimizer = new(ExpressionOptimizer);
    private static readonly SelectOptimizer SelectOptimizer = new();
    private static readonly Optimizer Optimizer = new(null!, ExpressionOptimizer, JoinOptimizer, SelectOptimizer, WhereOptimizer);

    [Test]
    public void Should_Push_Down_Single_Table_Clauses_To_Proposals()
    {
        var select = SelectBaseModelHelper.FromAsSql(@"""
            SELECT t.subject, e.name 
            FROM ticket->tickets t 
            INNER JOIN erp->employee e ON e.id = t.assigned_employee_id
            WHERE t.customer_id = '1234' AND e.name = 'Philipp'
            """);

        var (onPremise, tables) = Optimizer.SplitIntoTables(select);
        (onPremise.Join, var joinedTableSelect) = JoinOptimizer.CombineTablesByJoinPushDown(onPremise.Join, tables);

        WhereOptimizer.AssignWhereToJoinedProposals(onPremise, joinedTableSelect);

        onPremise.Where.Should().BeNull();

        joinedTableSelect.ShouldContainSelect(
            expectedFrom: new TableSpecifier("ticket", "tickets"),
            expectedWhere: new BinaryOperatorExpressionNode
            {
                Left = new FullySpecifiedColumnExpressionNode
                {
                    Attribute = new AttributeSpecifier("ticket", "tickets", "customer_id")
                },
                Operation = "=",
                Right = new ValueExpressionNode
                {
                    Value = "1234",
                    Type = ValueExpressionNode.ValueExpressionType.String
                }
            }
        );

        joinedTableSelect.ShouldContainSelect(
            expectedFrom: new TableSpecifier("erp", "employee"),
            expectedWhere: new BinaryOperatorExpressionNode
            {
                Left = new FullySpecifiedColumnExpressionNode
                {
                    Attribute = new AttributeSpecifier("erp", "employee", "name")
                },
                Operation = "=",
                Right = new ValueExpressionNode
                {
                    Value = "Philipp",
                    Type = ValueExpressionNode.ValueExpressionType.String
                }
            }
        );
    }

    [Test]
    public void Should_Keep_Mixed_Clauses_On_Premise()
    {
        var select = SelectBaseModelHelper.FromAsSql(@"""
            SELECT t.subject, e.name 
            FROM ticket->tickets t 
            INNER JOIN erp->employee e ON e.id = t.assigned_employee_id
            WHERE t.customer_id = '1234' AND e.name = 'Philipp' AND t.assigned_employee_id = e.id
            """);

        var (onPremise, tables) = Optimizer.SplitIntoTables(select);
        var (joinsLeft, joinedTableSelect) = JoinOptimizer.CombineTablesByJoinPushDown(onPremise.Join, tables);
        onPremise.Join = joinsLeft;

        WhereOptimizer.AssignWhereToJoinedProposals(onPremise, joinedTableSelect);

        onPremise.ShouldBeSelect(
            where: new BinaryOperatorExpressionNode
            {
                Left = new FullySpecifiedColumnExpressionNode
                {
                    Attribute = new AttributeSpecifier("ticket", "tickets", "assigned_employee_id")
                },
                Operation = "=",
                Right = new FullySpecifiedColumnExpressionNode
                {
                    Attribute = new AttributeSpecifier("erp", "employee", "id")
                }
            }
        );

        joinedTableSelect.ShouldContainSelect(
            expectedFrom: new TableSpecifier("ticket", "tickets"),
            expectedWhere: new BinaryOperatorExpressionNode
            {
                Left = new FullySpecifiedColumnExpressionNode
                {
                    Attribute = new AttributeSpecifier("ticket", "tickets", "customer_id")
                },
                Operation = "=",
                Right = new ValueExpressionNode
                {
                    Value = "1234",
                    Type = ValueExpressionNode.ValueExpressionType.String
                }
            }
        );
        joinedTableSelect.ShouldContainSelect(
            expectedFrom: new TableSpecifier("erp", "employee"),
            expectedWhere: new BinaryOperatorExpressionNode
            {
                Left = new FullySpecifiedColumnExpressionNode
                {
                    Attribute = new AttributeSpecifier("erp", "employee", "name")
                },
                Operation = "=",
                Right = new ValueExpressionNode
                {
                    Value = "Philipp",
                    Type = ValueExpressionNode.ValueExpressionType.String
                }
            }
        );
    }

    [Test]
    public void Should_Handle_No_Where_Gracefully()
    {
        var select = SelectBaseModelHelper.FromAsSql(@"""
            SELECT t.subject, e.name 
            FROM ticket->tickets t 
            INNER JOIN erp->employee e ON e.id = t.assigned_employee_id
            """);

        var (onPremise, tables) = Optimizer.SplitIntoTables(select);
        var (joinsLeft, joinedTableSelect) = JoinOptimizer.CombineTablesByJoinPushDown(onPremise.Join, tables);
        onPremise.Join = joinsLeft;

        WhereOptimizer.AssignWhereToJoinedProposals(onPremise, joinedTableSelect);

        onPremise.Where.Should().BeNull();
        joinedTableSelect.ShouldContainSelect(
            expectedFrom: new TableSpecifier("ticket", "tickets")
        );
        joinedTableSelect.ShouldContainSelect(
            expectedFrom: new TableSpecifier("erp", "employee")
        );
    }
}
