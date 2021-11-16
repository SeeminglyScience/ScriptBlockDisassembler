using System;
using System.Linq.Expressions;
using AgileObjects.ReadableExpressions;
using AgileObjects.ReadableExpressions.Translations;
using System.Management.Automation;

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

    public static string Translate(ScriptBlock scriptBlock, string block, bool optimized)
    {
        scriptBlock.InvokePrivateMethod(
            "Compile",
            new object[] { optimized },
            new[] { typeof(bool) });

        string propertyName = block.ToLowerInvariant() switch
        {
            "begin" when optimized => "BeginBlock",
            "begin" when !optimized => "UnoptimizedBeginBlock",
            "process" when optimized => "ProcessBlock",
            "process" when !optimized => "UnoptimizedProcessBlock",
            "end" when optimized => "EndBlock",
            "end" when !optimized => "UnoptimizedEndBlock",
            _ => throw new ArgumentOutOfRangeException(nameof(block)),
        };

        Delegate? action = scriptBlock.AccessProperty<Delegate>(propertyName);
        Ensure.UnsupportedNotNull(action, propertyName);
        Ensure.UnsupportedNotNull(action.Target, $"{propertyName}.Target");

        object? delegateCreator = action.Target!.AccessField("_delegateCreator");
        Ensure.UnsupportedNotNull(delegateCreator, "_delegateCreator");

        Expression? lambda = delegateCreator.AccessField<Expression>("_lambda");
        Ensure.UnsupportedNotNull(lambda, "_lambda");
        return string.Concat(
            $"// ScriptBlock.{propertyName}",
            Environment.NewLine,
            Translate(lambda));
    }

    public static string Translate(Expression expression)
    {
        Expression reduced = new RecursiveReduce().Visit(expression);
        PSExpressionTranslation translator = new(
            reduced,
            (TranslationSettings)PSTranslationSettings.Default);

        return translator.GetTranslation();
    }

    public override ITranslation GetTranslationFor(Expression expression)
    {
        if (expression is DynamicExpression dynamic)
        {
            return DynamicTranslation.GetTranslationFor(dynamic, this);
        }

        return base.GetTranslationFor(expression);
    }
}
