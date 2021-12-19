using System;
using System.Linq.Expressions;
using System.Management.Automation;
using System.Text.RegularExpressions;
using AgileObjects.ReadableExpressions;
using AgileObjects.ReadableExpressions.Translations;

namespace ScriptBlockDisassembler;

internal class PSExpressionTranslation : ExpressionTranslation
{
    public PSExpressionTranslation(Expression expression, TranslationSettings settings)
        : base(expression, settings)
    {
    }

    protected PSExpressionTranslation(ExpressionAnalysis expressionAnalysis, TranslationSettings settings)
        : base(expressionAnalysis, settings)
    {
    }

    public static string Translate(
        ScriptBlock scriptBlock,
        string block,
        DisassemblerOptions options)
    {
        Expression? lambda = GetExpressionForScriptBlock(
            scriptBlock,
            block,
            options,
            out string propertyName);

        return string.Concat(
            $"// ScriptBlock.{propertyName}",
            Environment.NewLine,
            Translate(lambda, options));
    }

    internal static Expression GetExpressionForScriptBlock(
        ScriptBlock scriptBlock,
        string block,
        DisassemblerOptions options,
        out string propertyName)
    {
        bool optimized = !options.Unoptimized;
        scriptBlock.InvokePrivateMethod(
            "Compile",
            new object[] { optimized },
            new[] { typeof(bool) });

        propertyName = block.ToLowerInvariant() switch
        {
            "begin" when optimized => "BeginBlock",
            "begin" when !optimized => "UnoptimizedBeginBlock",
            "process" when optimized => "ProcessBlock",
            "process" when !optimized => "UnoptimizedProcessBlock",
            "end" when optimized => "EndBlock",
            "end" when !optimized => "UnoptimizedEndBlock",
            "dynamicparam" when optimized => "DynamicParamBlock",
            "dynamicparam" when !optimized => "UnoptimizedDynamicParamBlock",
            "clean" when optimized => "CleanBlock",
            "clean" when !optimized => "UnoptimizedCleanBlock",
            _ => throw new ArgumentOutOfRangeException(nameof(block)),
        };

        Delegate? action = scriptBlock.AccessProperty<Delegate>(propertyName);
        Ensure.UnsupportedNotNull(action, propertyName);
        Ensure.UnsupportedNotNull(action.Target, $"{propertyName}.Target");

        object? delegateCreator = action.Target!.AccessField("_delegateCreator");
        Ensure.UnsupportedNotNull(delegateCreator, "_delegateCreator");

        Expression? lambda = delegateCreator.AccessField<Expression>("_lambda");
        Ensure.UnsupportedNotNull(lambda, "_lambda");
        return lambda;
    }

    public static string Translate(Expression expression, DisassemblerOptions options)
    {
        Expression reduced = new RecursiveReduce(options).Visit(expression);
        PSExpressionTranslation translator = new(
            reduced,
            (TranslationSettings)PSTranslationSettings.Default);

        if (!options.IgnoreStartupAndTeardown)
        {
            return translator.GetTranslation();
        }

        return Regex.Replace(
            translator.GetTranslation(),
            @"MutableTuple<.+?> locals;\r?\n",
            string.Empty);
    }

    public override ITranslation GetTranslationFor(Expression expression)
    {
        if (expression is DynamicExpression dynamic)
        {
            return DynamicTranslation.GetTranslationFor(dynamic, this);
        }

        if (expression is TypeBinaryExpression typeBinary)
        {
            return TypeIsTranslation.GetTranslationFor(typeBinary, this);
        }

        return base.GetTranslationFor(expression);
    }
}
