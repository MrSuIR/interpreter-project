using Lait.Ast;
using Lait.Ast.Declarations;
using Lait.Ast.Expressions;
using Lait.Ast.Statements;
using Lait.Runtime;

using ValueType = Lait.Runtime.ValueType;

namespace Lait.TestDoubles;

/// <summary>
/// Краткие способы построить узлы AST в тестах.
/// </summary>
public static class AstFactory
{
    public static LiteralExpression Number(int value)
    {
        return new LiteralExpression(new Value(value));
    }

    public static LiteralExpression Float(double value)
    {
        return new LiteralExpression(new Value(value));
    }

    public static LiteralExpression Bool(bool value)
    {
        return new LiteralExpression(new Value(value));
    }

    public static VariableAccessExpression Name(string name)
    {
        return new VariableAccessExpression(name);
    }

    public static UnaryMinusExpression Minus(Expression operand)
    {
        return new UnaryMinusExpression(operand);
    }

    public static LogicalNotExpression Not(Expression operand)
    {
        return new LogicalNotExpression(operand);
    }

    public static BinaryOperationExpression Binary(Expression left, BinaryOperation operation, Expression right)
    {
        return new BinaryOperationExpression(left, operation, right);
    }

    public static AssignmentExpression Assign(Expression left, Expression right)
    {
        return new AssignmentExpression(left, right);
    }

    public static FunctionCallExpression Call(string name, params Expression[] arguments)
    {
        return new FunctionCallExpression(name, arguments);
    }

    public static ExpressionStatement Expr(Expression expression)
    {
        return new ExpressionStatement(expression);
    }

    /// <summary>
    /// Объявление переменной типа int.
    /// </summary>
    public static VariableDeclarationStatement Var(string name, Expression initialValue)
    {
        return Var(ValueType.Int, name, initialValue);
    }

    public static VariableDeclarationStatement Var(ValueType type, string name, Expression initialValue)
    {
        return new VariableDeclarationStatement(new VariableDeclaration(name, type, initialValue));
    }

    public static IfElseStatement If(Expression condition, BlockStatement thenBranch, Statement? elseBranch = null)
    {
        return new IfElseStatement(condition, thenBranch, elseBranch);
    }

    public static WhileLoopStatement While(Expression condition, params Statement[] body)
    {
        return new WhileLoopStatement(condition, new BlockStatement(body));
    }

    public static ForLoopStatement For(Statement initializer, Expression condition, Expression step, params Statement[] body)
    {
        return new ForLoopStatement(initializer, condition, new ExpressionStatement(step), new BlockStatement(body));
    }

    public static BreakLoopStatement Break()
    {
        return new BreakLoopStatement();
    }

    public static ContinueLoopStatement Continue()
    {
        return new ContinueLoopStatement();
    }

    public static ReturnStatement Return(Expression? value = null)
    {
        return new ReturnStatement(value);
    }

    public static BlockStatement Block(params Statement[] statements)
    {
        return new BlockStatement(statements);
    }

    public static ParameterDeclaration Parameter(string name, ValueType type)
    {
        return new ParameterDeclaration(name, type);
    }

    /// <summary>
    /// Функция с параметрами типа int и результатом int или void.
    /// </summary>
    public static FunctionDeclaration Function(string name, string[] parameters, bool returnsValue, params Statement[] body)
    {
        return TypedFunction(
            name,
            [.. parameters.Select(parameter => Parameter(parameter, ValueType.Int))],
            returnsValue ? ValueType.Int : ValueType.Void,
            body);
    }

    public static FunctionDeclaration TypedFunction(
        string name,
        ParameterDeclaration[] parameters,
        ValueType resultType,
        params Statement[] body)
    {
        return new FunctionDeclaration(name, parameters, resultType, new BlockStatement(body));
    }

    public static FunctionDeclaration MainFunction(params Statement[] body)
    {
        return Function("main", [], false, body);
    }

    public static ProgramNode MainProgram(params Statement[] body)
    {
        return ProgramOf(MainFunction(body));
    }

    public static ProgramNode ProgramOf(params FunctionDeclaration[] functions)
    {
        return new ProgramNode(functions);
    }
}