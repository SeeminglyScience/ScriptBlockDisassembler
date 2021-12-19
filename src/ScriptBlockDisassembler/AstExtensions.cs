using System;
using System.Management.Automation.Language;
using System.Reflection;

namespace ScriptBlockDisassembler
{
    internal static class AstExtensions
    {
        private static readonly Lazy<Func<ScriptBlockAst, NamedBlockAst?>> s_getCleanBlock
            = new(() =>
            {
                return typeof(ScriptBlockAst).GetProperty(
                    "CleanBlock",
                    BindingFlags.Instance | BindingFlags.Public)
                    ?.GetGetMethod()
                    ?.CreateDelegate<Func<ScriptBlockAst, NamedBlockAst>>()
                    ?? new Func<ScriptBlockAst, NamedBlockAst?>(_ => null);
            });

        public static NamedBlockAst? GetCleanBlock(this ScriptBlockAst scriptBlockAst)
            => s_getCleanBlock.Value(scriptBlockAst);
    }
}
