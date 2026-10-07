using Lait.Lexemes;
using Lait.Runtime;

namespace Lait.Parsing;

/// <summary>
/// Рекурсивный спуск по EBNF из спецификации. Инструкции VM создаются без AST.
/// </summary>
public class Parser
{
    private const int MaximumNesting = 256;
    private readonly Lexer _lexer;
    private readonly List<Instruction> _instructions = new();
    private Token _current = new(TokenType.EndOfFile);
    private int _tokenNumber;
    private int _nesting;
    private bool _started;

    public Parser(Lexer lexer)
    {
        _lexer = lexer;
    }

    public IReadOnlyList<Instruction> ParseProgram()
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
        return _instructions.AsReadOnly();
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
            Emit(Operation.Discard);
        }

        Expect(TokenType.Semicolon);
    }

    private void ParsePrintStatement()
    {
        ExpectIdentifier("print");
        Expect(TokenType.OpenParenthesis);
        ParseExpression();
        Expect(TokenType.CloseParenthesis);
        Emit(Operation.Print);
    }

    private void ParseExpression()
    {
        ParseAdditiveExpression();
    }

    private void ParseAdditiveExpression()
    {
        ParseMultiplicativeExpression();
        while (_current.Type is TokenType.Plus or TokenType.Minus)
        {
            TokenType operation = _current.Type;
            Advance();
            ParseMultiplicativeExpression();
            Emit(operation == TokenType.Plus ? Operation.Add : Operation.Subtract);
        }
    }

    private void ParseMultiplicativeExpression()
    {
        ParseUnaryExpression();
        while (_current.Type is TokenType.Multiply or TokenType.Divide)
        {
            TokenType operation = _current.Type;
            Advance();
            ParseUnaryExpression();
            Emit(operation == TokenType.Multiply ? Operation.Multiply : Operation.Divide);
        }
    }

    private void ParseUnaryExpression()
    {
        _nesting++;
        if (_nesting > MaximumNesting)
        {
            throw SyntaxError($"Превышена допустимая вложенность выражений ({MaximumNesting}).");
        }

        if (_current.Type == TokenType.Minus)
        {
            Advance();
            ParseUnaryExpression();
            Emit(Operation.Negate);
        }
        else
        {
            ParsePrimaryExpression();
        }

        _nesting--;
    }

    private void ParsePrimaryExpression()
    {
        if (_current.Type == TokenType.IntLiteral)
        {
            _instructions.Add(new Instruction(Operation.Push, _current.Value!.ToInt()));
            Advance();
        }
        else if (_current.Type == TokenType.OpenParenthesis)
        {
            Advance();
            ParseExpression();
            Expect(TokenType.CloseParenthesis);
        }
        else if (IsIdentifier("readInt"))
        {
            ParseReadExpression();
        }
        else if (IsIdentifier("abs"))
        {
            ParseAbsoluteExpression();
        }
        else if (IsIdentifier("min") || IsIdentifier("max"))
        {
            ParseExtremaExpression();
        }
        else
        {
            throw SyntaxError("Ожидалось число, скобка или вызов readInt, abs, min, max.");
        }
    }

    private void ParseReadExpression()
    {
        ExpectIdentifier("readInt");
        Expect(TokenType.OpenParenthesis);
        Expect(TokenType.CloseParenthesis);
        Emit(Operation.Read);
    }

    private void ParseAbsoluteExpression()
    {
        ExpectIdentifier("abs");
        Expect(TokenType.OpenParenthesis);
        ParseExpression();
        Expect(TokenType.CloseParenthesis);
        Emit(Operation.Absolute);
    }

    private void ParseExtremaExpression()
    {
        Operation operation = IsIdentifier("min") ? Operation.Minimum : Operation.Maximum;
        Advance();
        Expect(TokenType.OpenParenthesis);
        ParseExpression();
        Expect(TokenType.Comma);
        ParseExpression();
        Expect(TokenType.CloseParenthesis);
        Emit(operation);
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

    private void Emit(Operation operation)
    {
        _instructions.Add(new Instruction(operation));
    }
}