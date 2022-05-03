using System;
using System.Collections.Generic;
using System.Text;

namespace MyQueryBuilder.Clauses
{
    public abstract class AbstractCondition : AbstractClause
    {
        public bool IsOr { get; set; } = false;
        public bool IsNot { get; set; } = false;
    }

    public class NullCondition : AbstractCondition
    {
        public string Column { get; set; }

        /// <inheritdoc />
        public override AbstractClause Clone()
        {
            return new NullCondition
            {
                Engine = Engine,
                Column = Column,
                IsOr = IsOr,
                IsNot = IsNot,
                Component = Component,
            };
        }
    }

    public class InCondition<T> : AbstractCondition
    {
        public string Column { get; set; }
        public IEnumerable<T> Values { get; set; }
        public override AbstractClause Clone()
        {
            return new InCondition<T>
            {
                Engine = Engine,
                Column = Column,
                Values = new List<T>(Values),
                IsOr = IsOr,
                IsNot = IsNot,
                Component = Component,
            };
        }

    }
}
