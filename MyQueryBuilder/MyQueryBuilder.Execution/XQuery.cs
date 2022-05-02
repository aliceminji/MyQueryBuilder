using MyQueryBuilder.Compilers;
using System.Data;

namespace MyQueryBuilder.Execution
{
    public class XQuery : Query
    {
        public IDbConnection Connection { get; set; }
        public Compiler Compiler { get; set; }
        public QueryManager QueryManager { get; set; }


        public XQuery(IDbConnection connection, Compiler compiler)
        {
            this.Connection = connection;
            this.Compiler = compiler;
        }

    }
}
