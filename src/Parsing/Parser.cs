using System.Globalization;
using System.Runtime.CompilerServices;

using Lait.Lexemes;

namespace Lait.Parsing;

/// <summary>
/// Разбирает программу рекурсивным спуском и сразу выполняет её инструкции.
/// </summary>
public class Parser
{
    private readonly Lexer _lexer;
    private readonly TextReader _input;
    private readonly TextWriter _output;
    private Token _current = new(TokenType.EndOfFile);
    private int _tokenNumber;
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
        try
        {
            Advance();
            Expect(TokenType.Func);
            ExpectIdentifier("main");
            Expect(TokenType.OpenParenthesis);
            Expect(TokenType.CloseParenthesis);
            Expect(TokenType.Colon);
            Expect(TokenType.Void);
            ParseBlock();
            Expect(TokenType.EndOfFile);
        }
        catch (OverflowException)
        {
            throw new InvalidOperationException("Результат операции выходит за диапазон int.");
        }
        catch (DivideByZeroException)
        {
            throw new InvalidOperationException("Деление на ноль.");
        }
        catch (InsufficientExecutionStackException)
        {
            throw new InvalidOperationException("Недостаточно стека для разбора вложенного выражения.");
        }
    }

    private void ParseBlock()
    {
        Expect(TokenType.OpenBrace);
        while (_current.Type != TokenType.CloseBrace)
        {
            if (_current.Type == TokenType.EndOfFile)
            {
                throw SyntaxError("Ожидалась закрывающая фигурная скобка.");
            }

            ParseStatement();
        }

        Expect(TokenType.CloseBrace);
    }

    private void ParseStatement()
    {
        if (IsIdentifier("print"))
        {
            int value = ParsePrintCall();
            if (_current.Type != TokenType.Semicolon)
            {
                throw SyntaxError("Ожидалась точка с запятой.");
            }

            // Выводим после проверки инструкции, до чтения следующей лексемы.
            _output.WriteLine(value.ToString(CultureInfo.InvariantCulture));
            _output.Flush();
        }
        else
        {
            _ = ParseExpression();
        }

        Expect(TokenType.Semicolon);
    }

    private int ParsePrintCall()
    {
        ExpectIdentifier("print");
        Expect(TokenType.OpenParenthesis);
        int value = ParseExpression();
        Expect(TokenType.CloseParenthesis);
        return value;
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
        RuntimeHelpers.EnsureSufficientExecutionStack();
        if (_current.Type == TokenType.Minus)
        {
            Advance();
            return checked(-ParseUnaryExpression());
        }

        return ParsePrimaryExpression();
    }

    private int ParsePrimaryExpression()
    {
        if (_current.Type == TokenType.IntLiteral)
        {
            int value = _current.Value!.ToInt();
            Advance();
            return value;
        }

        if (_current.Type == TokenType.OpenParenthesis)
        {
            Advance();
            int value = ParseExpression();
            Expect(TokenType.CloseParenthesis);
            return value;
        }

        if (_current.Type == TokenType.Identifier)
        {
            return ParseNumericCall();
        }

        throw SyntaxError("Ожидалось число, скобка или вызов числовой функции.");
    }

    private int ParseNumericCall()
    {
        string name = ParseNumericFunction();
        Expect(TokenType.OpenParenthesis);
        List<int> arguments = _current.Type == TokenType.CloseParenthesis ? [] : ParseArguments();
        Expect(TokenType.CloseParenthesis);
        int expectedCount = name switch
        {
            "readInt" => 0,
            "abs" => 1,
            _ => 2,
        };
        if (arguments.Count != expectedCount)
        {
            throw SyntaxError($"Функция '{name}' ожидает аргументов: {expectedCount}; получено: {arguments.Count}.");
        }

        return name switch
        {
            "readInt" => ReadInteger(),
            "abs" => Math.Abs(arguments[0]),
            "min" => Math.Min(arguments[0], arguments[1]),
            "max" => Math.Max(arguments[0], arguments[1]),
            _ => throw new InvalidOperationException("Неизвестная числовая функция."),
        };
    }

    private string ParseNumericFunction()
    {
        if (_current.Type != TokenType.Identifier)
        {
            throw SyntaxError("Ожидалось имя числовой функции.");
        }

        string name = _current.Value!.ToString();
        if (name is not ("readInt" or "abs" or "min" or "max"))
        {
            throw SyntaxError($"Недопустимая числовая функция '{name}'.");
        }

        Advance();
        return name;
    }

    private List<int> ParseArguments()
    {
        List<int> arguments = [ParseExpression()];
        while (_current.Type == TokenType.Comma)
        {
            Advance();
            arguments.Add(ParseExpression());
        }

        return arguments;
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