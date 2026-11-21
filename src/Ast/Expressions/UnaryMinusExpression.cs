namespace Lait.Ast.Expressions;

/// <summary>
/// Унарный минус: смена знака числа.
/// </summary>
public sealed class UnaryMinusExpression : Expression
{
    public UnaryMinusExpression(Expression operand)
    {
        Operand = operand;
    }

    public Expression Operand { get; }

    public override void Accept(IAstVisitor visitor)
    {
        visitor.Visit(this);
    }
}