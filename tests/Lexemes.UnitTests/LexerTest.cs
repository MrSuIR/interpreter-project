using Lait.Lexemes;

using Xunit;

namespace Lait.Lexemes.UnitTests;

public class LexerTest
{
    [Fact]
    public void Can_tokenize_source_file()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "TestData", "program.lait");
        List<Token> expected =
        [
            new Token(TokenType.Func),
            new Token(TokenType.Identifier, "main"),
            new Token(TokenType.OpenParenthesis),
            new Token(TokenType.CloseParenthesis),
            new Token(TokenType.Colon),
            new Token(TokenType.Void),
            new Token(TokenType.OpenBrace),
            new Token(TokenType.String),
            new Token(TokenType.Identifier, "message"),
            new Token(TokenType.Assign),
            new Token(TokenType.StringLiteral, "Привет\nLait"),
            new Token(TokenType.Semicolon),
            new Token(TokenType.Identifier, "print"),
            new Token(TokenType.OpenParenthesis),
            new Token(TokenType.Identifier, "message"),
            new Token(TokenType.CloseParenthesis),
            new Token(TokenType.Semicolon),
            new Token(TokenType.CloseBrace),
        ];

        List<Token> actual = Tokenize(Lexer.FromFile(path));

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void From_file_throws_when_file_does_not_exist()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "TestData", "missing.lait");
        Action action = () => Lexer.FromFile(path);

        Assert.Throws<FileNotFoundException>(action);
    }

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
    [MemberData(nameof(GetTokenizeStringLiteralsData))]
    public void Can_tokenize_string_literals(string code, List<Token> expected)
    {
        List<Token> actual = Tokenize(code);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [MemberData(nameof(GetSkipCommentsData))]
    public void Can_skip_comments(string code, List<Token> expected)
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
    [MemberData(nameof(GetTokenizeProgramFragmentsData))]
    public void Can_tokenize_program_fragments(string code, List<Token> expected)
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
            {
                "// comment",
                0
            },
            {
                "/* comment */",
                0
            },
            {
                " \t/* first */\n// second",
                0
            },
            {
                "/* unfinished",
                1
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

    public static TheoryData<string, List<Token>> GetTokenizeStringLiteralsData()
    {
        return new TheoryData<string, List<Token>>
        {
            {
                "\"\" \"Lait\"",
                [
                    new Token(TokenType.StringLiteral, string.Empty),
                    new Token(TokenType.StringLiteral, "Lait"),
                ]
            },
            {
                "\"Привет, Lait!\"",
                [
                    new Token(TokenType.StringLiteral, "Привет, Lait!"),
                ]
            },
            {
                "\"First line\\nSecond line\"",
                [
                    new Token(TokenType.StringLiteral, "First line\nSecond line"),
                ]
            },
            {
                "\"a\\tb\"",
                [
                    new Token(TokenType.StringLiteral, "a\tb"),
                ]
            },
            {
                "\"Say \\\"Hello\\\"\"",
                [
                    new Token(TokenType.StringLiteral, "Say \"Hello\""),
                ]
            },
            {
                "\"C:\\\\code\"",
                [
                    new Token(TokenType.StringLiteral, "C:\\code"),
                ]
            },
            {
                "\"// /* */\"",
                [
                    new Token(TokenType.StringLiteral, "// /* */"),
                ]
            },
            {
                "\"\\q\"",
                [
                    new Token(TokenType.Error, "\\q"),
                ]
            },
            {
                "\"Lait",
                [
                    new Token(TokenType.Error, "Lait"),
                ]
            },
            {
                "\"First\nSecond\"",
                [
                    new Token(TokenType.Error, "First\nSecond"),
                ]
            },
            {
                "\"a\u0001b\"",
                [
                    new Token(TokenType.Error, "a\u0001b"),
                ]
            },
            {
                "\"first\"\"second\"",
                [
                    new Token(TokenType.StringLiteral, "first"),
                    new Token(TokenType.StringLiteral, "second"),
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

    public static TheoryData<string, List<Token>> GetSkipCommentsData()
    {
        return new TheoryData<string, List<Token>>
        {
            {
                "first // comment\nsecond",
                [
                    new Token(TokenType.Identifier, "first"),
                    new Token(TokenType.Identifier, "second"),
                ]
            },
            {
                "first // comment\rsecond",
                [
                    new Token(TokenType.Identifier, "first"),
                    new Token(TokenType.Identifier, "second"),
                ]
            },
            {
                "first // comment",
                [
                    new Token(TokenType.Identifier, "first"),
                ]
            },
            {
                "//",
                []
            },
            {
                "first /* comment */ second",
                [
                    new Token(TokenType.Identifier, "first"),
                    new Token(TokenType.Identifier, "second"),
                ]
            },
            {
                "first /* line one\nline two */ second",
                [
                    new Token(TokenType.Identifier, "first"),
                    new Token(TokenType.Identifier, "second"),
                ]
            },
            {
                "/* first */ value */",
                [
                    new Token(TokenType.Identifier, "value"),
                    new Token(TokenType.Multiply),
                    new Token(TokenType.Divide),
                ]
            },
            {
                " \t/* first */\n// second\r/* third */ value",
                [
                    new Token(TokenType.Identifier, "value"),
                ]
            },
            {
                "left/right // comment\nnext/* comment */last",
                [
                    new Token(TokenType.Identifier, "left"),
                    new Token(TokenType.Divide),
                    new Token(TokenType.Identifier, "right"),
                    new Token(TokenType.Identifier, "next"),
                    new Token(TokenType.Identifier, "last"),
                ]
            },
            {
                "/* outer /* inner */ value",
                [
                    new Token(TokenType.Identifier, "value"),
                ]
            },
            {
                "/* unfinished",
                [
                    new Token(TokenType.Error, "/* unfinished"),
                ]
            },
        };
    }

    public static TheoryData<string, List<Token>> GetTokenizeProgramFragmentsData()
    {
        return new TheoryData<string, List<Token>>
        {
            {
                """
                func add(left: int, right: int): int {
                    return left + right;
                }
                """,
                [
                    new Token(TokenType.Func),
                    new Token(TokenType.Identifier, "add"),
                    new Token(TokenType.OpenParenthesis),
                    new Token(TokenType.Identifier, "left"),
                    new Token(TokenType.Colon),
                    new Token(TokenType.Int),
                    new Token(TokenType.Comma),
                    new Token(TokenType.Identifier, "right"),
                    new Token(TokenType.Colon),
                    new Token(TokenType.Int),
                    new Token(TokenType.CloseParenthesis),
                    new Token(TokenType.Colon),
                    new Token(TokenType.Int),
                    new Token(TokenType.OpenBrace),
                    new Token(TokenType.Return),
                    new Token(TokenType.Identifier, "left"),
                    new Token(TokenType.Plus),
                    new Token(TokenType.Identifier, "right"),
                    new Token(TokenType.Semicolon),
                    new Token(TokenType.CloseBrace),
                ]
            },
            {
                """
                if (value >= 10 && value != 20) {
                    print("ok");
                } else {
                    print("not ok");
                }
                """,
                [
                    new Token(TokenType.If),
                    new Token(TokenType.OpenParenthesis),
                    new Token(TokenType.Identifier, "value"),
                    new Token(TokenType.GreaterThanOrEqual),
                    new Token(TokenType.IntLiteral, 10),
                    new Token(TokenType.And),
                    new Token(TokenType.Identifier, "value"),
                    new Token(TokenType.NotEqual),
                    new Token(TokenType.IntLiteral, 20),
                    new Token(TokenType.CloseParenthesis),
                    new Token(TokenType.OpenBrace),
                    new Token(TokenType.Identifier, "print"),
                    new Token(TokenType.OpenParenthesis),
                    new Token(TokenType.StringLiteral, "ok"),
                    new Token(TokenType.CloseParenthesis),
                    new Token(TokenType.Semicolon),
                    new Token(TokenType.CloseBrace),
                    new Token(TokenType.Else),
                    new Token(TokenType.OpenBrace),
                    new Token(TokenType.Identifier, "print"),
                    new Token(TokenType.OpenParenthesis),
                    new Token(TokenType.StringLiteral, "not ok"),
                    new Token(TokenType.CloseParenthesis),
                    new Token(TokenType.Semicolon),
                    new Token(TokenType.CloseBrace),
                ]
            },
            {
                """
                while (items[index].active) {
                    index = index + 1; // Следующий элемент.
                }
                """,
                [
                    new Token(TokenType.While),
                    new Token(TokenType.OpenParenthesis),
                    new Token(TokenType.Identifier, "items"),
                    new Token(TokenType.OpenBracket),
                    new Token(TokenType.Identifier, "index"),
                    new Token(TokenType.CloseBracket),
                    new Token(TokenType.Dot),
                    new Token(TokenType.Identifier, "active"),
                    new Token(TokenType.CloseParenthesis),
                    new Token(TokenType.OpenBrace),
                    new Token(TokenType.Identifier, "index"),
                    new Token(TokenType.Assign),
                    new Token(TokenType.Identifier, "index"),
                    new Token(TokenType.Plus),
                    new Token(TokenType.IntLiteral, 1),
                    new Token(TokenType.Semicolon),
                    new Token(TokenType.CloseBrace),
                ]
            },
        };
    }

    private static List<Token> Tokenize(string code)
    {
        Lexer lexer = new(code);
        return Tokenize(lexer);
    }

    private static List<Token> Tokenize(Lexer lexer)
    {
        List<Token> results = [];

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