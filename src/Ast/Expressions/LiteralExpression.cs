using Lait.Runtime;

namespace Lait.Ast.Expressions;

/// <summary>
/// Литерал: целое число, число с плавающей точкой или булево значение.
/// </summary>
public sealed class LiteralExpression : Expression
{
    public LiteralExpression(Value value)
    {
        Value = value;
    }

    public Value Value { get; }

    public override void Accept(IAstVisitor visitor)
    {
        visitor.Visit(this);
    }
}