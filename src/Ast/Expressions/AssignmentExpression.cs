namespace Lait.Ast.Expressions;

/// <summary>
/// Присваивание; его значение равно присвоенному значению.
/// </summary>
public sealed class AssignmentExpression : Expression
{
    public AssignmentExpression(Expression left, Expression right)
    {
        Left = left;
        Right = right;
    }

    /// <summary>
    /// Левый операнд; то, что он является переменной, проверяется при проверке программы.
    /// </summary>
    public Expression Left { get; }

    public Expression Right { get; }

    public override void Accept(IAstVisitor visitor)
    {
        visitor.Visit(this);
    }
}