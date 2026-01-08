using AlliumSativum;
using AlliumSativum.Compiler;
using AlliumSativum.Parser;
using AlliumSativum.STAR.NonTerminals.Access;
using AlliumSativum.Token;

var compiledResult = QueryCompiler.Compile("SELECT c.name, erp->customers.customer_number FROM erp->customers c WHERE erp->customers.name = 'test mit space' AND erp->customers.customer_number = 123 OR erp->customers.name = 'peda'");

Console.WriteLine(compiledResult);