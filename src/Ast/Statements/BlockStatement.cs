namespace Lait.Ast.Statements;

/// <summary>
/// Блок инструкций в фигурных скобках; образует область видимости.
/// </summary>
public sealed class BlockStatement : Statement
{
    public BlockStatement(IReadOnlyList<Statement> statements)
    {
        Statements = statements;
    }

    public IReadOnlyList<Statement> Statements { get; }

    public override void Accept(IAstVisitor visitor)
    {
        visitor.Visit(this);
    }
}