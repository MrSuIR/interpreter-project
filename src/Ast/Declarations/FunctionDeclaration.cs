using Lait.Ast.Statements;

using ValueType = Lait.Runtime.ValueType;

namespace Lait.Ast.Declarations;

/// <summary>
/// Объявление функции, определённой в программе.
/// </summary>
public sealed class FunctionDeclaration : AbstractFunctionDeclaration
{
    public FunctionDeclaration(
        string name,
        IReadOnlyList<ParameterDeclaration> parameters,
        ValueType resultType,
        BlockStatement body)
        : base(name, parameters, resultType)
    {
        Body = body;
    }

    public BlockStatement Body { get; }

    public override void Accept(IAstVisitor visitor)
    {
        visitor.Visit(this);
    }
}