using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace ScriptBlockDisassembler
{
    internal static class Throw
    {
        [DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
        public static void SomethingChanged(string expected)
        {
            throw new InvalidOperationException(
                $"An unsupported implementation detail has unsurprisingly changed. Expected: {expected}");
        }

        [DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
        public static void Unreachable()
        {
            throw new InvalidOperationException("This program location is thought to be unreachable.");
        }

        [DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
        public static T Unreachable<T>()
        {
            throw new InvalidOperationException("This program location is thought to be unreachable.");
        }
    }
}
