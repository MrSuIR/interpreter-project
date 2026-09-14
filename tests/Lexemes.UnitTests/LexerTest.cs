using Lait.Lexemes;

using Xunit;

namespace Lait.Lexemes.UnitTests;

public class LexerTest
{
    [Theory]
    [MemberData(nameof(GetTokenizeIdentifiersAndKeywordsData))]
    public void Can_tokenize_identifiers_and_keywords(string code, List<Token> expected)
    {
        List<Token> actual = Tokenize(code);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [MemberData(nameof(GetTokenizeIntLiteralsData))]
    public void Can_tokenize_integer_literals(string code, List<Token> expected)
    {
        List<Token> actual = Tokenize(code);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [MemberData(nameof(GetTokenizeOperatorsAndPunctuationData))]
    public void Can_tokenize_operators_and_punctuation(string code, List<Token> expected)
    {
        List<Token> actual = Tokenize(code);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [MemberData(nameof(GetEndOfFileData))]
    public void Parse_token_returns_end_of_file(string code, int tokenCount)
    {
        Lexer lexer = new(code);

        for (int i = 0; i < tokenCount; i++)
        {
            lexer.ParseToken();
        }

        Token actual = lexer.ParseToken();

        Assert.Equal(new Token(TokenType.EndOfFile), actual);
    }

    public static TheoryData<string, List<Token>> GetTokenizeIdentifiersAndKeywordsData()
    {
        return new TheoryData<string, List<Token>>
        {
            {
                "x value userName",
                [
                    new Token(TokenType.Identifier, "x"),
                    new Token(TokenType.Identifier, "value"),
                    new Token(TokenType.Identifier, "userName"),
                ]
            },
            {
                "value2 Point2D read_value",
                [
                    new Token(TokenType.Identifier, "value2"),
                    new Token(TokenType.Identifier, "Point2D"),
                    new Token(TokenType.Identifier, "read_value"),
                ]
            },
            {
                "value Value VALUE",
                [
                    new Token(TokenType.Identifier, "value"),
                    new Token(TokenType.Identifier, "Value"),
                    new Token(TokenType.Identifier, "VALUE"),
                ]
            },
            {
                "bool else false func if int new return string struct true void while",
                [
                    new Token(TokenType.Bool),
                    new Token(TokenType.Else),
                    new Token(TokenType.False),
                    new Token(TokenType.Func),
                    new Token(TokenType.If),
                    new Token(TokenType.Int),
                    new Token(TokenType.New),
                    new Token(TokenType.Return),
                    new Token(TokenType.String),
                    new Token(TokenType.Struct),
                    new Token(TokenType.True),
                    new Token(TokenType.Void),
                    new Token(TokenType.While),
                ]
            },
            {
                "Bool If TRUE While",
                [
                    new Token(TokenType.Identifier, "Bool"),
                    new Token(TokenType.Identifier, "If"),
                    new Token(TokenType.Identifier, "TRUE"),
                    new Token(TokenType.Identifier, "While"),
                ]
            },
            {
                "main print readInt readString",
                [
                    new Token(TokenType.Identifier, "main"),
                    new Token(TokenType.Identifier, "print"),
                    new Token(TokenType.Identifier, "readInt"),
                    new Token(TokenType.Identifier, "readString"),
                ]
            },
            {
                "_value",
                [
                    new Token(TokenType.Error, "_"),
                    new Token(TokenType.Identifier, "value"),
                ]
            },
            {
                "имя",
                [
                    new Token(TokenType.Error, "и"),
                    new Token(TokenType.Error, "м"),
                    new Token(TokenType.Error, "я"),
                ]
            },
            {
                "first \tsecond\nthird\rfourth",
                [
                    new Token(TokenType.Identifier, "first"),
                    new Token(TokenType.Identifier, "second"),
                    new Token(TokenType.Identifier, "third"),
                    new Token(TokenType.Identifier, "fourth"),
                ]
            },
        };
    }

    public static TheoryData<string, int> GetEndOfFileData()
    {
        return new TheoryData<string, int>
        {
            {
                string.Empty,
                0
            },
            {
                " \t\n\r",
                0
            },
            {
                "value",
                1
            },
            {
                "int value",
                2
            },
        };
    }

    public static TheoryData<string, List<Token>> GetTokenizeIntLiteralsData()
    {
        return new TheoryData<string, List<Token>>
        {
            {
                "0",
                [
                    new Token(TokenType.IntLiteral, 0),
                ]
            },
            {
                "7 1250",
                [
                    new Token(TokenType.IntLiteral, 7),
                    new Token(TokenType.IntLiteral, 1250),
                ]
            },
            {
                "2147483647",
                [
                    new Token(TokenType.IntLiteral, 2147483647),
                ]
            },
            {
                "00 007",
                [
                    new Token(TokenType.Error, "00"),
                    new Token(TokenType.Error, "007"),
                ]
            },
            {
                "2147483648",
                [
                    new Token(TokenType.Error, "2147483648"),
                ]
            },
        };
    }

    public static TheoryData<string, List<Token>> GetTokenizeOperatorsAndPunctuationData()
    {
        return new TheoryData<string, List<Token>>
        {
            {
                "+ - * /",
                [
                    new Token(TokenType.Plus),
                    new Token(TokenType.Minus),
                    new Token(TokenType.Multiply),
                    new Token(TokenType.Divide),
                ]
            },
            {
                "= == != !",
                [
                    new Token(TokenType.Assign),
                    new Token(TokenType.Equal),
                    new Token(TokenType.NotEqual),
                    new Token(TokenType.Not),
                ]
            },
            {
                "< <= > >=",
                [
                    new Token(TokenType.LessThan),
                    new Token(TokenType.LessThanOrEqual),
                    new Token(TokenType.GreaterThan),
                    new Token(TokenType.GreaterThanOrEqual),
                ]
            },
            {
                "&& || .",
                [
                    new Token(TokenType.And),
                    new Token(TokenType.Or),
                    new Token(TokenType.Dot),
                ]
            },
            {
                "=== !== <<=",
                [
                    new Token(TokenType.Equal),
                    new Token(TokenType.Assign),
                    new Token(TokenType.NotEqual),
                    new Token(TokenType.Assign),
                    new Token(TokenType.LessThan),
                    new Token(TokenType.LessThanOrEqual),
                ]
            },
            {
                "& |",
                [
                    new Token(TokenType.Error, "&"),
                    new Token(TokenType.Error, "|"),
                ]
            },
            {
                "(){}[]",
                [
                    new Token(TokenType.OpenParenthesis),
                    new Token(TokenType.CloseParenthesis),
                    new Token(TokenType.OpenBrace),
                    new Token(TokenType.CloseBrace),
                    new Token(TokenType.OpenBracket),
                    new Token(TokenType.CloseBracket),
                ]
            },
            {
                ",:;",
                [
                    new Token(TokenType.Comma),
                    new Token(TokenType.Colon),
                    new Token(TokenType.Semicolon),
                ]
            },
            {
                "value=10;",
                [
                    new Token(TokenType.Identifier, "value"),
                    new Token(TokenType.Assign),
                    new Token(TokenType.IntLiteral, 10),
                    new Token(TokenType.Semicolon),
                ]
            },
            {
                "-15",
                [
                    new Token(TokenType.Minus),
                    new Token(TokenType.IntLiteral, 15),
                ]
            },
        };
    }

    private static List<Token> Tokenize(string code)
    {
        List<Token> results = [];
        Lexer lexer = new(code);

        for (
            Token token = lexer.ParseToken();
            token.Type != TokenType.EndOfFile;
            token = lexer.ParseToken())
        {
            results.Add(token);
        }

        return results;
    }
}