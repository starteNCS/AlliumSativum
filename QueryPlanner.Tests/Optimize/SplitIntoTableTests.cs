using AlliumSativum.Optimize;
using AlliumSativum.Shared.Models.IntermediateModels.Expressions;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;
using AlliumSativum.Worker.Sdk;
using FluentAssertions;
using NSubstitute;
using QueryPlanner.Tests.Helpers;

namespace QueryPlanner.Tests.Optimize;

public sealed class SplitIntoTableTests
{
    private static readonly ExpressionNodeOptimizer ExpressionOptimizer = new();
    private static readonly JoinOptimizer JoinOptimizer = new(ExpressionOptimizer);
    private static readonly WhereOptimizer WhereOptimizer = new(ExpressionOptimizer);
    private static readonly SelectOptimizer SelectOptimizer = new();
    private static readonly IPlannerApi Planner = Substitute.For<IPlannerApi>();

    private static readonly Optimizer Optimizer = new(Planner, ExpressionOptimizer, JoinOptimizer, SelectOptimizer,
        WhereOptimizer);

    [Test]
    public void Should_Split_Tables_One_Table()
    {
        var select = SelectBaseModelHelper.FromAsSql("SELECT t.subject FROM ticket->tickets t");
        var (onPremise, dataSources) = Optimizer.SplitIntoTables(select);

        onPremise.ShouldBeEmpty();

        dataSources.Should().NotBeEmpty();
        dataSources.Count.Should().Be(1);

        var dataSourceSelect = dataSources.Single();
        dataSourceSelect.ShouldBeSelect(
            new TableSpecifier("ticket", "tickets"),
            [new AttributeSpecifier("ticket", "tickets", "subject")]);
    }

    [Test]
    public void Should_Split_Tables_Two_Tables_One_Datasource()
    {
        var select = SelectBaseModelHelper.FromAsSql("""
                                                     SELECT t.subject, tc.body 
                                                     FROM ticket->tickets t 
                                                     INNER JOIN ticket->ticket_comments tc ON tc.ticket_id = t.id
                                                     """);
        var (onPremise, dataSources) = Optimizer.SplitIntoTables(select);

        onPremise.ShouldBeSelect(join: select.Join);

        dataSources.Should().NotBeEmpty();
        dataSources.Count.Should().Be(2);

        dataSources.ShouldContainSelect(
            new TableSpecifier("ticket", "tickets"),
            [new AttributeSpecifier("ticket", "tickets", "subject")]);
        dataSources.ShouldContainSelect(
            new TableSpecifier("ticket", "ticket_comments"),
            [new AttributeSpecifier("ticket", "ticket_comments", "body")]);
    }

    [Test]
    public void Should_Split_Tables_Two_Tables_Two_Datasources()
    {
        var select = SelectBaseModelHelper.FromAsSql("""
                                                     SELECT t.subject, e.name 
                                                     FROM ticket->tickets t 
                                                     INNER JOIN erp->employee e ON e.id = t.assigned_employee_id
                                                     """);
        var (onPremise, dataSources) = Optimizer.SplitIntoTables(select);

        onPremise.ShouldBeSelect(join: select.Join);

        dataSources.Should().NotBeEmpty();
        dataSources.Count.Should().Be(2);

        dataSources.ShouldContainSelect(
            new TableSpecifier("ticket", "tickets"),
            [new AttributeSpecifier("ticket", "tickets", "subject")]);
        dataSources.ShouldContainSelect(
            new TableSpecifier("erp", "employee"),
            [new AttributeSpecifier("erp", "employee", "name")]);
    }

    [Test]
    public void Should_Split_Tables_One_Table_Full_Where()
    {
        var select = SelectBaseModelHelper.FromAsSql("""
                                                     SELECT t.subject
                                                     FROM ticket->tickets t 
                                                     WHERE t.customer_id = '1234'
                                                     """);
        var (onPremise, dataSources) = Optimizer.SplitIntoTables(select);

        onPremise.ShouldBeEmpty();

        dataSources.Should().NotBeEmpty();
        dataSources.Count.Should().Be(1);

        dataSources.ShouldContainSelect(
            new TableSpecifier("ticket", "tickets"),
            [new AttributeSpecifier("ticket", "tickets", "subject")],
            expectedWhere: select.Where);
    }

    [Test]
    public void Should_Split_Tables_Two_Tables_Split_Where()
    {
        var select = SelectBaseModelHelper.FromAsSql("""
                                                     SELECT t.subject, e.name
                                                     FROM ticket->tickets t 
                                                     INNER JOIN erp->employee e ON e.id = t.assigned_employee_id
                                                     WHERE t.customer_id = '1234' AND e.name = 'Philipp'
                                                     """);
        var (onPremise, dataSources) = Optimizer.SplitIntoTables(select);

        onPremise.ShouldBeSelect(join: select.Join);

        dataSources.Should().NotBeEmpty();
        dataSources.Count.Should().Be(2);

        dataSources.ShouldContainSelect(
            new TableSpecifier("ticket", "tickets"),
            [new AttributeSpecifier("ticket", "tickets", "subject")],
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
            });

        dataSources.ShouldContainSelect(
            new TableSpecifier("erp", "employee"),
            [new AttributeSpecifier("erp", "employee", "name")],
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
            });
    }

    [Test]
    public void Should_Split_Tables_Two_Tables_Split_Where_And_Keep_Mixed_On_Premise()
    {
        var select = SelectBaseModelHelper.FromAsSql("""
                                                     SELECT t.subject, e.name
                                                     FROM ticket->tickets t 
                                                     INNER JOIN erp->employee e ON e.id = t.assigned_employee_id
                                                     WHERE t.customer_id = '1234' 
                                                         AND e.name = 'Philipp'
                                                         AND t.assigned_employee_id = e.id
                                                     """);
        var (onPremise, dataSources) = Optimizer.SplitIntoTables(select);

        onPremise.ShouldBeSelect(
            join: select.Join,
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
            });

        dataSources.Should().NotBeEmpty();
        dataSources.Count.Should().Be(2);

        dataSources.ShouldContainSelect(
            new TableSpecifier("ticket", "tickets"),
            [new AttributeSpecifier("ticket", "tickets", "subject")],
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
            });

        dataSources.ShouldContainSelect(
            new TableSpecifier("erp", "employee"),
            [new AttributeSpecifier("erp", "employee", "name")],
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
            });
    }
}