using Lait.Ast.Expressions;

namespace Lait.Ast.Statements;

/// <summary>
/// Цикл for с инициализатором, условием и шагом; образует область видимости.
/// </summary>
public sealed class ForLoopStatement : Statement
{
    public ForLoopStatement(Statement initializer, Expression condition, ExpressionStatement step, BlockStatement body)
    {
        Initializer = initializer;
        Condition = condition;
        Step = step;
        Body = body;
    }

    /// <summary>
    /// Объявление переменной цикла или инструкция-выражение.
    /// </summary>
    public Statement Initializer { get; }

    public Expression Condition { get; }

    /// <summary>
    /// Шаг цикла; его результат отбрасывается, как у инструкции-выражения.
    /// </summary>
    public ExpressionStatement Step { get; }

    public BlockStatement Body { get; }

    public override void Accept(IAstVisitor visitor)
    {
        visitor.Visit(this);
    }
}