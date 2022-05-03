﻿using System;
using System.Collections.Generic;
using System.Text;

namespace MyQueryBuilder.Clauses
{
    public class AggregateClause : AbstractClause
    {
        /// <summary>
        /// Gets or sets columns that used in aggregate clause.
        /// </summary>
        /// <value>
        /// The columns to be aggregated.
        /// </value>
        public List<string> Columns { get; set; }

        /// <summary>
        /// Gets or sets the type of aggregate function.
        /// </summary>
        /// <value>
        /// The type of aggregate function, e.g. "MAX", "MIN", etc.
        /// </value>
        public string Type { get; set; }

        /// <inheritdoc />
        public override AbstractClause Clone()
        {
            return new AggregateClause
            {
                Engine = Engine,
                Type = Type,
                Columns = new List<string>(Columns),
                Component = Component,
            };
        }
    }
}
