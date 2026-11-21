namespace Lait.Ast.Declarations;

/// <summary>
/// Общая информация о переменной или параметре функции.
/// </summary>
public abstract class AbstractVariableDeclaration : Declaration
{
    protected AbstractVariableDeclaration(string name)
    {
        Name = name;
    }

    public string Name { get; }
}