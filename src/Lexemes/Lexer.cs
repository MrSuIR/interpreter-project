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

    private static readonly Dictionary<char, char> EscapeSequences = new()
    {
        { 'n', '\n' },
        { 't', '\t' },
        { '"', '"' },
        { '\\', '\\' },
    };

    private readonly TextScanner _scanner;

    public Lexer(string code)
    {
        _scanner = new TextScanner(code);
    }

    /// <summary>
    /// Создаёт лексер, загрузив исходный файл целиком в память.
    /// </summary>
    public static Lexer FromFile(string path)
    {
        string code = File.ReadAllText(path);
        return new Lexer(code);
    }

    /// <summary>
    /// Возвращает следующий токен исходного текста.
    /// </summary>
    public Token ParseToken()
    {
        Token? commentError = SkipWhiteSpacesAndComments();
        if (commentError is not null)
        {
            return commentError;
        }

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

        if (current == '"')
        {
            return ParseStringLiteral();
        }

        // Разбираем операторы, скобки и разделители.
        switch (current)
        {
            case '+':
                _scanner.Advance();
                return new Token(TokenType.Plus);

            case '-':
                _scanner.Advance();
                return new Token(TokenType.Minus);

            case '*':
                _scanner.Advance();
                return new Token(TokenType.Multiply);

            case '/':
                _scanner.Advance();
                return new Token(TokenType.Divide);

            case '=':
                _scanner.Advance();
                if (_scanner.Peek() == '=')
                {
                    _scanner.Advance();
                    return new Token(TokenType.Equal);
                }

                return new Token(TokenType.Assign);

            case '!':
                _scanner.Advance();
                if (_scanner.Peek() == '=')
                {
                    _scanner.Advance();
                    return new Token(TokenType.NotEqual);
                }

                return new Token(TokenType.Not);

            case '<':
                _scanner.Advance();
                if (_scanner.Peek() == '=')
                {
                    _scanner.Advance();
                    return new Token(TokenType.LessThanOrEqual);
                }

                return new Token(TokenType.LessThan);

            case '>':
                _scanner.Advance();
                if (_scanner.Peek() == '=')
                {
                    _scanner.Advance();
                    return new Token(TokenType.GreaterThanOrEqual);
                }

                return new Token(TokenType.GreaterThan);

            case '&':
                _scanner.Advance();
                if (_scanner.Peek() == '&')
                {
                    _scanner.Advance();
                    return new Token(TokenType.And);
                }

                return new Token(TokenType.Error, current.ToString());

            case '|':
                _scanner.Advance();
                if (_scanner.Peek() == '|')
                {
                    _scanner.Advance();
                    return new Token(TokenType.Or);
                }

                return new Token(TokenType.Error, current.ToString());

            case '.':
                _scanner.Advance();
                return new Token(TokenType.Dot);

            case '(':
                _scanner.Advance();
                return new Token(TokenType.OpenParenthesis);

            case ')':
                _scanner.Advance();
                return new Token(TokenType.CloseParenthesis);

            case '{':
                _scanner.Advance();
                return new Token(TokenType.OpenBrace);

            case '}':
                _scanner.Advance();
                return new Token(TokenType.CloseBrace);

            case '[':
                _scanner.Advance();
                return new Token(TokenType.OpenBracket);

            case ']':
                _scanner.Advance();
                return new Token(TokenType.CloseBracket);

            case ',':
                _scanner.Advance();
                return new Token(TokenType.Comma);

            case ':':
                _scanner.Advance();
                return new Token(TokenType.Colon);

            case ';':
                _scanner.Advance();
                return new Token(TokenType.Semicolon);

            default:
                _scanner.Advance();
                return new Token(TokenType.Error, current.ToString());
        }
    }

    /// <summary>
    /// Разбирает строковый литерал и декодирует разрешённые escape-последовательности.
    /// </summary>
    private Token ParseStringLiteral()
    {
        StringBuilder valueBuilder = new();
        bool hasError = false;

        // Пропускаем открывающую кавычку.
        _scanner.Advance();

        while (!_scanner.IsEnd() && _scanner.Peek() != '"')
        {
            char current = _scanner.Peek();
            if (current == '\\')
            {
                ParseEscapeSequence(valueBuilder, ref hasError);
                continue;
            }

            if (char.IsControl(current))
            {
                hasError = true;
            }

            valueBuilder.Append(current);
            _scanner.Advance();
        }

        if (_scanner.IsEnd())
        {
            return new Token(TokenType.Error, valueBuilder.ToString());
        }

        // Пропускаем закрывающую кавычку.
        _scanner.Advance();

        return hasError
            ? new Token(TokenType.Error, valueBuilder.ToString())
            : new Token(TokenType.StringLiteral, valueBuilder.ToString());
    }

    /// <summary>
    /// Декодирует escape-последовательность или сохраняет неизвестную последовательность как ошибочную.
    /// </summary>
    private void ParseEscapeSequence(StringBuilder valueBuilder, ref bool hasError)
    {
        // Пропускаем обратную косую черту.
        _scanner.Advance();

        char escaped = _scanner.Peek();
        if (EscapeSequences.TryGetValue(escaped, out char decoded))
        {
            valueBuilder.Append(decoded);
            _scanner.Advance();
            return;
        }

        hasError = true;
        valueBuilder.Append('\\');

        if (!_scanner.IsEnd())
        {
            valueBuilder.Append(escaped);
            _scanner.Advance();
        }
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
    /// Пропускает пробельные символы и комментарии до начала следующей лексемы.
    /// </summary>
    private Token? SkipWhiteSpacesAndComments()
    {
        while (true)
        {
            SkipWhiteSpaces();

            if (_scanner.Peek() == '/' && _scanner.Peek(1) == '/')
            {
                SkipSingleLineComment();
                continue;
            }

            if (_scanner.Peek() == '/' && _scanner.Peek(1) == '*')
            {
                Token? error = SkipMultiLineComment();
                if (error is not null)
                {
                    return error;
                }

                continue;
            }

            return null;
        }
    }

    /// <summary>
    /// Пропускает однострочный комментарий до переноса строки или конца текста.
    /// </summary>
    private void SkipSingleLineComment()
    {
        // Пропускаем начало комментария.
        _scanner.Advance();
        _scanner.Advance();

        while (!_scanner.IsEnd() && _scanner.Peek() is not '\n' and not '\r')
        {
            _scanner.Advance();
        }
    }

    /// <summary>
    /// Пропускает многострочный комментарий или возвращает ошибку, если комментарий не завершён.
    /// </summary>
    private Token? SkipMultiLineComment()
    {
        StringBuilder commentBuilder = new();

        commentBuilder.Append('/');
        _scanner.Advance();
        commentBuilder.Append('*');
        _scanner.Advance();

        while (!_scanner.IsEnd())
        {
            if (_scanner.Peek() == '*' && _scanner.Peek(1) == '/')
            {
                _scanner.Advance();
                _scanner.Advance();
                return null;
            }

            commentBuilder.Append(_scanner.Peek());
            _scanner.Advance();
        }

        return new Token(TokenType.Error, commentBuilder.ToString());
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