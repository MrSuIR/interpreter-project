using Lait.Ast.Expressions;

using ValueType = Lait.Runtime.ValueType;

namespace Lait.Ast.Declarations;

/// <summary>
/// Объявление переменной с обязательным начальным значением.
/// </summary>
public sealed class VariableDeclaration : AbstractVariableDeclaration
{
    public VariableDeclaration(string name, ValueType type, Expression initialValue)
        : base(name, type)
    {
        InitialValue = initialValue;
    }

    public Expression InitialValue { get; }

    public override void Accept(IAstVisitor visitor)
    {
        visitor.Visit(this);
    }
}