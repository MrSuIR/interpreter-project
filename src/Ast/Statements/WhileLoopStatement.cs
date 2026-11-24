using Lait.Ast.Expressions;

namespace Lait.Ast.Statements;

/// <summary>
/// Цикл while: тело выполняется, пока условие истинно.
/// </summary>
public sealed class WhileLoopStatement : Statement
{
    public WhileLoopStatement(Expression condition, BlockStatement body)
    {
        Condition = condition;
        Body = body;
    }

    public Expression Condition { get; }

    public BlockStatement Body { get; }

    public override void Accept(IAstVisitor visitor)
    {
        visitor.Visit(this);
    }
}