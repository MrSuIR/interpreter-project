using Lait.Ast.Statements;

namespace Lait.Ast.Declarations;

/// <summary>
/// Объявление функции, определённой в программе, с телом из инструкций.
/// </summary>
public sealed class FunctionDeclaration : Declaration
{
    public FunctionDeclaration(string name, IReadOnlyList<Statement> body)
    {
        Name = name;
        Body = body;
    }

    public string Name { get; }

    public IReadOnlyList<Statement> Body { get; }

    public override void Accept(IAstVisitor visitor)
    {
        visitor.Visit(this);
    }
}