using Lait.Ast.Attributes;

using ValueType = Lait.Runtime.ValueType;

namespace Lait.Ast.Expressions;

/// <summary>
/// Надкласс для узлов выражений.
/// </summary>
public abstract class Expression : AstNode
{
    private AstAttribute<ValueType> _resultType;

    /// <summary>
    /// Тип значения выражения; устанавливается при проверке программы.
    /// </summary>
    public ValueType ResultType
    {
        get => _resultType.Get();
        set => _resultType.Set(value);
    }
}