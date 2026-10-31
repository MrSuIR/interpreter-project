namespace Lait.Ast.Declarations;

/// <summary>
/// Объявление параметра функции типа int.
/// </summary>
public sealed class ParameterDeclaration : AbstractVariableDeclaration
{
    public ParameterDeclaration(string name)
        : base(name)
    {
    }

    public override void Accept(IAstVisitor visitor)
    {
        visitor.Visit(this);
    }
}