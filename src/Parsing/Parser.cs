using System.Runtime.CompilerServices;

using Lait.Ast;
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
    ///     program = function_declaration, { function_declaration } ;
    /// </summary>
    public ProgramNode ParseProgram()
    {
        if (_started)
        {
            throw new InvalidOperationException("Парсер допускает только один разбор программы.");
        }

        _started = true;
        try
        {
            Advance();
            List<FunctionDeclaration> functions = [ParseFunctionDeclaration()];
            while (_current.Type != TokenType.EndOfFile)
            {
                functions.Add(ParseFunctionDeclaration());
            }

            return new ProgramNode(functions);
        }
        catch (InsufficientExecutionStackException)
        {
            throw new InvalidOperationException("Недостаточно стека для разбора вложенного выражения.");
        }
    }

    /// <summary>
    /// Разбирает объявление функции.
    /// Правило:
    ///     function_declaration = "func", ? Identifier ?, "(", [ parameters ], ")", ":", result_type, block ;
    /// </summary>
    private FunctionDeclaration ParseFunctionDeclaration()
    {
        Expect(TokenType.Func);
        string name = ExpectIdentifier();
        Expect(TokenType.OpenParenthesis);
        List<ParameterDeclaration> parameters = _current.Type == TokenType.CloseParenthesis ? [] : ParseParameters();
        Expect(TokenType.CloseParenthesis);
        Expect(TokenType.Colon);
        bool returnsValue = ParseResultType();
        BlockStatement body = ParseBlock();
        return new FunctionDeclaration(name, parameters, returnsValue, body);
    }

    /// <summary>
    /// Разбирает список параметров функции.
    /// Правило:
    ///     parameters = parameter, { ",", parameter } ;
    /// </summary>
    private List<ParameterDeclaration> ParseParameters()
    {
        List<ParameterDeclaration> parameters = [ParseParameter()];
        while (_current.Type == TokenType.Comma)
        {
            Advance();
            parameters.Add(ParseParameter());
        }

        return parameters;
    }

    /// <summary>
    /// Разбирает параметр функции.
    /// Правило:
    ///     parameter = ? Identifier ?, ":", "int" ;
    /// </summary>
    private ParameterDeclaration ParseParameter()
    {
        string name = ExpectIdentifier();
        Expect(TokenType.Colon);
        Expect(TokenType.Int);
        return new ParameterDeclaration(name);
    }

    /// <summary>
    /// Разбирает тип результата функции и возвращает истину для int.
    /// Правило:
    ///     result_type = "int" | "void" ;
    /// </summary>
    private bool ParseResultType()
    {
        if (_current.Type is not (TokenType.Int or TokenType.Void))
        {
            throw SyntaxError("Ожидался тип результата int или void.");
        }

        bool returnsValue = _current.Type == TokenType.Int;
        Advance();
        return returnsValue;
    }

    /// <summary>
    /// Разбирает блок инструкций.
    /// Правило:
    ///     block = "{", { statement }, "}" ;
    /// </summary>
    private BlockStatement ParseBlock()
    {
        RuntimeHelpers.EnsureSufficientExecutionStack();
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
        return new BlockStatement(statements);
    }

    /// <summary>
    /// Разбирает инструкцию; вид инструкции определяется по первой лексеме.
    /// Правило:
    ///     statement = variable_declaration | return_statement | block | expression_statement ;
    /// </summary>
    private Statement ParseStatement()
    {
        return _current.Type switch
        {
            TokenType.Int => ParseVariableDeclaration(),
            TokenType.Return => ParseReturnStatement(),
            TokenType.OpenBrace => ParseBlock(),
            _ => ParseExpressionStatement(),
        };
    }

    /// <summary>
    /// Разбирает объявление переменной.
    /// Правило:
    ///     variable_declaration = "int", ? Identifier ?, "=", expression, ";" ;
    /// </summary>
    private VariableDeclarationStatement ParseVariableDeclaration()
    {
        Expect(TokenType.Int);
        string name = ExpectIdentifier();
        Expect(TokenType.Assign);
        Expression initialValue = ParseExpression();
        Expect(TokenType.Semicolon);
        return new VariableDeclarationStatement(new VariableDeclaration(name, initialValue));
    }

    /// <summary>
    /// Разбирает инструкцию возврата.
    /// Правило:
    ///     return_statement = "return", [ expression ], ";" ;
    /// </summary>
    private ReturnStatement ParseReturnStatement()
    {
        Expect(TokenType.Return);
        Expression? value = _current.Type == TokenType.Semicolon ? null : ParseExpression();
        Expect(TokenType.Semicolon);
        return new ReturnStatement(value);
    }

    /// <summary>
    /// Разбирает инструкцию-выражение.
    /// Правило:
    ///     expression_statement = expression, ";" ;
    /// </summary>
    private ExpressionStatement ParseExpressionStatement()
    {
        Expression expression = ParseExpression();
        Expect(TokenType.Semicolon);
        return new ExpressionStatement(expression);
    }

    /// <summary>
    /// Разбирает выражение.
    /// Правило:
    ///     expression = assignment_expression ;
    /// </summary>
    private Expression ParseExpression()
    {
        return ParseAssignmentExpression();
    }

    /// <summary>
    /// Разбирает правоассоциативное присваивание; то, что слева стоит переменная, проверяет семантика.
    /// Правило:
    ///     assignment_expression = additive_expression, [ "=", assignment_expression ] ;
    /// </summary>
    private Expression ParseAssignmentExpression()
    {
        RuntimeHelpers.EnsureSufficientExecutionStack();
        Expression left = ParseAdditiveExpression();
        if (_current.Type != TokenType.Assign)
        {
            return left;
        }

        Advance();
        return new AssignmentExpression(left, ParseAssignmentExpression());
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
    /// Разбирает число, выражение в скобках, переменную или вызов функции.
    /// Правило:
    ///     primary_expression = ? IntLiteral ? | "(", expression, ")" | ? Identifier ?, [ argument_list ] ;
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
            // Имя, после которого стоит "(", обозначает вызов функции, иначе - переменную.
            string name = ExpectIdentifier();
            return _current.Type == TokenType.OpenParenthesis
                ? new FunctionCallExpression(name, ParseArgumentList())
                : new VariableAccessExpression(name);
        }

        throw SyntaxError("Ожидалось число, скобка, переменная или вызов функции.");
    }

    /// <summary>
    /// Разбирает список аргументов вызова функции.
    /// Правило:
    ///     argument_list = "(", [ expression, { ",", expression } ], ")" ;
    /// </summary>
    private List<Expression> ParseArgumentList()
    {
        List<Expression> arguments = [];
        Expect(TokenType.OpenParenthesis);
        if (_current.Type != TokenType.CloseParenthesis)
        {
            arguments.Add(ParseExpression());
            while (_current.Type == TokenType.Comma)
            {
                Advance();
                arguments.Add(ParseExpression());
            }
        }

        Expect(TokenType.CloseParenthesis);
        return arguments;
    }

    /// <summary>
    /// Читает идентификатор и возвращает его имя.
    /// </summary>
    private string ExpectIdentifier()
    {
        if (_current.Type != TokenType.Identifier)
        {
            throw SyntaxError("Ожидался идентификатор.");
        }

        string name = _current.Value!.ToString();
        Advance();
        return name;
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