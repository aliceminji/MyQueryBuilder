using System;
using System.Collections.Generic;
using System.Text;

namespace MyQueryBuilder.Clauses
{
    public class OffsetClause : AbstractClause
    {
        private int _offset;

        public int Offset
        {
            get => _offset;
            set => _offset = value > 0 ? value : _offset;
        }

        public override AbstractClause Clone()
        {
            return new OffsetClause
            {
                Engine = Engine,
                Offset = Offset,
                Component = Component,
            };
        }
    }
}
