using System.Linq.Expressions;
using System.Text.RegularExpressions;
using AgileObjects.ReadableExpressions.Translations;

namespace ScriptBlockDisassembler;

internal sealed class DynamicTranslation : ITranslation
{
    private readonly DynamicExpression _expression;

    private readonly string _value;

    private readonly ITranslation[] _arguments;

    private readonly Lazy<int> _argumentsLineCount;

    private DynamicTranslation(string value, DynamicExpression expression, ITranslation[] arguments)
    {
        _value = value;
        _expression = expression;
        _arguments = arguments;
        _argumentsLineCount = new(() =>
        {
            int total = 0;
            foreach (ITranslation argument in _arguments)
            {
                int current = argument.GetLineCount();
                if (argument.NodeType is ExpressionType.Block)
                {
                    current += 2;
                }

                total += current;
            }

            if (total == _arguments.Length && _arguments.Length < 3)
            {
                return 0;
            }

            return total;
        });
    }

    public static ITranslation GetTranslationFor(Expression expression, ExpressionTranslation translator)
    {
        if (expression is not DynamicExpression dynamicExpression)
        {
            return translator.GetTranslationFor(expression);
        }

        ITranslation[] arguments = new ITranslation[dynamicExpression.Arguments.Count];
        for (int i = 0; i < arguments.Length; i++)
        {
            arguments[i] = translator.GetTranslationFor(dynamicExpression.Arguments[i]);
        }

        DynamicStringBuilder sb = new(dynamicExpression);
        return new DynamicTranslation(
            sb.AppendDynamicExpression().ToString(),
            dynamicExpression,
            arguments);
    }

    public ExpressionType NodeType => ExpressionType.Dynamic;

    public Type Type => _expression.Type;

    public int TranslationSize => _value.Length
        + (_arguments.Length * 2)
        + _arguments.Sum(arg => arg.TranslationSize)
        + 1;

    public int FormattingSize => 0;

    public int GetIndentSize() => 0;

    public int GetLineCount() => Regex.Matches(_value, @"\r?\n").Count + _argumentsLineCount.Value;

    public void WriteTo(TranslationWriter writer)
    {
        writer.WriteToTranslation(_value);
        if (_arguments.Length is 0)
        {
            writer.WriteToTranslation(")()");
            return;
        }

        writer.WriteToTranslation(")(");
        if (_argumentsLineCount.Value is not 0)
        {
            writer.WriteNewLineToTranslation();
            writer.Indent();
        }

        WriteArgument(_arguments[0], writer);
        for (int i = 1; i < _arguments.Length; i++)
        {
            writer.WriteToTranslation(",");
            if (_argumentsLineCount.Value is 0)
            {
                writer.WriteToTranslation(" ");
            }
            else
            {
                writer.WriteNewLineToTranslation();
            }

            WriteArgument(_arguments[i], writer);
        }

        writer.WriteToTranslation(")");
        if (_argumentsLineCount.Value is not 0)
        {
            writer.Unindent();
        }

        static void WriteArgument(ITranslation translation, TranslationWriter writer)
        {
            if (translation.NodeType is ExpressionType.Block)
            {
                writer.WriteToTranslation("{");
                writer.WriteNewLineToTranslation();
                writer.Indent();
            }

            translation.WriteTo(writer);
            if (translation.NodeType is ExpressionType.Block)
            {
                writer.WriteNewLineToTranslation();
                writer.Unindent();
                writer.WriteToTranslation("}");
            }
        }
    }
}
