using System.Runtime.CompilerServices;

using Lait.Ast;
using Lait.Ast.Declarations;
using Lait.Ast.Expressions;
using Lait.Ast.Statements;
using Lait.Lexemes;
using Lait.Runtime;

using ValueType = Lait.Runtime.ValueType;

namespace Lait.Parsing;

/// <summary>
/// Разбирает программу рекурсивным спуском и строит её AST.
/// </summary>
public class Parser
{
    private static readonly Dictionary<TokenType, BinaryOperation> OrOperations = new()
    {
        { TokenType.Or, BinaryOperation.Or },
    };

    private static readonly Dictionary<TokenType, BinaryOperation> AndOperations = new()
    {
        { TokenType.And, BinaryOperation.And },
    };

    private static readonly Dictionary<TokenType, BinaryOperation> EqualityOperations = new()
    {
        { TokenType.Equal, BinaryOperation.Equal },
        { TokenType.NotEqual, BinaryOperation.NotEqual },
    };

    private static readonly Dictionary<TokenType, BinaryOperation> RelationalOperations = new()
    {
        { TokenType.LessThan, BinaryOperation.LessThan },
        { TokenType.LessThanOrEqual, BinaryOperation.LessThanOrEqual },
        { TokenType.GreaterThan, BinaryOperation.GreaterThan },
        { TokenType.GreaterThanOrEqual, BinaryOperation.GreaterThanOrEqual },
    };

    private static readonly Dictionary<TokenType, BinaryOperation> AdditiveOperations = new()
    {
        { TokenType.Plus, BinaryOperation.Add },
        { TokenType.Minus, BinaryOperation.Subtract },
    };

    private static readonly Dictionary<TokenType, BinaryOperation> MultiplicativeOperations = new()
    {
        { TokenType.Multiply, BinaryOperation.Multiply },
        { TokenType.Divide, BinaryOperation.Divide },
        { TokenType.Remainder, BinaryOperation.Remainder },
    };

    private static readonly Dictionary<TokenType, ValueType> Types = new()
    {
        { TokenType.Int, ValueType.Int },
        { TokenType.Float, ValueType.Float },
        { TokenType.Bool, ValueType.Bool },
    };

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
        ValueType resultType = ParseResultType();
        BlockStatement body = ParseBlock();
        return new FunctionDeclaration(name, parameters, resultType, body);
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
    ///     parameter = ? Identifier ?, ":", type ;
    /// </summary>
    private ParameterDeclaration ParseParameter()
    {
        string name = ExpectIdentifier();
        Expect(TokenType.Colon);
        ValueType type = ParseType();
        return new ParameterDeclaration(name, type);
    }

    /// <summary>
    /// Разбирает тип переменной или параметра.
    /// Правило:
    ///     type = "int" | "float" | "bool" ;
    /// </summary>
    private ValueType ParseType()
    {
        if (!Types.TryGetValue(_current.Type, out ValueType type))
        {
            throw SyntaxError("Ожидался тип int, float или bool.");
        }

        Advance();
        return type;
    }

    /// <summary>
    /// Разбирает тип результата функции.
    /// Правило:
    ///     result_type = type | "void" ;
    /// </summary>
    private ValueType ParseResultType()
    {
        if (_current.Type == TokenType.Void)
        {
            Advance();
            return ValueType.Void;
        }

        if (!Types.ContainsKey(_current.Type))
        {
            throw SyntaxError("Ожидался тип результата int, float, bool или void.");
        }

        return ParseType();
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
    ///     statement = variable_declaration | if_statement | while_statement | for_statement
    ///         | break_statement | continue_statement | return_statement | block | expression_statement ;
    /// </summary>
    private Statement ParseStatement()
    {
        return _current.Type switch
        {
            TokenType.Int or TokenType.Float or TokenType.Bool => ParseVariableDeclaration(),
            TokenType.If => ParseIfStatement(),
            TokenType.While => ParseWhileStatement(),
            TokenType.For => ParseForStatement(),
            TokenType.Break => ParseBreakStatement(),
            TokenType.Continue => ParseContinueStatement(),
            TokenType.Return => ParseReturnStatement(),
            TokenType.OpenBrace => ParseBlock(),
            _ => ParseExpressionStatement(),
        };
    }

    /// <summary>
    /// Разбирает объявление переменной.
    /// Правило:
    ///     variable_declaration = variable_definition, ";" ;
    /// </summary>
    private VariableDeclarationStatement ParseVariableDeclaration()
    {
        VariableDeclarationStatement statement = ParseVariableDefinition();
        Expect(TokenType.Semicolon);
        return statement;
    }

    /// <summary>
    /// Разбирает объявление переменной без завершающей точки с запятой.
    /// Правило:
    ///     variable_definition = type, ? Identifier ?, "=", expression ;
    /// </summary>
    private VariableDeclarationStatement ParseVariableDefinition()
    {
        ValueType type = ParseType();
        string name = ExpectIdentifier();
        Expect(TokenType.Assign);
        Expression initialValue = ParseExpression();
        return new VariableDeclarationStatement(new VariableDeclaration(name, type, initialValue));
    }

    /// <summary>
    /// Разбирает условную инструкцию; ветвь else может быть блоком или следующей условной инструкцией.
    /// Правило:
    ///     if_statement = "if", "(", expression, ")", block, [ "else", ( if_statement | block ) ] ;
    /// </summary>
    private IfElseStatement ParseIfStatement()
    {
        RuntimeHelpers.EnsureSufficientExecutionStack();
        Expect(TokenType.If);
        Expression condition = ParseCondition();
        BlockStatement thenBranch = ParseBlock();
        Statement? elseBranch = null;
        if (_current.Type == TokenType.Else)
        {
            Advance();
            elseBranch = _current.Type == TokenType.If ? ParseIfStatement() : ParseBlock();
        }

        return new IfElseStatement(condition, thenBranch, elseBranch);
    }

    /// <summary>
    /// Разбирает цикл while.
    /// Правило:
    ///     while_statement = "while", "(", expression, ")", block ;
    /// </summary>
    private WhileLoopStatement ParseWhileStatement()
    {
        Expect(TokenType.While);
        Expression condition = ParseCondition();
        BlockStatement body = ParseBlock();
        return new WhileLoopStatement(condition, body);
    }

    /// <summary>
    /// Разбирает цикл for; инициализатор - объявление переменной или выражение.
    /// Правило:
    ///     for_statement = "for", "(", ( variable_definition | expression ), ";", expression, ";", expression, ")", block ;
    /// </summary>
    private ForLoopStatement ParseForStatement()
    {
        Expect(TokenType.For);
        Expect(TokenType.OpenParenthesis);
        Statement initializer = Types.ContainsKey(_current.Type)
            ? ParseVariableDefinition()
            : new ExpressionStatement(ParseExpression());
        Expect(TokenType.Semicolon);
        Expression condition = ParseExpression();
        Expect(TokenType.Semicolon);
        ExpressionStatement step = new(ParseExpression());
        Expect(TokenType.CloseParenthesis);
        BlockStatement body = ParseBlock();
        return new ForLoopStatement(initializer, condition, step, body);
    }

    /// <summary>
    /// Разбирает условие ветвления или цикла в круглых скобках.
    /// </summary>
    private Expression ParseCondition()
    {
        Expect(TokenType.OpenParenthesis);
        Expression condition = ParseExpression();
        Expect(TokenType.CloseParenthesis);
        return condition;
    }

    /// <summary>
    /// Разбирает инструкцию break.
    /// Правило:
    ///     break_statement = "break", ";" ;
    /// </summary>
    private BreakLoopStatement ParseBreakStatement()
    {
        Expect(TokenType.Break);
        Expect(TokenType.Semicolon);
        return new BreakLoopStatement();
    }

    /// <summary>
    /// Разбирает инструкцию continue.
    /// Правило:
    ///     continue_statement = "continue", ";" ;
    /// </summary>
    private ContinueLoopStatement ParseContinueStatement()
    {
        Expect(TokenType.Continue);
        Expect(TokenType.Semicolon);
        return new ContinueLoopStatement();
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
    ///     assignment_expression = or_expression, [ "=", assignment_expression ] ;
    /// </summary>
    private Expression ParseAssignmentExpression()
    {
        RuntimeHelpers.EnsureSufficientExecutionStack();
        Expression left = ParseOrExpression();
        if (_current.Type != TokenType.Assign)
        {
            return left;
        }

        Advance();
        return new AssignmentExpression(left, ParseAssignmentExpression());
    }

    /// <summary>
    /// Разбирает логическое "или".
    /// Правило:
    ///     or_expression = and_expression, { "||", and_expression } ;
    /// </summary>
    private Expression ParseOrExpression()
    {
        Expression expression = ParseAndExpression();
        while (TryReadOperation(OrOperations, out BinaryOperation operation))
        {
            expression = new BinaryOperationExpression(expression, operation, ParseAndExpression());
        }

        return expression;
    }

    /// <summary>
    /// Разбирает логическое "и".
    /// Правило:
    ///     and_expression = equality_expression, { "&&", equality_expression } ;
    /// </summary>
    private Expression ParseAndExpression()
    {
        Expression expression = ParseEqualityExpression();
        while (TryReadOperation(AndOperations, out BinaryOperation operation))
        {
            expression = new BinaryOperationExpression(expression, operation, ParseEqualityExpression());
        }

        return expression;
    }

    /// <summary>
    /// Разбирает проверки равенства и неравенства.
    /// Правило:
    ///     equality_expression = relational_expression, { ( "==" | "!=" ), relational_expression } ;
    /// </summary>
    private Expression ParseEqualityExpression()
    {
        Expression expression = ParseRelationalExpression();
        while (TryReadOperation(EqualityOperations, out BinaryOperation operation))
        {
            expression = new BinaryOperationExpression(expression, operation, ParseRelationalExpression());
        }

        return expression;
    }

    /// <summary>
    /// Разбирает сравнения чисел.
    /// Правило:
    ///     relational_expression = additive_expression, { ( "<" | "<=" | ">" | ">=" ), additive_expression } ;
    /// </summary>
    private Expression ParseRelationalExpression()
    {
        Expression expression = ParseAdditiveExpression();
        while (TryReadOperation(RelationalOperations, out BinaryOperation operation))
        {
            expression = new BinaryOperationExpression(expression, operation, ParseAdditiveExpression());
        }

        return expression;
    }

    /// <summary>
    /// Разбирает операторы сложения и вычитания.
    /// Правило:
    ///     additive_expression = multiplicative_expression, { ( "+" | "-" ), multiplicative_expression } ;
    /// </summary>
    private Expression ParseAdditiveExpression()
    {
        Expression expression = ParseMultiplicativeExpression();
        while (TryReadOperation(AdditiveOperations, out BinaryOperation operation))
        {
            expression = new BinaryOperationExpression(expression, operation, ParseMultiplicativeExpression());
        }

        return expression;
    }

    /// <summary>
    /// Разбирает операторы умножения, деления и остатка от деления.
    /// Правило:
    ///     multiplicative_expression = unary_expression, { ( "*" | "/" | "%" ), unary_expression } ;
    /// </summary>
    private Expression ParseMultiplicativeExpression()
    {
        Expression expression = ParseUnaryExpression();
        while (TryReadOperation(MultiplicativeOperations, out BinaryOperation operation))
        {
            expression = new BinaryOperationExpression(expression, operation, ParseUnaryExpression());
        }

        return expression;
    }

    /// <summary>
    /// Разбирает унарный минус и логическое отрицание.
    /// Правило:
    ///     unary_expression = ( "-" | "!" ), unary_expression | primary_expression ;
    /// </summary>
    private Expression ParseUnaryExpression()
    {
        RuntimeHelpers.EnsureSufficientExecutionStack();
        if (_current.Type == TokenType.Minus)
        {
            Advance();
            return new UnaryMinusExpression(ParseUnaryExpression());
        }

        if (_current.Type == TokenType.Not)
        {
            Advance();
            return new LogicalNotExpression(ParseUnaryExpression());
        }

        return ParsePrimaryExpression();
    }

    /// <summary>
    /// Разбирает литерал, выражение в скобках, переменную или вызов функции.
    /// Правило:
    ///     primary_expression = ? IntLiteral ? | ? FloatLiteral ? | "true" | "false"
    ///         | "(", expression, ")" | ? Identifier ?, [ argument_list ] ;
    /// </summary>
    private Expression ParsePrimaryExpression()
    {
        Value? literalValue = _current.Type switch
        {
            TokenType.IntLiteral => new Value(_current.Value!.ToInt()),
            TokenType.FloatLiteral => new Value(_current.Value!.ToFloat()),
            TokenType.True => new Value(true),
            TokenType.False => new Value(false),
            _ => null,
        };
        if (literalValue is not null)
        {
            Advance();
            return new LiteralExpression(literalValue);
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

        throw SyntaxError("Ожидалось число, логическое значение, скобка, переменная или вызов функции.");
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

    /// <summary>
    /// Если текущая лексема - оператор из таблицы, пропускает её и возвращает соответствующую операцию.
    /// </summary>
    private bool TryReadOperation(Dictionary<TokenType, BinaryOperation> operations, out BinaryOperation operation)
    {
        if (!operations.TryGetValue(_current.Type, out operation))
        {
            return false;
        }

        Advance();
        return true;
    }

    private void Expect(TokenType type)
    {
        if (_current.Type != type)
        {
            throw SyntaxError($"Ожидался токен {type}.");
        }

        Advance();
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