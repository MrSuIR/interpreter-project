using Lait.Runtime;

using ValueType = Lait.Runtime.ValueType;

namespace Lait.Ast.Declarations;

/// <summary>
/// Определение встроенной функции языка.
/// </summary>
public sealed class BuiltinFunction : AbstractFunctionDeclaration
{
    private readonly Func<IReadOnlyList<Value>, Value> _implementation;

    public BuiltinFunction(
        string name,
        IReadOnlyList<ParameterDeclaration> parameters,
        ValueType resultType,
        Func<IReadOnlyList<Value>, Value> implementation)
        : base(name, parameters, resultType)
    {
        _implementation = implementation;
    }

    /// <summary>
    /// Выполняет функцию. Функция с типом void возвращает Value.Void.
    /// </summary>
    public Value Invoke(IReadOnlyList<Value> arguments)
    {
        return _implementation(arguments);
    }

    /// <summary>
    /// Встроенная функция не входит в дерево программы, поэтому посетитель к ней не применяется.
    /// </summary>
    public override void Accept(IAstVisitor visitor)
    {
        throw new InvalidOperationException($"Посетитель не применяется к встроенной функции '{Name}'.");
    }
}