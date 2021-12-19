using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Collections.Concurrent;

namespace ScriptBlockDisassembler;

internal class RecursiveReduce : ExpressionVisitor
{
    private readonly DisassemblerOptions _options;

    private int _untitledVariableId;

    private int _untitledLabelId;

    private readonly ConcurrentDictionary<LabelTarget, LabelTarget> _map = new();

    public RecursiveReduce(DisassemblerOptions options) => _options = options;

    public static Expression DefaultVisit(Expression node)
    {
        if (node is not Expression result)
        {
            return null!;
        }

        if (result.Reduce() is not Expression reduced)
        {
            return result;
        }

        return reduced;
    }

    private LabelTarget GetOrAddLabel(LabelTarget label)
    {
        return _map.GetOrAdd(
            label,
            _ => Expression.Label(label.Type, $"unnamed_label_{_untitledLabelId++}"));
    }

    protected override CatchBlock VisitCatchBlock(CatchBlock node)
    {
        if (node.Variable is { Name: null or "" })
        {
            typeof(ParameterExpression)
                .GetField("<Name>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(node.Variable, $"unnamedVar{_untitledVariableId++}");
        }

        return base.VisitCatchBlock(node);
    }

    protected override LabelTarget? VisitLabelTarget(LabelTarget? node)
    {
        if (node is null)
        {
            return null;
        }

        if (node.Name is null or "")
        {
            return GetOrAddLabel(node);
        }

        return node;
    }

    protected override Expression VisitDynamic(DynamicExpression node)
    {
        return node.Update(node.Arguments.Select(e => Visit(e)));
    }

    protected override Expression VisitExtension(Expression node)
    {
        if (node is DynamicExpression dynamicExpression)
        {
            return VisitDynamic(dynamicExpression);
        }

        if (_options.IgnoreUpdatePosition && node.GetType().Name is "UpdatePositionExpr")
        {
            return Expression.Empty();
        }

        return base.Visit(node.Reduce());
    }

    protected override ElementInit VisitElementInit(ElementInit node)
        => node.Update(node.Arguments.Select(e => Visit(e)));

    protected override MemberAssignment VisitMemberAssignment(MemberAssignment node)
    {
        var result = Visit(node.Expression);
        if (result == node.Expression)
        {
            return node;
        }

        return node.Update(result);
    }

    protected override MemberListBinding VisitMemberListBinding(MemberListBinding node)
        => node.Update(
            node.Initializers.Select(init => init.Update(init.Arguments.Select(e => Visit(e)))));

    protected override SwitchCase VisitSwitchCase(SwitchCase node)
    {
        return node.Update(
            node.TestValues.Select(e => Visit(e)),
            Visit(node.Body));
    }

    protected override Expression VisitGoto(GotoExpression node)
    {
        if (node.Target.Name is null or "")
        {
            node.Update(
                GetOrAddLabel(node.Target),
                base.Visit(node.Value));
        }

        return base.VisitGoto((GotoExpression)node.Reduce());
    }

    protected override Expression VisitBlock(BlockExpression node)
    {
        Expression[] children = node.Expressions
            .Select(child => Visit(child))
            .Where(child => !(child is DefaultExpression defaultExpr && defaultExpr.Type == typeof(void)))
            .ToArray();

        if (children is { Length: 0 })
        {
            return node.Update(
                node.Variables.Select(child => Visit(child)).Cast<ParameterExpression>(),
                node.Expressions.Select(child => Visit(child)));
        }

        if (children is { Length: 1 } && children[0].Type == node.Type)
        {
            return children[0];
        }

        return node.Update(
            node.Variables.Select(child => Visit(child)).Cast<ParameterExpression>(),
            children);
    }

    protected override Expression VisitLambda<T>(Expression<T> node)
    {
        if (!_options.IgnoreStartupAndTeardown)
        {
            return base.VisitLambda((Expression<T>)node.Reduce());
        }

        bool isInitialTryFinally =
            node.Body is BlockExpression { Expressions.Count: 1 } block
            && block.Expressions[0] is TryExpression tryExpression
            && tryExpression.Handlers is { Count: 0 }
            && tryExpression.Finally is MethodCallExpression methodCall
            && methodCall.Method.Name is "ExitScriptFunction";

        if (isInitialTryFinally)
        {
            return Visit(((TryExpression)((BlockExpression)node.Body).Expressions[0]).Body);
        }

        return base.VisitLambda((Expression<T>)node.Reduce());
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        var shouldSkip = _options.IgnoreStartupAndTeardown
            && node.NodeType is ExpressionType.Assign
            && (
                (node.Left is ParameterExpression parameter && parameter.Name is "context" or "locals")
                || (node.Left is MemberExpression member && member.Member.Name is "_functionName"));

        if (shouldSkip)
        {
            return Expression.Empty();
        }

        shouldSkip = _options.IgnoreQuestionMarkVariable
            && node.NodeType is ExpressionType.Assign
            && node.Left is MemberExpression member2 && member2.Member.Name is "QuestionMarkVariableValue";

        if (shouldSkip)
        {
            return Expression.Empty();
        }

        return base.VisitBinary((BinaryExpression)node.Reduce());
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (_options.IgnoreStartupAndTeardown && node.Method.Name is "EnterScriptFunction")
        {
            return Expression.Empty();
        }

        return base.VisitMethodCall((MethodCallExpression)node.Reduce());
    }

    protected override Expression VisitConditional(ConditionalExpression node)
        => base.VisitConditional((ConditionalExpression)node.Reduce());

    protected override Expression VisitConstant(ConstantExpression node)
        => base.VisitConstant((ConstantExpression)node.Reduce());

    protected override Expression VisitDebugInfo(DebugInfoExpression node)
        => base.VisitDebugInfo((DebugInfoExpression)node.Reduce());

    protected override Expression VisitDefault(DefaultExpression node)
        => base.VisitDefault((DefaultExpression)node.Reduce());

    protected override Expression VisitIndex(IndexExpression node)
        => base.VisitIndex((IndexExpression)node.Reduce());

    protected override Expression VisitInvocation(InvocationExpression node)
        => base.VisitInvocation((InvocationExpression)node.Reduce());

    protected override Expression VisitLabel(LabelExpression node)
        => base.VisitLabel((LabelExpression)node.Reduce());

    protected override Expression VisitListInit(ListInitExpression node)
        => base.VisitListInit((ListInitExpression)node.Reduce());

    protected override Expression VisitLoop(LoopExpression node)
        => base.VisitLoop((LoopExpression)node.Reduce());

    protected override Expression VisitMember(MemberExpression node)
        => base.VisitMember((MemberExpression)node.Reduce());

    protected override Expression VisitMemberInit(MemberInitExpression node)
        => base.VisitMemberInit((MemberInitExpression)node.Reduce());

    protected override Expression VisitNew(NewExpression node)
        => base.VisitNew((NewExpression)node.Reduce());

    protected override Expression VisitNewArray(NewArrayExpression node)
        => base.VisitNewArray((NewArrayExpression)node.Reduce());

    protected override Expression VisitParameter(ParameterExpression node)
        => base.VisitParameter((ParameterExpression)node.Reduce());

    protected override Expression VisitRuntimeVariables(RuntimeVariablesExpression node)
        => base.VisitRuntimeVariables((RuntimeVariablesExpression)node.Reduce());

    protected override Expression VisitSwitch(SwitchExpression node)
        => base.VisitSwitch((SwitchExpression)node.Reduce());

    protected override Expression VisitTry(TryExpression node)
        => base.VisitTry((TryExpression)node.Reduce());

    protected override Expression VisitTypeBinary(TypeBinaryExpression node)
        => base.VisitTypeBinary((TypeBinaryExpression)node.Reduce());

    protected override Expression VisitUnary(UnaryExpression node)
        => base.VisitUnary((UnaryExpression)node.Reduce());
}
