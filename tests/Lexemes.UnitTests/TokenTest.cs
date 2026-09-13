using Lait.Lexemes;

using Xunit;

namespace Lait.Lexemes.UnitTests;

public class TokenTest
{
    [Theory]
    [MemberData(nameof(GetTokenEqualityData))]
    public void Equals_compares_type_and_value(Token first, Token second, bool expected)
    {
        bool actual = first.Equals(second);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [MemberData(nameof(GetTokenHashCodeData))]
    public void Equal_tokens_have_equal_hash_codes(Token first, Token second)
    {
        int firstHashCode = first.GetHashCode();
        int secondHashCode = second.GetHashCode();

        Assert.Equal(firstHashCode, secondHashCode);
    }

    [Theory]
    [MemberData(nameof(GetTokenToStringData))]
    public void To_string_formats_token(Token token, string expected)
    {
        string actual = token.ToString();

        Assert.Equal(expected, actual);
    }

    [Theory]
    [MemberData(nameof(GetTokenValueToIntData))]
    public void Token_value_returns_integer(TokenValue tokenValue, int expected)
    {
        int actual = tokenValue.ToInt();

        Assert.Equal(expected, actual);
    }

    public static TheoryData<Token, Token, bool> GetTokenEqualityData()
    {
        return new TheoryData<Token, Token, bool>
        {
            {
                new Token(TokenType.Plus),
                new Token(TokenType.Plus),
                true
            },
            {
                new Token(TokenType.Identifier, "value"),
                new Token(TokenType.Identifier, "value"),
                true
            },
            {
                new Token(TokenType.IntLiteral, 10),
                new Token(TokenType.IntLiteral, 10),
                true
            },
            {
                new Token(TokenType.Plus),
                new Token(TokenType.Minus),
                false
            },
            {
                new Token(TokenType.Identifier, "first"),
                new Token(TokenType.Identifier, "second"),
                false
            },
            {
                new Token(TokenType.Identifier, "10"),
                new Token(TokenType.IntLiteral, 10),
                false
            },
        };
    }

    public static TheoryData<Token, Token> GetTokenHashCodeData()
    {
        return new TheoryData<Token, Token>
        {
            {
                new Token(TokenType.Plus),
                new Token(TokenType.Plus)
            },
            {
                new Token(TokenType.Identifier, "value"),
                new Token(TokenType.Identifier, "value")
            },
            {
                new Token(TokenType.IntLiteral, 10),
                new Token(TokenType.IntLiteral, 10)
            },
        };
    }

    public static TheoryData<Token, string> GetTokenToStringData()
    {
        return new TheoryData<Token, string>
        {
            {
                new Token(TokenType.Plus),
                "Plus"
            },
            {
                new Token(TokenType.Identifier, "value"),
                "Identifier (value)"
            },
            {
                new Token(TokenType.IntLiteral, 10),
                "IntLiteral (10)"
            },
        };
    }

    public static TheoryData<TokenValue, int> GetTokenValueToIntData()
    {
        return new TheoryData<TokenValue, int>
        {
            {
                new TokenValue(0),
                0
            },
            {
                new TokenValue(2147483647),
                2147483647
            },
        };
    }
}