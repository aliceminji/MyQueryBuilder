using MyQueryBuilder.Clauses;
using System;
using System.Collections.Generic;
using System.Text;

namespace MyQueryBuilder
{
    public partial class Query : BaseQuery<Query>
    {
        private string comment;

        public bool IsDistinct { get; set; } = false;
        public string QueryAlias { get; set; }
        public string Method { get; set; } = "select";
        public List<Include> Includes = new List<Include>();
        public Dictionary<string, object> Variables = new Dictionary<string, object>();

        public Query() : base()
        {
        }
        public Query As(string alias)
        {
            QueryAlias = alias;
            return this;
        }

        public Query(string table, string comment = null) : base()
        {
            From(table);
            Comment(comment);
        }
        public Query Comment(string comment)
        {
            this.comment = comment;
            return this;
        }


        internal int GetLimit(string engineCode = null)
        {
            engineCode = engineCode ?? EngineScope;
            var limit = this.GetOneComponent<LimitClause>("limit", engineCode);

            return limit?.Limit ?? 0;
        }

        internal int GetOffset(string engineCode = null)
        {
            engineCode = engineCode ?? EngineScope;
            var offset = this.GetOneComponent<OffsetClause>("offset", engineCode);

            return offset?.Offset ?? 0;
        }

        public object FindVariable(string variable)
        {
            var found = Variables.ContainsKey(variable);

            if (found)
            {
                return Variables[variable];
            }

            if (Parent != null)
            {
                return (Parent as Query).FindVariable(variable);
            }

            throw new Exception($"Variable '{variable}' not found");
        }

        public override Query NewQuery()
        {
            return new Query();
        }

        public override Query Clone()
        {
            var clone = base.Clone();
            clone.Parent = Parent;
            clone.QueryAlias = QueryAlias;
            clone.IsDistinct = IsDistinct;
            clone.Method = Method;
            clone.Includes = Includes;
            clone.Variables = Variables;
            return clone;
        }
    }
}
