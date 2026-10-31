using Lait.Ast.Declarations;

namespace Lait.Ast.Statements;

/// <summary>
/// Инструкция, объявляющая переменную в текущем блоке.
/// </summary>
public sealed class VariableDeclarationStatement : Statement
{
    public VariableDeclarationStatement(VariableDeclaration declaration)
    {
        Declaration = declaration;
    }

    public VariableDeclaration Declaration { get; }

    public override void Accept(IAstVisitor visitor)
    {
        visitor.Visit(this);
    }
}