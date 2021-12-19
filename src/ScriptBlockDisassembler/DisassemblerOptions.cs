namespace ScriptBlockDisassembler;

internal sealed record DisassemblerOptions(
    bool IgnoreUpdatePosition,
    bool Unoptimized,
    bool IgnoreStartupAndTeardown,
    bool IgnoreQuestionMarkVariable)
{
    public static DisassemblerOptions Default { get; } = new(
        IgnoreUpdatePosition: false,
        Unoptimized: false,
        IgnoreStartupAndTeardown: false,
        IgnoreQuestionMarkVariable: false);
}
