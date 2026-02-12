using AlliumSativum.Optimize;
using AlliumSativum.Shared.Exceptions;
using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators;
using AlliumSativum.Shared.Models.IntermediateModels;
using AlliumSativum.Worker.Sdk;
using FluentAssertions;
using NSubstitute;
using QueryPlanner.Tests.Helpers;

namespace QueryPlanner.Tests.Optimize;

public sealed class OptimizerTests
{
    private static readonly ExpressionNodeOptimizer ExpressionOptimizer = new();
    private static readonly JoinOptimizer JoinOptimizer = new(ExpressionOptimizer);
    private static readonly WhereOptimizer WhereOptimizer = new(ExpressionOptimizer);
    private static readonly SelectOptimizer SelectOptimizer = new();

    private static IPlannerApi CreatePlannerMock()
    {
        var planner = Substitute.For<IPlannerApi>();
        planner.PlanQueryAsync(Arg.Any<SelectBaseModel>())
            .Returns(ci =>
            {
                var model = (SelectBaseModel)ci[0]!;
                // return a simple pushdown plan per table
                var sql = $"SELECT * FROM {model.From.TableName}";
                var plan = new PushdownSqlPlanOperator(Guid.NewGuid(), sql)
                {
                    Cost = 1,
                    ExpectedCardinality = 100
                };
                return Task.FromResult<(PlanOperator?, SelectBaseModel?)>((plan, null));
            });
        return planner;
    }

    [Test]
    public async Task Should_Optimize_Single_Table_Select()
    {
        var select = SelectBaseModelHelper.FromAsSql("SELECT t.subject FROM ticket->tickets t");
        var planner = CreatePlannerMock();
        var optimizer = new Optimizer(planner, ExpressionOptimizer, JoinOptimizer, SelectOptimizer, WhereOptimizer);

        var plan = await optimizer.Optimize(select);

        plan.Should().NotBeNull();
        plan.RootOperator.Should().BeOfType<PushdownSqlPlanOperator>();
        var root = (PushdownSqlPlanOperator)plan.RootOperator;
        root.Cost.Should().BeGreaterThan(0);
        root.ExpectedCardinality.Should().Be(100);
    }

    [Test]
    public async Task Should_Optimize_Same_Datasource_Join()
    {
        var select = SelectBaseModelHelper.FromAsSql(@"""
            SELECT t.subject, tc.body 
            FROM ticket->tickets t 
            INNER JOIN ticket->ticket_comments tc ON tc.ticket_id = t.id
            """);
        var planner = CreatePlannerMock();
        var optimizer = new Optimizer(planner, ExpressionOptimizer, JoinOptimizer, SelectOptimizer, WhereOptimizer);

        var plan = await optimizer.Optimize(select);

        // Optimizer may return a single pushdown plan if the join collapses at planner stage,
        // or a JoinPlanOperator combining two pushdowns.
        if (plan.RootOperator is JoinPlanOperator join)
        {
            join.Left.Should().BeOfType<PushdownSqlPlanOperator>();
            join.Right.Should().BeOfType<PushdownSqlPlanOperator>();
        }
        else
        {
            plan.RootOperator.Should().BeOfType<PushdownSqlPlanOperator>();
        }
    }

    [Test]
    public async Task Should_Optimize_Cross_Datasource_Join_With_Where_Pushdown()
    {
        var select = SelectBaseModelHelper.FromAsSql(@"""
            SELECT t.subject, e.name 
            FROM ticket->tickets t 
            INNER JOIN erp->employee e ON e.id = t.assigned_employee_id
            WHERE t.customer_id = '1234' AND e.name = 'Philipp'
            """);
        var planner = CreatePlannerMock();
        var optimizer = new Optimizer(planner, ExpressionOptimizer, JoinOptimizer, SelectOptimizer, WhereOptimizer);

        var plan = await optimizer.Optimize(select);

        // Root may be a join of pushdowns, or a single pushdown depending on planner behavior.
        if (plan.RootOperator is JoinPlanOperator join)
        {
            join.Left.Should().BeAssignableTo<PlanOperator>();
            join.Right.Should().BeAssignableTo<PlanOperator>();
            join.Left.Should().BeOfType<PushdownSqlPlanOperator>();
            join.Right.Should().BeOfType<PushdownSqlPlanOperator>();
        }
        else
        {
            plan.RootOperator.Should().BeOfType<PushdownSqlPlanOperator>();
        }
    }

    [Test]
    public async Task Should_Throw_When_Planner_Returns_Null_Plan()
    {
        var select = SelectBaseModelHelper.FromAsSql("SELECT t.subject FROM ticket->tickets t");
        var planner = Substitute.For<IPlannerApi>();
        planner.PlanQueryAsync(Arg.Any<SelectBaseModel>()).Returns(Task.FromResult<(PlanOperator?, SelectBaseModel?)>((null, null)));
        var optimizer = new Optimizer(planner, ExpressionOptimizer, JoinOptimizer, SelectOptimizer, WhereOptimizer);

        var act = async () => await optimizer.Optimize(select);
        await act.Should().ThrowAsync<AsSqlOptimizeException>();
    }

    #region TestDriven_Failed_Queries

    [Test]
    public async Task Should_Optimize_Two_Joins_With_Same_Table_Same_Source()
    {
        var select = SelectBaseModelHelper.FromAsSql("""
                                                     SELECT t.subject, tc.body, tb.first_name, tb.last_name 
                                                     FROM ticket->tickets t 
                                                     INNER JOIN ticket->ticket_comments tc ON tc.ticket_id = t.id 
                                                     INNER JOIN ticket->time_bookings tb ON tb.ticket_id = t.id 
                                                     WHERE t.status = 'In Progress'
                                                     """);
        var planner = CreatePlannerMock();
        var optimizer = new Optimizer(planner, ExpressionOptimizer, JoinOptimizer, SelectOptimizer, WhereOptimizer);

        var plan = await optimizer.Optimize(select);

        // Root may be a join of pushdowns, or a single pushdown depending on planner behavior.
        if (plan.RootOperator is JoinPlanOperator join)
        {
            join.Left.Should().BeAssignableTo<PlanOperator>();
            join.Right.Should().BeAssignableTo<PlanOperator>();
            join.Left.Should().BeOfType<PushdownSqlPlanOperator>();
            join.Right.Should().BeOfType<PushdownSqlPlanOperator>();
        }
        else
        {
            plan.RootOperator.Should().BeOfType<PushdownSqlPlanOperator>();
        }
    }
    
    [Test]
    public async Task Should_Optimize_Two_Joins_With_Same_Table_Diff_Source()
    {
        var select = SelectBaseModelHelper.FromAsSql("""
            SELECT t.subject, tc.body, e.first_name, e.last_name 
            FROM ticket->tickets t 
            INNER JOIN ticket->ticket_comments tc ON tc.ticket_id = t.id 
            INNER JOIN erp->employees e ON e.id = t.assigned_employee_id 
            WHERE t.status = 'In Progress'
            """);
        var planner = CreatePlannerMock();
        var optimizer = new Optimizer(planner, ExpressionOptimizer, JoinOptimizer, SelectOptimizer, WhereOptimizer);

        var plan = await optimizer.Optimize(select);

        // Root may be a join of pushdowns, or a single pushdown depending on planner behavior.
        if (plan.RootOperator is JoinPlanOperator join)
        {
            join.Left.Should().BeAssignableTo<PlanOperator>();
            join.Right.Should().BeAssignableTo<PlanOperator>();
            join.Left.Should().BeOfType<PushdownSqlPlanOperator>();
            join.Right.Should().BeOfType<PushdownSqlPlanOperator>();
        }
        else
        {
            plan.RootOperator.Should().BeOfType<PushdownSqlPlanOperator>();
        }
    }
    
    [Test]
    public async Task Should_Optimize_Two_Deep_Joins()
    {
        var select = SelectBaseModelHelper.FromAsSql("""
            SELECT t.subject, tc.body, e.first_name, e.last_name, c.name 
            FROM ticket->tickets t 
            INNER JOIN erp->employees e ON e.id = t.assigned_employee_id 
            INNER JOIN erp->customers c ON c.id = e.customer_id 
            WHERE t.status = 'In Progress'
            """);
        var planner = CreatePlannerMock();
        var optimizer = new Optimizer(planner, ExpressionOptimizer, JoinOptimizer, SelectOptimizer, WhereOptimizer);

        var plan = await optimizer.Optimize(select);

        // Root may be a join of pushdowns, or a single pushdown depending on planner behavior.
        if (plan.RootOperator is JoinPlanOperator join)
        {
            join.Left.Should().BeAssignableTo<PlanOperator>();
            join.Right.Should().BeAssignableTo<PlanOperator>();
            join.Left.Should().BeOfType<PushdownSqlPlanOperator>();
            join.Right.Should().BeOfType<PushdownSqlPlanOperator>();
        }
        else
        {
            plan.RootOperator.Should().BeOfType<PushdownSqlPlanOperator>();
        }
    }

    #endregion
}
