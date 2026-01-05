using AlliumSativum.Parser.Exceptions;
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
}
