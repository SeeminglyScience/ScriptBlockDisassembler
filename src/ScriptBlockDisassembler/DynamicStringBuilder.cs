using System;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Management.Automation.Language;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace ScriptBlockDisassembler;

internal class DynamicStringBuilder
{
    private readonly StringBuilder _sb;

    private readonly DynamicExpression _expression;

    public DynamicStringBuilder(DynamicExpression expression)
    {
        _sb = new();
        _expression = expression;
    }

    public DynamicStringBuilder Append(string value)
    {
        _sb.Append(value);
        return this;
    }

    private DynamicStringBuilder Append(object value)
    {
        _sb.Append(value);
        return this;
    }

    private DynamicStringBuilder Append(char value)
    {
        _sb.Append(value);
        return this;
    }

    private DynamicStringBuilder AppendTypeExpression(Type? type, string argName = "type")
    {
        if (type is null)
        {
            return Append(argName).Append(": null");
        }

        return Append("typeof(").AppendTypeName(type).Append(')');
    }

    internal DynamicStringBuilder AppendTypeName(Type type)
    {
        if (type == typeof(void)) return Append("void");
        if (type == typeof(short)) return Append("short");
        if (type == typeof(byte)) return Append("byte");
        if (type == typeof(int)) return Append("int");
        if (type == typeof(long)) return Append("long");
        if (type == typeof(sbyte)) return Append("sbyte");
        if (type == typeof(ushort)) return Append("ushort");
        if (type == typeof(uint)) return Append("uint");
        if (type == typeof(ulong)) return Append("ulong");
        if (type == typeof(float)) return Append("float");
        if (type == typeof(double)) return Append("double");
        if (type == typeof(decimal)) return Append("decimal");
        if (type == typeof(object)) return Append("object");
        if (type == typeof(char)) return Append("char");
        if (type == typeof(string)) return Append("string");
        if (type == typeof(nint)) return Append("nint");
        if (type == typeof(nuint)) return Append("nuint");
        if (type == typeof(bool)) return Append("bool");

        if (type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            return AppendTypeName(type.GetElementType()!).Append('?');
        }

        if (type.IsPointer)
        {
            return AppendTypeName(type.GetElementType()!).Append('*');
        }

        if (type.IsByRef)
        {
            return Append("ref ").AppendTypeName(type.GetElementType()!);
        }

        if (type.IsArray)
        {
            if (type.IsSZArray)
            {
                return AppendTypeName(type.GetElementType()!).Append("[]");
            }

            int rank = type.GetArrayRank();
            return AppendTypeName(type.GetElementType()!).
                Append('[').Append(new string(',', rank - 1)).Append(']');
        }

        if (!type.IsGenericType)
        {
            return Append(type.Name);
        }

        Type[] genericTypes = type.GetGenericArguments();
        Append(Regex.Replace(type.Name, @"`\d+$", string.Empty)).Append('<');
        if (type.IsConstructedGenericType)
        {
            AppendTypeName(genericTypes[0]);
            for (int i = 1; i < genericTypes.Length; i++)
            {
                Append(", ").AppendTypeName(genericTypes[i]);
            }

            return Append('>');
        }

        if (genericTypes.Length is 1)
        {
            return Append('>');
        }

        return Append(new string(',', genericTypes.Length - 1)).Append('>');
    }

    public DynamicStringBuilder AppendDynamicExpression()
    {
        Append("Fake.Dynamic<").AppendTypeName(_expression.DelegateType).Append(">(");
        return AppendBinder(_expression.Binder);
    }

    public override string ToString() => _sb.ToString();

    private DynamicStringBuilder AppendCallInfo(CallInfo? callInfo, string argName = "callInfo")
    {
        if (callInfo is null)
        {
            return Append(argName).Append(": null");
        }

        // Technically this ctor has two args but PowerShell never uses the
        // other so it's just clutter.
        return Append("new CallInfo(").Append(callInfo.ArgumentCount.ToString()).Append(")");
    }

    private DynamicStringBuilder AppendString(string value, string argName = "name")
    {
        if (value is null)
        {
            return Append(argName).Append(": null");
        }

        return Append('"').Append(value).Append('"');
    }

    private DynamicStringBuilder AppendTypeDefinitionAst(Type? type, string argName = "classScopeAst")
    {
        if (type is null)
        {
            return Append(argName).Append(": null");
        }

        return Append("Fake.Const<TypeDefinitionAst>(name: \"").Append(type.Name).Append("\")");
    }

    private DynamicStringBuilder AppendInvocationConstraints(object? constraints)
    {
        if (constraints is null)
        {
            return Append("constraints: null");
        }

        Type? methodTargetType = constraints.AccessProperty<Type>("MethodTargetType");
        Type[] parameterTypes = constraints.AccessProperty<Type[]>("ParameterTypes")!;
        if (methodTargetType is null && (parameterTypes is null || parameterTypes.All(t => t is null)))
        {
            // This isn't a thing, but the constructor for the average case is so noisy.
            return Append("PSMethodInvocationConstraints.Default");
        }

        Append("new PSMethodInvocationConstraints(");
        if (methodTargetType is not null)
        {
            AppendTypeExpression(methodTargetType);
        }
        else
        {
            Append("methodTargetType: null");
        }

        if (parameterTypes is null)
        {
            return Append(", parameterTypes: null)");
        }

        Append(", new Type[] { ");
        if (parameterTypes.Length is not 0)
        {
            AppendTypeExpression(parameterTypes[0]);
            for (int i = 1; i < parameterTypes.Length; i++)
            {
                Append(", ").AppendTypeExpression(parameterTypes[i]);
            }
        }

        return Append(" })");
    }

    internal DynamicStringBuilder AppendEnum(object enumValue)
    {
        return AppendEnum(enumValue.GetType().Name, enumValue);
    }

    internal DynamicStringBuilder AppendEnum(string enumName, object enumValue)
    {
        string value = enumValue.ToString()!;
        if (value.Contains(','))
        {
            return Append(enumName).Append(".")
                .Append(string.Join($" | {enumName}.", Regex.Split(value, ", ?")));
        }

        if (Regex.IsMatch(value, @"^\d+$"))
        {
            return Append('(').Append(enumName).Append(')').Append(value);
        }

        return Append(enumName).Append('.').Append(value);
    }

    private DynamicStringBuilder AppendBool(string argName, bool value)
    {
        Append(argName).Append(": ");
        if (value)
        {
            return Append("true");
        }

        return Append("false");
    }

    private DynamicStringBuilder AppendBinder(CallSiteBinder binder)
    {
        var binderName = binder.GetType().Name;
        bool isNoArgsGetMethods = binderName is "PSEnumerableBinder"
            or "PSToObjectArrayBinder"
            or "PSPipeWriterBinder"
            or "PSToStringBinder"
            or "PSPipelineResultToBoolBinder"
            or "PSCustomObjectConverter"
            or "PSDynamicConvertBinder"
            or "PSVariableAssignmentBinder";

        if (isNoArgsGetMethods)
        {
            return Append(binderName).Append(".Get()");
        }

        if (binderName is "ReservedMemberBinder")
        {
            return Append("new PSGetMemberBinder.ReservedMemberBinder(")
                .AppendString(binder.AccessProperty<string>("Name") ?? "").Append(", ")
                .AppendBool("ignoreCase", binder.AccessProperty<bool>("IgnoreCase")).Append(", ")
                .Append("static: false)");
        }

        if (binderName is "PSArrayAssignmentRHSBinder")
        {
            return Append(binderName).Append(".Get(").Append(binder.AccessField<int>("_elements")).Append(')');
        }

        if (binderName is "PSInvokeDynamicMemberBinder")
        {
            return Append(binderName).Append(".Get(")
                .AppendCallInfo(binder.AccessField<CallInfo>("_callInfo")).Append(", ")
                .AppendTypeDefinitionAst(binder.AccessField<Type>("_classScope")).Append(", ")
                .AppendBool("static", binder.AccessField<bool>("_static")).Append(", ")
                .AppendBool("propertySetter", binder.AccessField<bool>("_propertySetter")).Append(", ")
                .AppendInvocationConstraints(binder.AccessField("_constraints"))
                .Append(')');
        }

        if (binderName is "PSGetDynamicMemberBinder" or "PSSetDynamicMemberBinder")
        {
            return Append(binderName).Append(".Get(")
                .AppendTypeDefinitionAst(binder.AccessField<Type>("_classScope")).Append(", ")
                .AppendBool("static", binder.AccessField<bool>("_static")).Append(')');
        }

        if (binderName is "PSSwitchClauseEvalBinder")
        {
            return Append(binderName).Append(".Get(")
                .AppendEnum("SwitchFlags", binder.AccessField<SwitchFlags>("_flags")).Append(")");
        }

        if (binderName is "PSInvokeBinder" or "ComInvokeAction")
        {
            return Append($"new {binderName}(").AppendCallInfo(binder.AccessProperty<CallInfo>("CallInfo")).Append(")");
        }

        if (binderName is "SplatInvokeBinder")
        {
            return Append($"{binderName}.Instance");
        }

        if (binderName is "PSAttributeGenerator")
        {
            return Append(binderName).Append(".Get(")
                .AppendCallInfo(binder.AccessProperty<CallInfo>("CallInfo")).Append(')');
        }

        if (binderName is "PSBinaryOperationBinder")
        {
            return Append(binderName).Append(".Get(")
                .AppendEnum("ExpressionType", binder.AccessProperty<ExpressionType>("Operation")).Append(", ")
                .AppendBool("ignoreCase", binder.AccessField<bool>("_ignoreCase")).Append(',')
                .AppendBool("scalarCompare", binder.AccessField<bool>("_scalarCompare")).Append(')');
        }

        if (binderName is "PSUnaryOperationBinder")
        {
            return Append(binderName).Append(".Get(")
                .AppendEnum("ExpressionType", binder.AccessProperty<ExpressionType>("Operation")).Append(")");
        }

        if (binderName is "PSConvertBinder")
        {
            return Append(binderName).Append(".Get(")
                .AppendTypeExpression(binder.AccessProperty<Type>("Type")).Append(')');
        }

        if (binderName is "PSGetIndexBinder")
        {
            return Append(binderName).Append(".Get(")
                .Append("argCount: ").Append(binder.AccessProperty<CallInfo>("CallInfo")?.ArgumentCount ?? 0).Append(", ")
                .AppendInvocationConstraints(binder.AccessField<object>("_constraints")).Append(", ")
                .AppendBool("allowSlicing", binder.AccessField<bool>("_allowSlicing")).Append(')');
        }

        if (binderName is "PSSetIndexBinder")
        {
            return Append(binderName).Append(".Get(")
                .Append("argCount: ").Append(binder.AccessProperty<CallInfo>("CallInfo")?.ArgumentCount ?? 0).Append(", ")
                .AppendInvocationConstraints(binder.AccessField<object>("_constraints")).Append(')');
        }

        if (binderName is "PSGetMemberBinder")
        {
            return Append(binderName).Append(".Get(")
                .AppendString(binder.AccessProperty<string>("Name") ?? "").Append(", ")
                .AppendTypeExpression(binder.AccessField<Type>("_classScope")).Append(", ")
                .AppendBool("static", binder.AccessField<bool>("_static")).Append(", ")
                .AppendBool("nonEnumerating", binder.AccessField<bool>("_nonEnumerating")).Append(')');
        }

        if (binderName is "PSSetMemberBinder")
        {
            return Append(binderName).Append(".Get(")
                .AppendString(binder.AccessProperty<string>("Name") ?? "").Append(", ")
                .AppendTypeExpression(binder.AccessField<Type>("_classScope")).Append(", ")
                .AppendBool("static", binder.AccessField<bool>("_static")).Append(')');
        }

        if (binderName is "PSInvokeMemberBinder")
        {
            return Append(binderName).Append(".Get(")
                .AppendString(binder.AccessProperty<string>("Name") ?? "").Append(", ")
                .AppendTypeExpression(binder.AccessField<Type>("_classScope"), "classScope").Append(", ")
                .AppendCallInfo(binder.AccessProperty<CallInfo>("CallInfo")).Append(", ")
                .AppendBool("static", binder.AccessField<bool>("_static")).Append(", ")
                .AppendBool("nonEnumerating", binder.AccessField<bool>("_nonEnumerating")).Append(", ")
                .AppendInvocationConstraints(binder.AccessField<object>("_invocationConstraints")).Append(')');
        }

        if (binderName is "PSCreateInstanceBinder")
        {
            return Append(binderName).Append(".Get(")
                .AppendCallInfo(binder.AccessProperty<CallInfo>("CallInfo")).Append(", ")
                .AppendInvocationConstraints(binder.AccessField<object>("_constraints")).Append(", ")
                .AppendBool("publicTypeOnly", binder.AccessField<bool>("_publicTypeOnly")).Append(')');
        }

        if (binderName is "PSInvokeBaseCtorBinder")
        {
            return Append(binderName).Append(".Get(")
                .AppendCallInfo(binder.AccessProperty<CallInfo>("CallInfo")).Append(", ")
                .AppendInvocationConstraints(binder.AccessField<object>("_constraints")).Append(')');
        }

        return Append("Fake.UnhandledBinder<").Append(binderName).Append(">()");
    }
}
