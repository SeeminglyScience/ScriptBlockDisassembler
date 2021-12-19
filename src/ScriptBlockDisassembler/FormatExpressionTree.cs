using System.Diagnostics;
using System.Linq.Expressions;
using System.Management.Automation;

namespace ScriptBlockDisassembler;

[Cmdlet(VerbsCommon.Format, "ExpressionTree")]
public sealed class FormatExpressionTree : PSCmdlet
{
    [Parameter(Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
    [ValidateNotNull]
    public Expression? Expression { get; set; }

    protected override void ProcessRecord()
    {
        Debug.Assert(Expression is not null);
        WriteObject(
            PSExpressionTranslation.Translate(
                Expression,
                DisassemblerOptions.Default));
    }
}
