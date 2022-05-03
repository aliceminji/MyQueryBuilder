using MyQueryBuilder.Clauses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyQueryBuilder
{
    public partial class Query
    {
        public Query Select(params string[] columns)
        {
            return Select(columns.AsEnumerable());
        }

        public Query Select(IEnumerable<string> columns)
        {
            Method = "select";

            columns = columns
                .Select(x => Helper.ExpandExpression(x))
                .SelectMany(x => x)
                .ToArray();


            foreach (var column in columns)
            {
                AddComponent("select", new Column
                {
                    Name = column
                });
            }

            return this;
        }
    }
}
