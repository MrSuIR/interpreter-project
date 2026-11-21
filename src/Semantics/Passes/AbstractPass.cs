using System.Runtime.CompilerServices;

using Lait.Ast;
using Lait.Ast.Declarations;
using Lait.Ast.Expressions;
using Lait.Ast.Statements;

namespace Lait.Semantics.Passes;

/// <summary>
/// Базовый класс для проходов по AST с целью вычисления атрибутов и семантических проверок.
/// </summary>
public abstract class AbstractPass : IAstVisitor
{
    public virtual void Visit(LiteralExpression expression)
    {
    }

    public virtual void Visit(BinaryOperationExpression expression)
    {
        RuntimeHelpers.EnsureSufficientExecutionStack();
        expression.Left.Accept(this);
        expression.Right.Accept(this);
    }

    public virtual void Visit(UnaryMinusExpression expression)
    {
        RuntimeHelpers.EnsureSufficientExecutionStack();
        expression.Operand.Accept(this);
    }

    public virtual void Visit(FunctionCallExpression expression)
    {
        RuntimeHelpers.EnsureSufficientExecutionStack();
        foreach (Expression argument in expression.Arguments)
        {
            argument.Accept(this);
        }
    }

    public virtual void Visit(VariableAccessExpression expression)
    {
    }

    public virtual void Visit(AssignmentExpression expression)
    {
        RuntimeHelpers.EnsureSufficientExecutionStack();
        expression.Left.Accept(this);
        expression.Right.Accept(this);
    }

    public virtual void Visit(ExpressionStatement statement)
    {
        statement.Expression.Accept(this);
    }

    public virtual void Visit(BlockStatement statement)
    {
        RuntimeHelpers.EnsureSufficientExecutionStack();
        foreach (Statement nested in statement.Statements)
        {
            nested.Accept(this);
        }
    }

    public virtual void Visit(VariableDeclarationStatement statement)
    {
        statement.Declaration.Accept(this);
    }

    public virtual void Visit(ReturnStatement statement)
    {
        statement.Value?.Accept(this);
    }

    public virtual void Visit(FunctionDeclaration declaration)
    {
        foreach (ParameterDeclaration parameter in declaration.Parameters)
        {
            parameter.Accept(this);
        }

        declaration.Body.Accept(this);
    }

    public virtual void Visit(ParameterDeclaration declaration)
    {
    }

    public virtual void Visit(VariableDeclaration declaration)
    {
        declaration.InitialValue.Accept(this);
    }

    public virtual void Visit(ProgramNode program)
    {
        foreach (FunctionDeclaration function in program.Functions)
        {
            function.Accept(this);
        }
    }
}