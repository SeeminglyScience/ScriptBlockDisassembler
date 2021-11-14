using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text;

namespace ScriptBlockDisassembler
{
    [Cmdlet(VerbsCommon.Get, "ScriptBlockDisassembly")]
    public sealed class GetScriptBlockDisassemblyCommand : PSCmdlet
    {
        private static readonly string[] s_defaultBlocks =
        {
            "begin",
            "process",
            "end",
        };

        [Parameter(Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNull]
        public ScriptBlock ScriptBlock { get; set; } = null!;

        [Parameter(Position = 0)]
        [ValidateSet("begin", "process", "end")]
        public string[] Block { get; set; } = null!;

        [Parameter]
        public SwitchParameter Unoptimized { get; set; }

        protected override void ProcessRecord()
        {
            bool omitErrors = false;
            if (Block is not { Length: > 0 })
            {
                Block = s_defaultBlocks;
                omitErrors = true;
            }

            StringBuilder text = new();
            bool first = true;
            foreach (string block in Block)
            {
                string? result = ProcessBlock(block, omitErrors);
                if (result is null)
                {
                    continue;
                }

                if (first)
                {
                    first = false;
                }
                else
                {
                    text.AppendLine().AppendLine();
                }

                text.Append(result);
            }

            WriteObject(text.ToString());
        }

        private string? ProcessBlock(string block, bool omitErrors)
        {
            bool blockExists = block.ToLowerInvariant() switch
            {
                "begin" => GetBody(ScriptBlock).BeginBlock is not null,
                "process" => GetBody(ScriptBlock).ProcessBlock is not null,
                "end" => GetBody(ScriptBlock).EndBlock is not null,
                _ => Throw.Unreachable<bool>(),
            };

            if (!blockExists)
            {
                if (!omitErrors)
                {
                    WriteError(
                        new ErrorRecord(
                            new PSArgumentException(
                                $"The specified named block '{block}' does not exist for this scriptblock.",
                                nameof(block)),
                            "BlockNotFound",
                            ErrorCategory.InvalidArgument,
                            ScriptBlock));
                }

                return null;
            }

            return PSExpressionTranslation.Translate(
                ScriptBlock!,
                block,
                !Unoptimized);

            static ScriptBlockAst GetBody(ScriptBlock scriptBlock)
            {
                if (scriptBlock.Ast is FunctionDefinitionAst functionDefinitionAst)
                {
                    return functionDefinitionAst.Body;
                }

                return (ScriptBlockAst)scriptBlock.Ast;
            }
        }
    }
}
