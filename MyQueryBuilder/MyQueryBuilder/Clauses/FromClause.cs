using MyQueryBuilder.Compilers;
using System;
using System.Collections.Generic;
using System.Text;

namespace MyQueryBuilder.Clauses
{

    public abstract class AbstractFrom : AbstractClause
    {
        protected string _alias;

        /// <summary>
        /// Try to extract the Alias for the current clause.
        /// </summary>
        /// <returns></returns>
        public virtual string Alias { get => _alias; set => _alias = value; }
    }

    public class FromClause : AbstractFrom
    {
        public string Table { get; set; }

        public override string Alias
        {
            get
            {
                if (Table.IndexOf(" as ", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var segments = Table.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    return segments[2];
                }

                return Table;
            }
        }

        public override AbstractClause Clone()
        {
            return new FromClause
            {
                Engine = Engine,
                Alias = Alias,
                Table = Table,
                Component = Component,
            };
        }
    }

    public class QueryFromClause : AbstractFrom
    {
        public Query Query { get; set; }

        public override string Alias
        {
            get
            {
                return string.IsNullOrEmpty(_alias) ? Query.QueryAlias : _alias;
            }
        }

        /// <inheritdoc />
        public override AbstractClause Clone()
        {
            return new QueryFromClause
            {
                Engine = Engine,
                Alias = Alias,
                Query = Query.Clone(),
                Component = Component,
            };
        }
    }

    public class RawFromClause : AbstractFrom
    {
        public string Expression { get; set; }
        public object[] Bindings { set; get; }

        /// <inheritdoc />
        public override AbstractClause Clone()
        {
            return new RawFromClause
            {
                Engine = Engine,
                Alias = Alias,
                Expression = Expression,
                Bindings = Bindings,
                Component = Component,
            };
        }
    }

    public class AdHocTableFromClause : AbstractFrom
    {
        public List<string> Columns { get; set; }
        public List<object> Values { get; set; }

        public override AbstractClause Clone()
        {
            return new AdHocTableFromClause
            {
                Engine = Engine,
                Alias = Alias,
                Columns = Columns,
                Values = Values,
                Component = Component,
            };
        }
    }
}
