using System.Runtime.CompilerServices;

using Lait.Execution;
using Lait.Lexemes;

namespace Lait.Parsing;

/// <summary>
/// Разбирает программу рекурсивным спуском и сразу выполняет её инструкции.
/// </summary>
public class Parser
{
    private readonly Lexer _lexer;
    private readonly IReadOnlyDictionary<string, BuiltinFunction> _functions;
    private Token _current = new(TokenType.EndOfFile);
    private int _tokenNumber;
    private bool _started;

    public Parser(Lexer lexer, Builtins builtins)
    {
        _lexer = lexer;
        _functions = builtins.Functions;
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
        BuiltinFunction? function = _current.Type == TokenType.Identifier ? FindFunction() : null;
        if (function is { ReturnsValue: false })
        {
            List<int> arguments = ParseCallArguments(function);
            if (_current.Type != TokenType.Semicolon)
            {
                throw SyntaxError("Ожидалась точка с запятой.");
            }

            // Функцию без результата вызываем после проверки инструкции, до чтения следующей лексемы.
            _ = function.Invoke(arguments);
        }
        else
        {
            _ = ParseExpression();
        }

        Expect(TokenType.Semicolon);
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
            return ParseFunctionCall();
        }

        throw SyntaxError("Ожидалось число, скобка или вызов функции.");
    }

    private int ParseFunctionCall()
    {
        BuiltinFunction function = FindFunction();
        if (!function.ReturnsValue)
        {
            throw SyntaxError($"Функция '{function.Name}' не возвращает значения и допускается только как инструкция.");
        }

        List<int> arguments = ParseCallArguments(function);
        return function.Invoke(arguments);
    }

    private List<int> ParseCallArguments(BuiltinFunction function)
    {
        Advance();
        Expect(TokenType.OpenParenthesis);
        List<int> arguments = _current.Type == TokenType.CloseParenthesis ? [] : ParseArguments();
        Expect(TokenType.CloseParenthesis);
        if (arguments.Count != function.Parameters.Count)
        {
            throw SyntaxError(
                $"Функция '{function.Name}' ожидает аргументов: {function.Parameters.Count}; получено: {arguments.Count}.");
        }

        return arguments;
    }

    private BuiltinFunction FindFunction()
    {
        string name = _current.Value!.ToString();
        if (!_functions.TryGetValue(name, out BuiltinFunction? function))
        {
            throw SyntaxError($"Неизвестная функция '{name}'.");
        }

        return function;
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
}