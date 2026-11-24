namespace Lait.Ast.Statements;

/// <summary>
/// Инструкция break: завершает ближайший цикл.
/// </summary>
public sealed class BreakLoopStatement : Statement
{
    public override void Accept(IAstVisitor visitor)
    {
        visitor.Visit(this);
    }
}