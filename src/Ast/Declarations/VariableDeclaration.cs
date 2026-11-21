using Lait.Ast.Expressions;

namespace Lait.Ast.Declarations;

/// <summary>
/// Объявление переменной типа int с обязательным начальным значением.
/// </summary>
public sealed class VariableDeclaration : AbstractVariableDeclaration
{
    public VariableDeclaration(string name, Expression initialValue)
        : base(name)
    {
        InitialValue = initialValue;
    }

    public Expression InitialValue { get; }

    public override void Accept(IAstVisitor visitor)
    {
        visitor.Visit(this);
    }
}