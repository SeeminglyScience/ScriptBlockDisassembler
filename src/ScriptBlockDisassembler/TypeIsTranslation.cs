using System;
using System.Linq.Expressions;
using AgileObjects.ReadableExpressions.Translations;

namespace ScriptBlockDisassembler
{
    internal sealed class TypeIsTranslation : ITranslation
    {
        private readonly string _typeName;

        private readonly ITranslation _operand;

        private TypeIsTranslation(ITranslation operand, Type type)
        {
            _typeName = new DynamicStringBuilder(null!).AppendTypeName(type).ToString();
            _operand = operand;
            TranslationSize = operand.TranslationSize
                + _typeName.Length
                + " is ".Length;

            FormattingSize = TranslationSize;
        }

        public ExpressionType NodeType => ExpressionType.TypeIs;

        public Type Type => typeof(bool);

        public int TranslationSize { get; }

        public int FormattingSize { get; }

        internal static ITranslation GetTranslationFor(TypeBinaryExpression typeBinary, PSExpressionTranslation translation)
        {
            return new TypeIsTranslation(
                translation.GetTranslationFor(typeBinary.Expression),
                typeBinary.TypeOperand);
        }

        public int GetIndentSize() => _operand.GetIndentSize();

        public int GetLineCount() => _operand.GetLineCount();

        public void WriteTo(TranslationWriter writer)
        {
            _operand.WriteTo(writer);
            writer.WriteToTranslation(" is ");
            writer.WriteToTranslation(_typeName);
        }
    }
}
