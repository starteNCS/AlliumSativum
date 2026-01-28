using AlliumSativum.Optimize;
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
    private readonly Optimizer _optimizer;

    public QueryCompiler(Tokenizer tokenizer, TokenQueryParser parser, SemanticTransformer semanticTransformer, Optimizer optimizer)
    {
        _tokenizer = tokenizer;
        _parser = parser;
        _semanticTransformer = semanticTransformer;
        _optimizer = optimizer;
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
        var _ = _optimizer.Optimize(selectModel);
        
        return selectModel;
    }
}
