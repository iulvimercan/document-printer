using DocPrinter.Agent;
using DocPrinter.Core.Models;

namespace DocPrinter.Tests;

public class SessionParserTests
{
    [Theory]
    [InlineData("tyt", DocumentSession.Standard)]
    [InlineData("TYT", DocumentSession.Standard)]
    [InlineData("  Tyt ", DocumentSession.Standard)]
    [InlineData("ayt", DocumentSession.Sectioned)]
    [InlineData("AYT", DocumentSession.Sectioned)]
    public void Parses_supported_sessions(string value, DocumentSession expected)
    {
        Assert.Equal(expected, SessionParser.Parse(value));
    }

    [Theory]
    [InlineData("ydt")]
    [InlineData("YDT")]
    public void Rejects_ydt_as_unsupported(string value)
    {
        Assert.Throws<NotSupportedException>(() => SessionParser.Parse(value));
    }

    [Theory]
    [InlineData("")]
    [InlineData("xyz")]
    [InlineData(null)]
    public void Rejects_unknown_sessions(string? value)
    {
        Assert.Throws<ArgumentException>(() => SessionParser.Parse(value));
    }
}
