using System.Runtime.CompilerServices;

using Lait.Ast.Declarations;
using Lait.Ast.Expressions;
using Lait.Ast.Statements;
using Lait.Lexemes;

namespace Lait.Parsing;

/// <summary>
/// Разбирает программу рекурсивным спуском и строит её AST.
/// </summary>
public class Parser
{
    private readonly Lexer _lexer;
    private Token _current = new(TokenType.EndOfFile);
    private int _tokenNumber;
    private bool _started;

    public Parser(Lexer lexer)
    {
        _lexer = lexer;
    }

    /// <summary>
    /// Разбирает программу.
    /// Правило:
    ///     program = "func", "main", "(", ")", ":", "void", block ;
    /// </summary>
    public FunctionDeclaration ParseProgram()
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
            List<Statement> body = ParseBlock();
            Expect(TokenType.EndOfFile);
            return new FunctionDeclaration("main", body);
        }
        catch (InsufficientExecutionStackException)
        {
            throw new InvalidOperationException("Недостаточно стека для разбора вложенного выражения.");
        }
    }

    /// <summary>
    /// Разбирает блок инструкций.
    /// Правило:
    ///     block = "{", { statement }, "}" ;
    /// </summary>
    private List<Statement> ParseBlock()
    {
        List<Statement> statements = [];
        Expect(TokenType.OpenBrace);
        while (_current.Type != TokenType.CloseBrace)
        {
            if (_current.Type == TokenType.EndOfFile)
            {
                throw SyntaxError("Ожидалась закрывающая фигурная скобка.");
            }

            statements.Add(ParseStatement());
        }

        Expect(TokenType.CloseBrace);
        return statements;
    }

    /// <summary>
    /// Разбирает инструкцию.
    /// Правило:
    ///     statement = expression, ";" ;
    /// </summary>
    private ExpressionStatement ParseStatement()
    {
        Expression expression = ParseExpression();
        Expect(TokenType.Semicolon);
        return new ExpressionStatement(expression);
    }

    /// <summary>
    /// Разбирает выражение.
    /// Правило:
    ///     expression = additive_expression ;
    /// </summary>
    private Expression ParseExpression()
    {
        return ParseAdditiveExpression();
    }

    /// <summary>
    /// Разбирает операторы сложения и вычитания.
    /// Правило:
    ///     additive_expression = multiplicative_expression, { ( "+" | "-" ), multiplicative_expression } ;
    /// </summary>
    private Expression ParseAdditiveExpression()
    {
        Expression expression = ParseMultiplicativeExpression();
        while (_current.Type is TokenType.Plus or TokenType.Minus)
        {
            BinaryOperation operation = _current.Type == TokenType.Plus ? BinaryOperation.Add : BinaryOperation.Subtract;
            Advance();
            expression = new BinaryOperationExpression(expression, operation, ParseMultiplicativeExpression());
        }

        return expression;
    }

    /// <summary>
    /// Разбирает операторы умножения и деления.
    /// Правило:
    ///     multiplicative_expression = unary_expression, { ( "*" | "/" ), unary_expression } ;
    /// </summary>
    private Expression ParseMultiplicativeExpression()
    {
        Expression expression = ParseUnaryExpression();
        while (_current.Type is TokenType.Multiply or TokenType.Divide)
        {
            BinaryOperation operation = _current.Type == TokenType.Multiply
                ? BinaryOperation.Multiply
                : BinaryOperation.Divide;
            Advance();
            expression = new BinaryOperationExpression(expression, operation, ParseUnaryExpression());
        }

        return expression;
    }

    /// <summary>
    /// Разбирает унарный минус.
    /// Правило:
    ///     unary_expression = "-", unary_expression | primary_expression ;
    /// </summary>
    private Expression ParseUnaryExpression()
    {
        RuntimeHelpers.EnsureSufficientExecutionStack();
        if (_current.Type == TokenType.Minus)
        {
            Advance();
            return new UnaryMinusExpression(ParseUnaryExpression());
        }

        return ParsePrimaryExpression();
    }

    /// <summary>
    /// Разбирает число, выражение в скобках или вызов функции.
    /// Правило:
    ///     primary_expression = ? IntLiteral ? | "(", expression, ")" | function_call ;
    /// </summary>
    private Expression ParsePrimaryExpression()
    {
        if (_current.Type == TokenType.IntLiteral)
        {
            LiteralExpression literal = new(_current.Value!.ToInt());
            Advance();
            return literal;
        }

        if (_current.Type == TokenType.OpenParenthesis)
        {
            Advance();
            Expression expression = ParseExpression();
            Expect(TokenType.CloseParenthesis);
            return expression;
        }

        if (_current.Type == TokenType.Identifier)
        {
            return ParseFunctionCall();
        }

        throw SyntaxError("Ожидалось число, скобка или вызов функции.");
    }

    /// <summary>
    /// Разбирает вызов функции.
    /// Правило:
    ///     function_call = ? Identifier ?, "(", [ arguments ], ")" ;
    /// </summary>
    private FunctionCallExpression ParseFunctionCall()
    {
        string name = _current.Value!.ToString();
        Advance();
        Expect(TokenType.OpenParenthesis);
        List<Expression> arguments = _current.Type == TokenType.CloseParenthesis ? [] : ParseArguments();
        Expect(TokenType.CloseParenthesis);
        return new FunctionCallExpression(name, arguments);
    }

    /// <summary>
    /// Разбирает аргументы вызова функции.
    /// Правило:
    ///     arguments = expression, { ",", expression } ;
    /// </summary>
    private List<Expression> ParseArguments()
    {
        List<Expression> arguments = [ParseExpression()];
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