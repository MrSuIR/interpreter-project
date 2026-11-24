using ValueType = Lait.Runtime.ValueType;

namespace Lait.Ast.Declarations;

/// <summary>
/// Общая информация о функции - встроенной или объявленной в программе.
/// </summary>
public abstract class AbstractFunctionDeclaration : Declaration
{
    protected AbstractFunctionDeclaration(string name, IReadOnlyList<ParameterDeclaration> parameters, ValueType resultType)
    {
        Name = name;
        Parameters = parameters;
        ResultType = resultType;
    }

    public string Name { get; }

    public IReadOnlyList<ParameterDeclaration> Parameters { get; }

    /// <summary>
    /// Тип результата функции; Void, если функция не возвращает значения.
    /// </summary>
    public ValueType ResultType { get; }

    /// <summary>
    /// Истина, если функция возвращает значение.
    /// </summary>
    public bool ReturnsValue => ResultType != ValueType.Void;
}