using Lait.Ast.Expressions;

namespace Lait.Ast.Statements;

/// <summary>
/// Инструкция возврата из функции с необязательным значением.
/// </summary>
public sealed class ReturnStatement : Statement
{
    public ReturnStatement(Expression? value)
    {
        Value = value;
    }

    public Expression? Value { get; }

    public override void Accept(IAstVisitor visitor)
    {
        visitor.Visit(this);
    }
}