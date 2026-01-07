using AlliumSativum;
using AlliumSativum.Parser;
using AlliumSativum.STAR.NonTerminals.Access;
using AlliumSativum.Token;

var query = "SELECT c.name, erp->customers.customer_number FROM erp->customers c WHERE erp->customers.name = 'test mit space' AND erp->customers.customer_number = 123 OR erp->customers.name = 'peda'";
var tokens = Tokenizer.Tokenize(query);
var parsedResult = TokenQueryParser.Parse(tokens);
// todo: expand variable names

Console.WriteLine(parsedResult.ToString());