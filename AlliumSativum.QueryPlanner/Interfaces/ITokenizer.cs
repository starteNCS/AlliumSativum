namespace AlliumSativum.Interfaces;

public interface ITokenizer
{
    Stack<string> Tokenize(string query);
}