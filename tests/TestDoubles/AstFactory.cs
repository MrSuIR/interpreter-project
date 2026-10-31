using Lait.Ast;
using Lait.Ast.Declarations;
using Lait.Ast.Expressions;
using Lait.Ast.Statements;

namespace Lait.TestDoubles;

/// <summary>
/// Краткие способы построить узлы AST в тестах.
/// </summary>
public static class AstFactory
{
    public static LiteralExpression Number(int value)
    {
        return new LiteralExpression(value);
    }

    public static VariableAccessExpression Name(string name)
    {
        return new VariableAccessExpression(name);
    }

    public static UnaryMinusExpression Minus(Expression operand)
    {
        return new UnaryMinusExpression(operand);
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

    public static VariableDeclarationStatement Var(string name, Expression initialValue)
    {
        return new VariableDeclarationStatement(new VariableDeclaration(name, initialValue));
    }

    public static ReturnStatement Return(Expression? value = null)
    {
        return new ReturnStatement(value);
    }

    public static BlockStatement Block(params Statement[] statements)
    {
        return new BlockStatement(statements);
    }

    public static FunctionDeclaration Function(string name, string[] parameters, bool returnsValue, params Statement[] body)
    {
        return new FunctionDeclaration(
            name,
            [.. parameters.Select(parameter => new ParameterDeclaration(parameter))],
            returnsValue,
            new BlockStatement(body));
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