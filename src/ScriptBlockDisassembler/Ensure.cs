using System.Diagnostics.CodeAnalysis;

namespace ScriptBlockDisassembler;

internal static class Ensure
{
    public static void UnsupportedNotNull([NotNull] object? value, string fieldName)
    {
        if (value is null)
        {
            Throw.SomethingChanged($"{fieldName} is not null");
        }
    }
}
