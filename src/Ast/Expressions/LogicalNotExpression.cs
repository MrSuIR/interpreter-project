namespace Lait.Ast.Expressions;

/// <summary>
/// Логическое отрицание булева значения.
/// </summary>
public sealed class LogicalNotExpression : Expression
{
    public LogicalNotExpression(Expression operand)
    {
        Operand = operand;
    }

    public Expression Operand { get; }

    public override void Accept(IAstVisitor visitor)
    {
        visitor.Visit(this);
    }
}