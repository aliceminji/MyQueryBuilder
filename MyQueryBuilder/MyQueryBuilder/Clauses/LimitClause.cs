using System;
using System.Collections.Generic;
using System.Text;

namespace MyQueryBuilder.Clauses
{
    public class LimitClause : AbstractClause
    {
        private int _limit;

        public int Limit
        {
            get => _limit;
            set => _limit = value > 0 ? value : _limit;
        }
        public override AbstractClause Clone()
        {
            return new LimitClause
            {
                Engine = Engine,
                Limit = Limit,
                Component = Component,
            };
        }
    }
}
