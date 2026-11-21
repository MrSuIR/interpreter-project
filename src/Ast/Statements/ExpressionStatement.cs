using Lait.Ast.Expressions;

namespace Lait.Ast.Statements;

/// <summary>
/// Инструкция, которая вычисляет выражение и отбрасывает его результат.
/// </summary>
public sealed class ExpressionStatement : Statement
{
    public ExpressionStatement(Expression expression)
    {
        Expression = expression;
    }

    public Expression Expression { get; }

    public override void Accept(IAstVisitor visitor)
    {
        visitor.Visit(this);
    }
}