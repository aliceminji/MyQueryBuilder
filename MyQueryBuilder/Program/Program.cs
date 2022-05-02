using MyQueryBuilder.Execution;
using MySql.Data.MySqlClient;
using MyQueryBuilder.Compilers;

namespace Program
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var db = MySqlQueryManager())
            {
                
            }
        }

        private static QueryManager MySqlQueryManager()
        {
            var connection = new MySqlConnection("Host=localhost;Port=3308;User=root;Password=1234;Database=shop_db;SslMode=None");
            var db = new QueryManager(connection, new MySqlCompiler());

            return db;
        }
    }
}
