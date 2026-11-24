using ValueType = Lait.Runtime.ValueType;

namespace Lait.Ast.Declarations;

/// <summary>
/// Общая информация о переменной или параметре функции.
/// </summary>
public abstract class AbstractVariableDeclaration : Declaration
{
    protected AbstractVariableDeclaration(string name, ValueType type)
    {
        Name = name;
        Type = type;
    }

    public string Name { get; }

    /// <summary>
    /// Объявленный тип переменной или параметра.
    /// </summary>
    public ValueType Type { get; }
}