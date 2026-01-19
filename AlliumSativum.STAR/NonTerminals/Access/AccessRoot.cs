// using System.Diagnostics;
// using AlliumSativum.STAR.NonTerminals.Join;
//
// namespace AlliumSativum.STAR.NonTerminals.Access;
//
// public class AccessRoot : NonTerminal
// {
//     public int Repo { get; }
//
//     public AccessRoot(int repo)
//     {
//         Repo = repo;
//     }
//     
//     public override HashSet<Rule> Stars { get; init; } =
//     [
//         new Rule
//         {
//             Productions = (nonTerminal, select) => AccessStar(nonTerminal as AccessRoot),
//             Condition = (nonTerminal, select) => true
//         }
//     ];
//
//     private static List<ISymbol> AccessStar(AccessRoot? root)
//     {
//         Debug.Assert(root != null);
//         
//         return
//         [
//             new RepoAccess(root.Repo),
//         ];
//     }
// }