using System.Globalization;
using System.Text;

namespace Lait.Lexemes;

/// <summary>
/// Преобразует исходный текст Lait в последовательность токенов.
/// </summary>
public class Lexer
{
    private static readonly Dictionary<string, TokenType> Keywords = new()
    {
        { "bool", TokenType.Bool },
        { "else", TokenType.Else },
        { "false", TokenType.False },
        { "func", TokenType.Func },
        { "if", TokenType.If },
        { "int", TokenType.Int },
        { "new", TokenType.New },
        { "return", TokenType.Return },
        { "string", TokenType.String },
        { "struct", TokenType.Struct },
        { "true", TokenType.True },
        { "void", TokenType.Void },
        { "while", TokenType.While },
    };

    private readonly TextScanner _scanner;

    public Lexer(string code)
    {
        _scanner = new TextScanner(code);
    }

    /// <summary>
    /// Возвращает следующий токен исходного текста.
    /// </summary>
    public Token ParseToken()
    {
        SkipWhiteSpaces();

        if (_scanner.IsEnd())
        {
            return new Token(TokenType.EndOfFile);
        }

        char current = _scanner.Peek();
        if (char.IsAsciiLetter(current))
        {
            return ParseIdentifierOrKeyword();
        }

        if (char.IsAsciiDigit(current))
        {
            return ParseIntLiteral();
        }

        _scanner.Advance();
        return new Token(TokenType.Error, current.ToString());
    }

    /// <summary>
    /// Разбирает целочисленный литерал.
    /// </summary>
    private Token ParseIntLiteral()
    {
        StringBuilder digitsBuilder = new();

        for (
            char current = _scanner.Peek(); char.IsAsciiDigit(current); current = _scanner.Peek())
        {
            digitsBuilder.Append(current);
            _scanner.Advance();
        }

        string digits = digitsBuilder.ToString();
        if (digits.Length > 1 && digits[0] == '0')
        {
            return new Token(TokenType.Error, digits);
        }

        if (int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out int value))
        {
            return new Token(TokenType.IntLiteral, value);
        }

        return new Token(TokenType.Error, digits);
    }

    /// <summary>
    /// Разбирает идентификатор и проверяет, является ли он ключевым словом.
    /// </summary>
    private Token ParseIdentifierOrKeyword()
    {
        StringBuilder valueBuilder = new();

        for (
            char current = _scanner.Peek();
            char.IsAsciiLetter(current) || char.IsAsciiDigit(current) || current == '_';
            current = _scanner.Peek())
        {
            valueBuilder.Append(current);
            _scanner.Advance();
        }

        string value = valueBuilder.ToString();
        if (Keywords.TryGetValue(value, out TokenType type))
        {
            return new Token(type);
        }

        return new Token(TokenType.Identifier, value);
    }

    /// <summary>
    /// Пропускает пробельные символы, перечисленные в спецификации Lait.
    /// </summary>
    private void SkipWhiteSpaces()
    {
        while (IsWhiteSpace(_scanner.Peek()))
        {
            _scanner.Advance();
        }
    }

    private static bool IsWhiteSpace(char value)
    {
        return value is ' ' or '\t' or '\n' or '\r';
    }
}