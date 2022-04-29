using System;
using System.Collections.Generic;
using System.Text;

namespace MyQueryBuilder.Compilers
{
    public partial class Compiler
    {
        protected virtual string parameterPlaceholder { get; set; } = "?";
        protected virtual string parameterPrefix { get; set; } = "@p";
        protected virtual string OpeningIdentifier { get; set; } = "\"";
        protected virtual string ClosingIdentifier { get; set; } = "\"";
        protected virtual string ColumnAsKeyword { get; set; } = "AS ";
        protected virtual string TableAsKeyword { get; set; } = "AS ";
        protected virtual string LastId { get; set; } = "";
        protected virtual string EscapeCharacter { get; set; } = "\\";

        public virtual string EngineCode { get; }

        public virtual string CompileLimit(ResultData ctx)
        {
            var limit = ctx.Query.GetLimit(EngineCode);
            var offset = ctx.Query.GetOffset(EngineCode);

            if (limit == 0 && offset == 0)
            {
                return null;
            }

            if (offset == 0)
            {
                ctx.Bindings.Add(limit);
                return "LIMIT ?";
            }

            if (limit == 0)
            {
                ctx.Bindings.Add(offset);
                return "OFFSET ?";
            }

            ctx.Bindings.Add(limit);
            ctx.Bindings.Add(offset);

            return "LIMIT ? OFFSET ?";
        }
    }
}
