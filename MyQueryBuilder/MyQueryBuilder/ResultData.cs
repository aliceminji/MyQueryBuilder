using MyQueryBuilder.Compilers;
using System;
using System.Collections.Generic;
using System.Text;

namespace MyQueryBuilder
{
    public class ResultData
    {
        public Query Query { get; set; }
        public string RawSql { get; set; } = "";
        public string Sql { get; set; } = "";

        public List<object> Bindings { get; set; } = new List<object>();
        public Dictionary<string, object> NamedBindings = new Dictionary<string, object>();

    }
}
