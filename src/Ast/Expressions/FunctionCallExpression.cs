using Lait.Ast.Attributes;
using Lait.Ast.Declarations;

namespace Lait.Ast.Expressions;

/// <summary>
/// Вызов функции со списком аргументов.
/// </summary>
public sealed class FunctionCallExpression : Expression
{
    private AstAttribute<AbstractFunctionDeclaration> _function;

    public FunctionCallExpression(string name, IReadOnlyList<Expression> arguments)
    {
        Name = name;
        Arguments = arguments;
    }

    public string Name { get; }

    public IReadOnlyList<Expression> Arguments { get; }

    /// <summary>
    /// Объявление вызываемой функции; устанавливается при проверке программы.
    /// </summary>
    public AbstractFunctionDeclaration Function
    {
        get => _function.Get();
        set => _function.Set(value);
    }

    public override void Accept(IAstVisitor visitor)
    {
        visitor.Visit(this);
    }
}