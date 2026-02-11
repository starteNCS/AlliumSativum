using AlliumSativum.Optimize;
using AlliumSativum.Shared.Exceptions;
using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators;
using AlliumSativum.Shared.Models.IntermediateModels;
using AlliumSativum.Shared.Models.IntermediateModels.Expressions;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;
using FluentAssertions;
using QueryPlanner.Tests.Helpers;

namespace QueryPlanner.Tests.Optimize.Join;

public sealed class ConstructJoinPopTreeFromIntermediateJoinTreeTest
{
    private static readonly ExpressionNodeOptimizer ExpressionOptimizer = new();
    private static readonly JoinOptimizer JoinOptimizer = new(ExpressionOptimizer);

    [Test]
    public void Should_Return_Single_Plan_If_No_JoinTree()
    {
        var plans = new PopLookupTable();
        var table = new TableSpecifier("ticket", "tickets");
        var leafPlan = new PushdownSqlPlanOperator(Guid.NewGuid(), "SELECT * FROM tickets") { Cost = 1 };
        plans.Add(table, leafPlan);

        var root = JoinOptimizer.ConstructJoinPopTreeFromIntermediateJoinTree(null, plans);

        root.Should().BeSameAs(leafPlan);
    }

    [Test]
    public void Should_Throw_If_No_JoinTree_And_Multiple_Plans()
    {
        var plans = new PopLookupTable();
        plans.Add(new TableSpecifier("ticket", "tickets"), new PushdownSqlPlanOperator(Guid.NewGuid(), "SELECT * FROM tickets") { Cost = 1 });
        plans.Add(new TableSpecifier("erp", "employee"), new PushdownSqlPlanOperator(Guid.NewGuid(), "SELECT * FROM employee") { Cost = 1 });

        Action act = () => JoinOptimizer.ConstructJoinPopTreeFromIntermediateJoinTree(null, plans);
        act.ShouldThrowOptimizeException("Expected a intermediate join tree, as there are more than one plans");
    }

    [Test]
    public void Should_Construct_Join_Tree_From_Intermediate_Tree()
    {
        // Build intermediate join tree: join( tickets , employees ) on t.assigned_employee_id = e.id
        var intermediate = new IntermediateJoinNode
        {
            Left = new IntermediateJoinTreeTableSpecifier("ticket", "tickets"),
            Expression = new BinaryOperatorExpressionNode
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
            },
            Right = new IntermediateJoinTreeTableSpecifier("erp", "employee")
        };

        // Prepare plans for both tables
        var plans = new PopLookupTable();
        var ticket = new TableSpecifier("ticket", "tickets");
        var employee = new TableSpecifier("erp", "employee");
        var leftPlan = new PushdownSqlPlanOperator(Guid.NewGuid(), "SELECT * FROM tickets") { Cost = 1 };
        var rightPlan = new PushdownSqlPlanOperator(Guid.NewGuid(), "SELECT * FROM employee") { Cost = 1 };
        plans.Add(ticket, leftPlan);
        plans.Add(employee, rightPlan);

        var root = JoinOptimizer.ConstructJoinPopTreeFromIntermediateJoinTree(intermediate, plans);

        root.Should().BeOfType<JoinPlanOperator>();
        var join = (JoinPlanOperator)root;
        join.Left.Should().Be(leftPlan);
        join.Right.Should().Be(rightPlan);

        // Ensure lookup was consumed (plans removed) when building tree
        plans.Count.Should().Be(0);
    }
}
