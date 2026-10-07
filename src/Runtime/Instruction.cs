namespace Lait.Runtime;

/// <summary>
/// Одна инструкция VM; операнд используется только операцией Push.
/// </summary>
public readonly record struct Instruction(Operation Operation, int Operand = 0);