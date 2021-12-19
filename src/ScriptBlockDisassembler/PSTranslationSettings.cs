using System;
using System.Management.Automation;
using System.Management.Automation.Internal;
using AgileObjects.ReadableExpressions;

namespace ScriptBlockDisassembler;

public class PSTranslationSettings : TranslationSettings
{
    public static ITranslationSettings Default
    {
        get
        {
            var settings = (ITranslationSettings)new PSTranslationSettings();
            return settings.ShowLambdaParameterTypes
                .ShowImplicitArrayTypes
                .ShowQuotedLambdaComments
                .UseExplicitTypeNames
                .UseExplicitGenericParameters
                .IndentUsing("    ")
                .TranslateConstantsUsing(
                    (type, value) => ConstToString(type, value));
        }
    }

    private static string ConstToString(Type type, object? value)
    {
        DynamicStringBuilder text = new(null!);
        return ConstToString(type, value, text).ToString();
    }

    private static DynamicStringBuilder ConstToString(Type type, object? value, DynamicStringBuilder text)
    {
        if (value is null)
        {
            return text.Append("null");
        }

        Type actualType = value.GetType();
        if (value is PSObject pso && pso == AutomationNull.Value)
        {
            return text.Append("AutomationNull.Value");
        }

        if (type == typeof(bool))
        {
            return text.Append((bool)value ? "true" : "false");
        }

        if (type == typeof(char))
        {
            return text.Append($"'{value}'");
        }

        if (type.IsPrimitive)
        {
            return text.Append(value.ToString() ?? Throw.Unreachable<string>());
        }

        if (type == typeof(string))
        {
            if (((string)value).StartsWith("// Debug to"))
            {
                return text.Append((string)value);
            }

            return text.Append($"\"{value}\"");
        }

        if (actualType.IsEnum)
        {
            return text.AppendEnum(value);
        }

        text.Append("Fake.Const<")
            .AppendTypeName(type)
            .Append(">")
            .Append("(");

        if (value is Type staticType)
        {
            return text.Append("typeof(").AppendTypeName(staticType).Append("))");
        }

        if (type.IsArray)
        {
            Type elementType = type.GetElementType()!;
            Array asArray = (Array)value;
            if (asArray.Length is 0)
            {
                return text.Append("/* empty */)");
            }

            ConstToString(elementType, asArray.GetValue(0), text);
            for (int i = 1; i < asArray.Length; i++)
            {
                text.Append(", ");
                ConstToString(elementType, asArray.GetValue(i), text);
            }

            return text.Append(")");
        }

        if (actualType != type)
        {
            text.Append("typeof(").AppendTypeName(value.GetType()).Append("), ");
        }

        if (actualType.Name.Equals("ScriptBlockExpressionWrapper"))
        {
            return text.Append($"\"{value.AccessField("_ast")}\")");
        }

        string? toStringValue = value.ToString();
        if (toStringValue is null or "")
        {
            return text.Append("\"_empty<").AppendTypeName(value.GetType()).Append(">\")");
        }

        if (toStringValue.Equals(value.GetType().ToString(), StringComparison.Ordinal))
        {
            return text.Append("\"_defaultToString<").AppendTypeName(value.GetType()).Append(">\")");
        }

        return text.Append($"\"{value}\")");
    }
}
