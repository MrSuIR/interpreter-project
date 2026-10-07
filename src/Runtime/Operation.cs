namespace Lait.Runtime;

/// <summary>
/// Операции стековой виртуальной машины.
/// </summary>
public enum Operation
{
    Push,
    Add,
    Subtract,
    Multiply,
    Divide,
    Negate,
    Read,
    Print,
    Absolute,
    Minimum,
    Maximum,
    Discard,
}