using MyQueryBuilder.Clauses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyQueryBuilder
{
    public abstract partial class BaseQuery<Q>
    {

        public Q WhereNotNull(string column)
        {
            return Not().WhereNull(column);
        }

        public Q WhereNull(string column)
        {
            return AddComponent("where", new NullCondition
            {
                Column = column,
                IsOr = GetOr(),
                IsNot = GetNot(),
            });
        }

        public Q WhereIn<T>(string column, IEnumerable<T> values)
        {

            // If the developer has passed a string they most likely want a List<string>
            // since string is considered as List<char>
            if (values is string)
            {
                string val = values as string;

                return AddComponent("where", new InCondition<string>
                {
                    Column = column,
                    IsOr = GetOr(),
                    IsNot = GetNot(),
                    Values = new List<string> { val }
                });
            }

            return AddComponent("where", new InCondition<T>
            {
                Column = column,
                IsOr = GetOr(),
                IsNot = GetNot(),
                Values = values.Distinct().ToList()
            });


        }

    }
}
