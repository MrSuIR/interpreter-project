namespace Lait.Ast;

/// <summary>
/// Надкласс для всей иерархии классов узлов синтаксического дерева.
/// </summary>
public abstract class AstNode
{
    /// <summary>
    /// Применяет посетителя к данному узлу дерева.
    /// </summary>
    public abstract void Accept(IAstVisitor visitor);
}