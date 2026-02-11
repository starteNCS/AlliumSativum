using AlliumSativum.Optimize;

namespace QueryPlanner.Tests.Optimize.ExpressionNode;

public sealed class AttributeExtractionTests
{
    private static readonly ExpressionNodeOptimizer Optimizer = new();

    #region GetAttributesOfExpression Tests

    [Fact]
    public void GetAttributesOfExpression_SingleColumn_ReturnsAttribute()
    {
    private static readonly ExpressionNodeOptimizer ExpressionOptimizer = new();


}

        var result = Optimizer.GetTablesOfExpression(expr);

        result.Should().ContainSingle().Which.Should().Be(expectedTable);
    }

    [Fact]
    public void GetTablesOfExpression_MultipleDataSources_ReturnsDistinctTables()
    {
        var attr1 = "ds1::t1.c1".A();
        var attr2 = "ds2::t1.c2".A();
        var expr = attr1.Col().Eq(attr2.Col());

        var expectedTable1 = "ds1::t1".T();
        var expectedTable2 = "ds2::t1".T();

        var result = Optimizer.GetTablesOfExpression(expr);

        result.Should().HaveCount(2)
            .And.Contain(expectedTable1)
            .And.Contain(expectedTable2);
    }

    [Fact]
    public void GetTablesOfExpression_ComplexExpression_ReturnsAllUniqueTables()
    {
        var attr1 = "ds1::t1.id".A();
        var attr2 = "ds1::t2.id".A();
        var attr3 = "ds1::t1.name".A();
        var attr4 = "ds1::t3.value".A();

        var expr = attr1.Col().Eq(attr2.Col())
            .And(attr3.Col().Like("test%".Val()))
            .And(attr4.Col().Gt(0.Val()));

        var expectedTable1 = "ds1::t1".T();
        var expectedTable2 = "ds1::t2".T();
        var expectedTable3 = "ds1::t3".T();

        var result = Optimizer.GetTablesOfExpression(expr);

        result.Should().HaveCount(3)
            .And.Contain(expectedTable1)
            .And.Contain(expectedTable2)
            .And.Contain(expectedTable3);
    }

    [Fact]
    public void GetTablesOfExpression_DuplicateTableReferences_ReturnsUniqueTable()
    {
        var attr1 = "ds1::t1.c1".A();
        var attr2 = "ds1::t1.c2".A();
        var attr3 = "ds1::t1.c3".A();

        var expr = attr1.Col().Gt(10.Val())
            .And(attr2.Col().Lt(100.Val()))
            .And(attr3.Col());

        var expectedTable = "ds1::t1".T();

        var result = Optimizer.GetTablesOfExpression(expr);

        result.Should().ContainSingle().Which.Should().Be(expectedTable);
    }

    [Fact]
    public void GetTablesOfExpression_MixedDataSourcesAndTables_ReturnsAllUniqueTables()
    {
        var attr1 = "ds1::t1.c1".A();
        var attr2 = "ds1::t2.c2".A();
        var attr3 = "ds2::t1.c3".A();
        var attr4 = "ds2::t3.c4".A();

        var expr = attr1.Col().Eq(attr2.Col())
            .And(attr3.Col().Eq(attr4.Col()));

        var expectedTable1 = "ds1::t1".T();
        var expectedTable2 = "ds1::t2".T();
        var expectedTable3 = "ds2::t1".T();
        var expectedTable4 = "ds2::t3".T();

        var result = Optimizer.GetTablesOfExpression(expr);

        result.Should().HaveCount(4)
            .And.Contain(expectedTable1)
            .And.Contain(expectedTable2)
            .And.Contain(expectedTable3)
            .And.Contain(expectedTable4);
    }

    [Fact]
    public void GetTablesOfExpression_VariableMapping_ThrowsException()
    {
        var varMapping = new VariableMappingSpecifier("alias", "column");
        var expr = new VariableMappingExpressionNode { VariableMapping = varMapping };

        var act = () => Optimizer.GetTablesOfExpression(expr);

        act.Should().Throw<AsSqlOptimizeException>()
            .WithMessage("*alias*");
    }

    [Fact]
    public void GetTablesOfExpression_MultipleTablesMultipleDataSources_ReturnsAllTables()
    {
        var attr1 = "ds1::orders.id".A();
        var attr2 = "ds1::customers.id".A();
        var attr3 = "ds2::products.id".A();
        var attr4 = "ds3::inventory.quantity".A();

        var expr = attr1.Col().Eq(attr2.Col())
            .And(attr3.Col())
            .And(attr4.Col());

        var expectedTable1 = "ds1::orders".T();
        var expectedTable2 = "ds1::customers".T();
        var expectedTable3 = "ds2::products".T();
        var expectedTable4 = "ds3::inventory".T();

        var result = Optimizer.GetTablesOfExpression(expr);

        result.Should().HaveCount(4)
            .And.Contain(expectedTable1)
            .And.Contain(expectedTable2)
            .And.Contain(expectedTable3)
            .And.Contain(expectedTable4);
    }

    [Fact]
    public void GetTablesOfExpression_OrOperator_ReturnsAllTables()
    {
        var attr1 = "ds1::t1.c1".A();
        var attr2 = "ds1::t2.c2".A();
        var expr = attr1.Col().Or(attr2.Col());

        var expectedTable1 = "ds1::t1".T();
        var expectedTable2 = "ds1::t2".T();

        var result = Optimizer.GetTablesOfExpression(expr);

        result.Should().HaveCount(2)
            .And.Contain(expectedTable1)
            .And.Contain(expectedTable2);
    }

    [Fact]
    public void GetTablesOfExpression_ComplexMixedOperators_ReturnsAllUniqueTables()
    {
        var attr1 = "ds1::employees.dept_id".A();
        var attr2 = "ds1::departments.id".A();
        var attr3 = "ds1::employees.salary".A();
        var attr4 = "ds2::bonuses.amount".A();

        var expr = attr1.Col().Eq(attr2.Col())
            .And(attr3.Col().Gt(50000.Val()).Or(attr4.Col()));

        var expectedTable1 = "ds1::employees".T();
        var expectedTable2 = "ds1::departments".T();
        var expectedTable3 = "ds2::bonuses".T();

        var result = Optimizer.GetTablesOfExpression(expr);

        result.Should().HaveCount(3)
            .And.Contain(expectedTable1)
            .And.Contain(expectedTable2)
            .And.Contain(expectedTable3);
    }

    [Fact]
    public void GetTablesOfExpression_DeepNesting_ReturnsAllTables()
    {
        var attr1 = "ds1::t1.c1".A();
        var attr2 = "ds1::t2.c2".A();
        var attr3 = "ds1::t3.c3".A();
        var attr4 = "ds1::t4.c4".A();
        var attr5 = "ds1::t5.c5".A();

        var expr = attr1.Col().Eq(attr2.Col())
            .And(attr3.Col())
            .And(attr4.Col())
            .And(attr5.Col());

        var expectedTable1 = "ds1::t1".T();
        var expectedTable2 = "ds1::t2".T();
        var expectedTable3 = "ds1::t3".T();
        var expectedTable4 = "ds1::t4".T();
        var expectedTable5 = "ds1::t5".T();

        var result = Optimizer.GetTablesOfExpression(expr);

        result.Should().HaveCount(5)
            .And.Contain(expectedTable1)
            .And.Contain(expectedTable2)
            .And.Contain(expectedTable3)
            .And.Contain(expectedTable4)
            .And.Contain(expectedTable5);
    }

    [Fact]
    public void GetTablesOfExpression_SelfJoin_ReturnsOneTable()
    {
        var attr1 = "ds1::employees.manager_id".A();
        var attr2 = "ds1::employees.id".A();
        var expr = attr1.Col().Eq(attr2.Col());

        var expectedTable = "ds1::employees".T();

        var result = Optimizer.GetTablesOfExpression(expr);

        result.Should().ContainSingle().Which.Should().Be(expectedTable);
    }

    #endregion
}
