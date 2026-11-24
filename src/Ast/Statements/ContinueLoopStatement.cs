namespace Lait.Ast.Statements;

/// <summary>
/// Инструкция continue: завершает текущий шаг ближайшего цикла.
/// </summary>
public sealed class ContinueLoopStatement : Statement
{
    public override void Accept(IAstVisitor visitor)
    {
        visitor.Visit(this);
    }
}