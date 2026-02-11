using AlliumSativum.Optimize;
using AlliumSativum.Shared.Models.IntermediateModels;
using FluentAssertions;
using ParserTests.Helpers;

namespace ParserTests.Optimize.Join;

public sealed class OnPremiseJoinTreeTests
{
    private static readonly ExpressionNodeOptimizer ExpressionOptimizer = new();
    private static readonly JoinOptimizer JoinOptimizer = new(ExpressionOptimizer);

    [Test]
    public void Should_Generated_Join_Tree_One_Join()
    {
        var select = SelectBaseModelHelper.FromAsSql("""
                                                     SELECT t.subject, c.name
                                                     FROM ticket->tickets t
                                                     INNER JOIN erp->customers c ON t.customer_id = c.id
                                                     """);

        var (joinTree, selectNeeded) = JoinOptimizer.ConstructOnPremiseJoin(select);

        joinTree.Should().NotBeNull();
        selectNeeded.Should().NotBeEmpty();

        joinTree.Should().BeOfType<IntermediateJoinNode>();
        var joinTreeNode =  (IntermediateJoinNode)joinTree;
        joinTreeNode.Left.Should().BeOfType<IntermediateJoinTreeTableSpecifier>();
        var left = (IntermediateJoinTreeTableSpecifier)joinTreeNode.Left;
        left.ToTableSpecifier().ShouldBeTable("erp", "customers");

        joinTreeNode.Right.Should().BeOfType<IntermediateJoinTreeTableSpecifier>();
        var right = (IntermediateJoinTreeTableSpecifier)joinTreeNode.Right;
        right.ToTableSpecifier().ShouldBeTable("ticket", "tickets");

        selectNeeded.ShouldContainAttributeSpecifier("ticket", "tickets", "customer_id");
        selectNeeded.ShouldContainAttributeSpecifier("erp", "customers", "id");
    }
    
    [Test]
    public void Should_Generated_Join_Tree_Two_Joins()
    {
        var select = SelectBaseModelHelper.FromAsSql("""
                                                     SELECT t.subject, c.name
                                                     FROM ticket->tickets t
                                                     INNER JOIN erp->customers c ON t.customer_id = c.id
                                                     INNER JOIN hr->employees e ON t.employee_id = e.id
                                                     """);

        var (joinTree, selectNeeded) = JoinOptimizer.ConstructOnPremiseJoin(select);

        joinTree.Should().NotBeNull();
        selectNeeded.Should().NotBeEmpty();

        joinTree.Should().BeOfType<IntermediateJoinNode>();
        var joinTreeNode = (IntermediateJoinNode)joinTree;
        joinTreeNode.Left.Should().BeOfType<IntermediateJoinNode>();
        var leftJoinTreeNode = (IntermediateJoinNode)joinTreeNode.Left;
        
        leftJoinTreeNode.Left.Should().BeOfType<IntermediateJoinTreeTableSpecifier>();
        var leftleft = (IntermediateJoinTreeTableSpecifier)leftJoinTreeNode.Left;
        leftleft.ToTableSpecifier().ShouldBeTable("erp", "customers");
        
        leftJoinTreeNode.Right.Should().BeOfType<IntermediateJoinTreeTableSpecifier>();
        var rightright = (IntermediateJoinTreeTableSpecifier)leftJoinTreeNode.Right;
        rightright.ToTableSpecifier().ShouldBeTable("ticket", "tickets");

        joinTreeNode.Right.Should().BeOfType<IntermediateJoinTreeTableSpecifier>();
        var right = (IntermediateJoinTreeTableSpecifier)joinTreeNode.Right;
        right.ToTableSpecifier().ShouldBeTable("hr", "employees");

        selectNeeded.ShouldContainAttributeSpecifier("ticket", "tickets", "customer_id");
        selectNeeded.ShouldContainAttributeSpecifier("erp", "customers", "id");
        selectNeeded.ShouldContainAttributeSpecifier("hr", "employees", "id");
    }
    
    [Test]
    public void Should_Not_Generate_For_One_Datasource()
    {
        var select = SelectBaseModelHelper.FromAsSql("""
                                                     SELECT t.subject, tc.body
                                                     FROM ticket->tickets t
                                                     INNER JOIN ticket->ticket_comments tc ON tc.ticket_id = t.id
                                                     """);

        var (joinTree, selectNeeded) = JoinOptimizer.ConstructOnPremiseJoin(select);

        joinTree.Should().BeNull();
        selectNeeded.Should().BeEmpty();
    }
    
    [Test]
    public void Should_Generate_Only_For_Mixed_Not_For_Same_Datasource()
    {
        var select = SelectBaseModelHelper.FromAsSql("""
                                                     SELECT t.subject, tc.body
                                                     FROM ticket->tickets t
                                                     INNER JOIN ticket->ticket_comments tc ON tc.ticket_id = t.id
                                                     INNER JOIN erp->customers c ON t.customer_id = c.id
                                                     """);

        var (joinTree, selectNeeded) = JoinOptimizer.ConstructOnPremiseJoin(select);

        joinTree.Should().NotBeNull();
        selectNeeded.Should().NotBeEmpty();
        
        joinTree.Should().BeOfType<IntermediateJoinNode>();
        var joinTreeNode =  (IntermediateJoinNode)joinTree;
        joinTreeNode.Left.Should().BeOfType<IntermediateJoinTreeTableSpecifier>();
        var left = (IntermediateJoinTreeTableSpecifier)joinTreeNode.Left;
        left.ToTableSpecifier().ShouldBeTable("erp", "customers");

        joinTreeNode.Right.Should().BeOfType<IntermediateJoinTreeTableSpecifier>();
        var right = (IntermediateJoinTreeTableSpecifier)joinTreeNode.Right;
        right.ToTableSpecifier().ShouldBeTable("ticket", "tickets");

        selectNeeded.Count.Should().Be(2);
        selectNeeded.ShouldContainAttributeSpecifier("ticket", "tickets", "customer_id");
        selectNeeded.ShouldContainAttributeSpecifier("erp", "customers", "id");
    }
    
    [Test]
    public void Should_Generate_Only_Across_Datasource_Border_Once()
    {
        // Optimizer assumes all joins can be pushed down
        // therefore we expect exactly one join, between the ticket system and the erp system
        // (as mentioned, because the optimizer wants to hand off the join within a datasource)
        // aasumes only INNER JOIN
        var select = SelectBaseModelHelper.FromAsSql("""
                                                     SELECT t.subject, tc.body, e.first_name, e.last_name, c.name 
                                                     FROM ticket->tickets t 
                                                         INNER JOIN ticket->ticket_comments tc ON tc.ticket_id = t.id
                                                         INNER JOIN erp->employees e ON e.id = t.assigned_employee_id 
                                                         INNER JOIN erp->customers c ON c.id = e.customer_id
                                                     WHERE t.status = 'In Progress'
                                                     """);

        var (joinTree, selectNeeded) = JoinOptimizer.ConstructOnPremiseJoin(select);

        joinTree.Should().NotBeNull();
        selectNeeded.Should().NotBeEmpty();
        
        joinTree.Should().BeOfType<IntermediateJoinNode>();
        var joinTreeNode =  (IntermediateJoinNode)joinTree;
        joinTreeNode.Left.Should().BeOfType<IntermediateJoinTreeTableSpecifier>();
        var left = (IntermediateJoinTreeTableSpecifier)joinTreeNode.Left;
        left.ToTableSpecifier().ShouldBeTable("erp", "employees");

        joinTreeNode.Right.Should().BeOfType<IntermediateJoinTreeTableSpecifier>();
        var right = (IntermediateJoinTreeTableSpecifier)joinTreeNode.Right;
        right.ToTableSpecifier().ShouldBeTable("ticket", "tickets");

        selectNeeded.Count.Should().Be(2);
        selectNeeded.ShouldContainAttributeSpecifier("ticket", "tickets", "assigned_employee_id");
        selectNeeded.ShouldContainAttributeSpecifier("erp", "employees", "id");
    }
}
