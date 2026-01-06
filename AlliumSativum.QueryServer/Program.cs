using AlliumSativum;
using AlliumSativum.Parser;
using AlliumSativum.STAR.NonTerminals.Access;

new QueryParser().Parse("SELECT erp->customers.name, erp->customers.customer_number FROM erp->customers WHERE erp->customers.name = 'test' AND erp->customers.customer_number = 123 OR erp->customers.name = 'peda'");