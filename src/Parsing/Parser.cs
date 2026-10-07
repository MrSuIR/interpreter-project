using System.Globalization;

using Lait.Lexemes;

namespace Lait.Parsing;

/// <summary>
/// Рекурсивный спуск по EBNF с непосредственным выполнением выражений.
/// </summary>
public class Parser
{
    private const int MaximumNesting = 256;
    private readonly Lexer _lexer;
    private readonly TextReader _input;
    private readonly TextWriter _output;
    private Token _current = new(TokenType.EndOfFile);
    private int _tokenNumber;
    private int _nesting;
    private bool _started;

    public Parser(Lexer lexer, TextReader input, TextWriter output)
    {
        _lexer = lexer;
        _input = input;
        _output = output;
    }

    public void ParseProgram()
    {
        if (_started)
        {
            throw new InvalidOperationException("Парсер допускает только один разбор программы.");
        }

        _started = true;
        Advance();
        Expect(TokenType.Func);
        ExpectIdentifier("main");
        Expect(TokenType.OpenParenthesis);
        Expect(TokenType.CloseParenthesis);
        Expect(TokenType.Colon);
        Expect(TokenType.Void);
        Expect(TokenType.OpenBrace);
        while (_current.Type != TokenType.CloseBrace)
        {
            ParseStatement();
        }

        Expect(TokenType.CloseBrace);
        Expect(TokenType.EndOfFile);
    }

    private void ParseStatement()
    {
        if (IsIdentifier("print"))
        {
            ParsePrintStatement();
        }
        else
        {
            ParseExpression();
        }

        Expect(TokenType.Semicolon);
    }

    private void ParsePrintStatement()
    {
        ExpectIdentifier("print");
        Expect(TokenType.OpenParenthesis);
        int value = ParseExpression();
        Expect(TokenType.CloseParenthesis);
        if (_current.Type != TokenType.Semicolon)
        {
            throw SyntaxError("Ожидалась точка с запятой.");
        }

        _output.WriteLine(value.ToString(CultureInfo.InvariantCulture));
        _output.Flush();
    }

    private int ParseExpression()
    {
        return ParseAdditiveExpression();
    }

    private int ParseAdditiveExpression()
    {
        int value = ParseMultiplicativeExpression();
        while (_current.Type is TokenType.Plus or TokenType.Minus)
        {
            TokenType operation = _current.Type;
            Advance();
            int right = ParseMultiplicativeExpression();
            value = operation == TokenType.Plus ? checked(value + right) : checked(value - right);
        }

        return value;
    }

    private int ParseMultiplicativeExpression()
    {
        int value = ParseUnaryExpression();
        while (_current.Type is TokenType.Multiply or TokenType.Divide)
        {
            TokenType operation = _current.Type;
            Advance();
            int right = ParseUnaryExpression();
            value = operation == TokenType.Multiply ? checked(value * right) : checked(value / right);
        }

        return value;
    }

    private int ParseUnaryExpression()
    {
        _nesting++;
        if (_nesting > MaximumNesting)
        {
            throw SyntaxError($"Превышена допустимая вложенность выражений ({MaximumNesting}).");
        }

        int value;
        if (_current.Type == TokenType.Minus)
        {
            Advance();
            value = checked(-ParseUnaryExpression());
        }
        else
        {
            value = ParsePrimaryExpression();
        }

        _nesting--;
        return value;
    }

    private int ParsePrimaryExpression()
    {
        if (_current.Type == TokenType.IntLiteral)
        {
            int value = _current.Value!.ToInt();
            Advance();
            return value;
        }
        else if (_current.Type == TokenType.OpenParenthesis)
        {
            Advance();
            int value = ParseExpression();
            Expect(TokenType.CloseParenthesis);
            return value;
        }
        else if (IsIdentifier("readInt"))
        {
            return ParseReadExpression();
        }
        else if (IsIdentifier("abs"))
        {
            return ParseAbsoluteExpression();
        }
        else if (IsIdentifier("min") || IsIdentifier("max"))
        {
            return ParseExtremaExpression();
        }
        else
        {
            throw SyntaxError("Ожидалось число, скобка или вызов readInt, abs, min, max.");
        }
    }

    private int ParseReadExpression()
    {
        ExpectIdentifier("readInt");
        Expect(TokenType.OpenParenthesis);
        Expect(TokenType.CloseParenthesis);
        return ReadInteger();
    }

    private int ParseAbsoluteExpression()
    {
        ExpectIdentifier("abs");
        Expect(TokenType.OpenParenthesis);
        int value = ParseExpression();
        Expect(TokenType.CloseParenthesis);
        return Math.Abs(value);
    }

    private int ParseExtremaExpression()
    {
        bool minimum = IsIdentifier("min");
        Advance();
        Expect(TokenType.OpenParenthesis);
        int left = ParseExpression();
        Expect(TokenType.Comma);
        int right = ParseExpression();
        Expect(TokenType.CloseParenthesis);
        return minimum ? Math.Min(left, right) : Math.Max(left, right);
    }

    private bool IsIdentifier(string name)
    {
        return _current.Type == TokenType.Identifier && _current.Value!.ToString() == name;
    }

    private void ExpectIdentifier(string name)
    {
        if (!IsIdentifier(name))
        {
            throw SyntaxError($"Ожидалось имя '{name}'.");
        }

        Advance();
    }

    private void Expect(TokenType type)
    {
        if (_current.Type != type)
        {
            throw SyntaxError($"Ожидался токен {type}.");
        }

        if (type != TokenType.EndOfFile)
        {
            Advance();
        }
    }

    private void Advance()
    {
        _current = _lexer.ParseToken();
        _tokenNumber++;
        if (_current.Type == TokenType.Error)
        {
            throw SyntaxError("Лексическая ошибка.");
        }
    }

    private FormatException SyntaxError(string message)
    {
        return new FormatException($"Токен {_tokenNumber} ({_current}): {message}");
    }

    private int ReadInteger()
    {
        string? line = _input.ReadLine();
        if (line is null)
        {
            throw new InvalidOperationException("readInt(): достигнут конец ввода.");
        }

        string text = line.Trim(' ', '\t');
        int start = text.Length > 0 && text[0] is '+' or '-' ? 1 : 0;
        if (start == text.Length)
        {
            throw new InvalidOperationException("readInt(): ожидалось целое число.");
        }

        for (int index = start; index < text.Length; index++)
        {
            if (!char.IsAsciiDigit(text[index]))
            {
                throw new InvalidOperationException("readInt(): неверный формат целого числа.");
            }
        }

        if (!int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int value))
        {
            throw new InvalidOperationException("readInt(): число вне диапазона int.");
        }

        return value;
    }
}