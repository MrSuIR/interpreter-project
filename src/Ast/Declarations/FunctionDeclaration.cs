using Lait.Ast.Statements;

namespace Lait.Ast.Declarations;

/// <summary>
/// Объявление функции, определённой в программе.
/// </summary>
public sealed class FunctionDeclaration : AbstractFunctionDeclaration
{
    public FunctionDeclaration(
        string name,
        IReadOnlyList<ParameterDeclaration> parameters,
        bool returnsValue,
        BlockStatement body)
        : base(name, returnsValue)
    {
        Parameters = parameters;
        Body = body;
    }

    public IReadOnlyList<ParameterDeclaration> Parameters { get; }

    public BlockStatement Body { get; }

    public override int ParameterCount => Parameters.Count;

    public override void Accept(IAstVisitor visitor)
    {
        visitor.Visit(this);
    }
}