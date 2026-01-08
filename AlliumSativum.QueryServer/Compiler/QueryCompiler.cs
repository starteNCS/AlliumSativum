using AlliumSativum.Exceptions;
using AlliumSativum.Parser;
using AlliumSativum.Parser.IntermediateModels;
using AlliumSativum.Semantic;
using AlliumSativum.Token;

namespace AlliumSativum.Compiler;

public static class QueryCompiler
{
    public static SelectBaseModel Compile(string query)
    {
        var tokens = Tokenizer.Tokenize(query);
        var selectModel = TokenQueryParser.Parse(tokens);
        if (selectModel == null)
        {
            throw new AsSqlException();
        }
        var semanticCheckedModel = SemanticTransformer.Transform(selectModel);
        
        return semanticCheckedModel;
    }
}
