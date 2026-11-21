namespace Lait.Ast.Declarations;

/// <summary>
/// Общая информация о функции - встроенной или объявленной в программе.
/// </summary>
public abstract class AbstractFunctionDeclaration : Declaration
{
    protected AbstractFunctionDeclaration(string name, bool returnsValue)
    {
        Name = name;
        ReturnsValue = returnsValue;
    }

    public string Name { get; }

    /// <summary>
    /// Истина, если функция возвращает int, и ложь, если её результат имеет тип void.
    /// </summary>
    public bool ReturnsValue { get; }

    /// <summary>
    /// Число параметров функции; все параметры имеют тип int.
    /// </summary>
    public abstract int ParameterCount { get; }
}