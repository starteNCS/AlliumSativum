using AlliumSativum.Optimize;
using AlliumSativum.Shared.Models.IntermediateModels;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;
using FluentAssertions;
using QueryPlanner.Tests.Helpers;

namespace QueryPlanner.Tests.Optimize.Select;

public sealed class AppendHiddenSelectsTest
{
    private static readonly SelectOptimizer SelectOptimizer = new ();

    [Test]
    public void Should_Append_Select_One_Table()
    {
        List<SelectBaseModel> selectBaseModel = [new()
        {
            From = new TableSpecifier("ticket", "tickets")
        }];
        var newSelects = new List<AttributeSpecifier>
        {
            new ("ticket", "tickets", "assigned_employee_id"),
        };
        
        var selects = SelectOptimizer.AppendComputationalSelects(selectBaseModel, newSelects);

        selects.Count.Should().Be(1);
        var select = selects[0];
        select.Select.ShouldContainAttributeSpecifier("ticket", "tickets", "assigned_employee_id", true);
    }
    
    [Test]
    public void Should_Append_Select_One_Table_Visible_When_Already_Existing()
    {
        var table = new TableSpecifier("ticket", "tickets");
        var attribute = table.ToAttributeSpecifier("id");
        List<SelectBaseModel> selectBaseModel = [new()
        {
            From = table,
            Select = [attribute]
        }];
        List<AttributeSpecifier> newSelects = [attribute];
        
        var selects = SelectOptimizer.AppendComputationalSelects(selectBaseModel, newSelects);

        selects.Count.Should().Be(1);
        var select = selects[0];
        select.Select.ShouldContainAttributeSpecifier(attribute, isHidden: false);
    }
    
    [Test]
    public void Should_Append_Select_Two_Tables()
    {
        var tickets = new TableSpecifier("ticket", "tickets");
        var employees = new TableSpecifier("erp", "employees");
        List<SelectBaseModel> selectBaseModel = [
            new() { From = tickets, },
            new() { From = employees, }
        ];
        var newSelects = new List<AttributeSpecifier>
        {
            new ("ticket", "tickets", "assigned_employee_id"),
            new ("erp", "employees", "id")
        };
        
        var selects = SelectOptimizer.AppendComputationalSelects(selectBaseModel, newSelects);

        selects.Count.Should().Be(2);
        var selectTicket = selects.First(s => s.From.Equals(tickets));
        selectTicket.Select.ShouldContainAttributeSpecifier(tickets.ToAttributeSpecifier("assigned_employee_id"), true);
        
        var selectErp = selects.First(s => s.From.Equals(employees));
        selectErp.Select.ShouldContainAttributeSpecifier(employees.ToAttributeSpecifier("id"), true);
    }
    
    [Test]
    public void Should_Throw_When_No_Select_Found()
    {
        List<SelectBaseModel> selectBaseModel = [
            new() { From = new TableSpecifier("ticket", "tickets"), },
        ];
        var newSelects = new List<AttributeSpecifier>
        {
            new ("erp", "employees", "id")
        };
        
        Action action = () => SelectOptimizer.AppendComputationalSelects(selectBaseModel, newSelects);
        action.ShouldThrowOptimizeException("Expected to find select model to push hidden attribute to");
    }
}
