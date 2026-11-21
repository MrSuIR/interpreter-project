namespace Lait.Ast.Expressions;

/// <summary>
/// Целочисленный литерал.
/// </summary>
public sealed class LiteralExpression : Expression
{
    public LiteralExpression(int value)
    {
        Value = value;
    }

    public int Value { get; }

    public override void Accept(IAstVisitor visitor)
    {
        visitor.Visit(this);
    }
}