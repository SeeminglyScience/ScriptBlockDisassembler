using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Collections.Concurrent;

namespace ScriptBlockDisassembler;

internal class RecursiveReduce : ExpressionVisitor
{
    private int _untitledVariableId;

    private int _untitledLabelId;

    private readonly ConcurrentDictionary<LabelTarget, LabelTarget> _map = new();

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

    protected override Expression VisitLambda<T>(Expression<T> node)
        => base.VisitLambda((Expression<T>)node.Reduce());

    protected override Expression VisitListInit(ListInitExpression node)
        => base.VisitListInit((ListInitExpression)node.Reduce());

    protected override Expression VisitLoop(LoopExpression node)
        => base.VisitLoop((LoopExpression)node.Reduce());

    protected override Expression VisitMember(MemberExpression node)
        => base.VisitMember((MemberExpression)node.Reduce());

    protected override Expression VisitMemberInit(MemberInitExpression node)
        => base.VisitMemberInit((MemberInitExpression)node.Reduce());

    protected override Expression VisitMethodCall(MethodCallExpression node)
        => base.VisitMethodCall((MethodCallExpression)node.Reduce());

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
