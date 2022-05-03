using System;
using System.Collections.Generic;
using System.Text;

namespace MyQueryBuilder
{
    public class UnsafeLiteral
    {
        public string Value { get; set; }

        public UnsafeLiteral(string value, bool replaceQuotes = true)
        {
            if (value == null)
            {
                value = "";
            }

            if (replaceQuotes)
            {
                value = value.Replace("'", "''");
            }

            this.Value = value;
        }

    }
}
