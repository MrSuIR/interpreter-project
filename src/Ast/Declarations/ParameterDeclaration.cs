using ValueType = Lait.Runtime.ValueType;

namespace Lait.Ast.Declarations;

/// <summary>
/// Объявление параметра функции.
/// </summary>
public sealed class ParameterDeclaration : AbstractVariableDeclaration
{
    public ParameterDeclaration(string name, ValueType type)
        : base(name, type)
    {
    }

    public override void Accept(IAstVisitor visitor)
    {
        visitor.Visit(this);
    }
}