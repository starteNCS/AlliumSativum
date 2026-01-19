using AlliumSativum.Shared.Exceptions;
using FluentAssertions;

namespace ParserTests.Helpers;

public static class ShouldThrowHelper
{
    public static void ShouldThrowParseException(this Action action, string parseContent, string message)
    {
        action.Should().Throw<AsSqlParseException>()
            .Where(e => e.ParseContent == parseContent)
            .Where(e => e.AsMessage == message);
    }
    
    public static void ShouldThrowSemanticException(this Action action, string message)
    {
        action.Should().Throw<AsSqlSemanticException>()
            .Where(e => e.AsMessage == message);
    }
}
