using Lait.Ast.Attributes;
using Lait.Ast.Declarations;

namespace Lait.Ast.Expressions;

/// <summary>
/// Обращение к переменной или параметру по имени.
/// </summary>
public sealed class VariableAccessExpression : Expression
{
    private AstAttribute<AbstractVariableDeclaration> _variable;

    public VariableAccessExpression(string name)
    {
        Name = name;
    }

    public string Name { get; }

    /// <summary>
    /// Объявление переменной, к которой относится имя; устанавливается при проверке программы.
    /// </summary>
    public AbstractVariableDeclaration Variable
    {
        get => _variable.Get();
        set => _variable.Set(value);
    }

    public override void Accept(IAstVisitor visitor)
    {
        visitor.Visit(this);
    }
}