using Lait.Ast.Expressions;

namespace Lait.Ast.Statements;

/// <summary>
/// Условная инструкция с необязательной ветвью else.
/// </summary>
public sealed class IfElseStatement : Statement
{
    public IfElseStatement(Expression condition, BlockStatement thenBranch, Statement? elseBranch)
    {
        Condition = condition;
        ThenBranch = thenBranch;
        ElseBranch = elseBranch;
    }

    public Expression Condition { get; }

    public BlockStatement ThenBranch { get; }

    /// <summary>
    /// Блок или вложенная условная инструкция else if; null, если ветви else нет.
    /// </summary>
    public Statement? ElseBranch { get; }

    public override void Accept(IAstVisitor visitor)
    {
        visitor.Visit(this);
    }
}