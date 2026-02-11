using AlliumSativum.Shared.Exceptions;
using FluentAssertions;

namespace QueryPlanner.Tests.Helpers;

public static class ShouldThrowHelper
{
    extension(Action action)
    {
        public void ShouldThrowParseException(string parseContent, string message)
        {
            action.Should().Throw<AsSqlParseException>()
                .Where(e => e.ParseContent == parseContent)
                .Where(e => e.AsMessage == message);
        }

        public void ShouldThrowSemanticException(string message)
        {
            action.Should().Throw<AsSqlSemanticException>()
                .Where(e => e.AsMessage == message);
        }

        public void ShouldThrowOptimizeException(string message)
        {
            action.Should().Throw<AsSqlOptimizeException>()
                .Where(e => e.AsMessage == message);
        }
    }
}
