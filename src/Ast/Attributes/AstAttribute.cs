using System.Runtime.CompilerServices;

namespace Lait.Ast.Attributes;

/// <summary>
/// Атрибут узла AST. Устанавливается один раз на этапе проверки программы, после чего доступен для чтения.
/// </summary>
/// <typeparam name="T">Тип значения атрибута.</typeparam>
public struct AstAttribute<T>
{
    private T _value;
    private bool _initialized;

    public readonly T Get([CallerMemberName] string? memberName = null)
    {
        if (!_initialized)
        {
            throw new InvalidOperationException($"Атрибут {memberName} ещё не установлен.");
        }

        return _value;
    }

    public void Set(T value, [CallerMemberName] string? memberName = null)
    {
        if (_initialized)
        {
            throw new InvalidOperationException($"Атрибут {memberName} уже установлен.");
        }

        _value = value;
        _initialized = true;
    }
}