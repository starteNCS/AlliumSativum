using AlliumSativum.Parser;
using AlliumSativum.Semantic;
using AlliumSativum.Shared.Exceptions;
using AlliumSativum.Shared.Models.IntermediateModels;
using AlliumSativum.Token;

namespace AlliumSativum.Compiler;

public class QueryCompiler
{
    private readonly Tokenizer _tokenizer;
    private readonly TokenQueryParser _parser;
    private readonly SemanticTransformer _semanticTransformer;

    public QueryCompiler(Tokenizer tokenizer, TokenQueryParser parser, SemanticTransformer semanticTransformer)
    {
        _tokenizer = tokenizer;
        _parser = parser;
        _semanticTransformer = semanticTransformer;
    }
    
    public SelectBaseModel Compile(string query)
    {
        var tokens = _tokenizer.Tokenize(query);
        var selectModel = _parser.Parse(tokens);
        if (selectModel == null)
        {
            throw new AsSqlException();
        }
        _semanticTransformer.Transform(selectModel);
        
        return selectModel;
    }
}
