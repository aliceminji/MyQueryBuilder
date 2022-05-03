using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Humanizer;
using MyQueryBuilder.Clauses;
using MyQueryBuilder.Compilers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace MyQueryBuilder.Execution
{
    public class QueryManager : IDisposable
    {
        public IDbConnection Connection { get; set; }
        public Compiler Compiler { get; set; }
        //public Action<SqlResult> Logger = result => { };
        private bool disposedValue;
        public int QueryTimeout { get; set; } = 30;

        public QueryManager() { }

        public QueryManager(IDbConnection connection, Compiler compiler, int timeout = 30)
        {
            Connection = connection;
            Compiler = compiler;
            QueryTimeout = timeout;
        }

        public Query Query()
        {
            var query = new XQuery(this.Connection, this.Compiler);

            query.QueryManager = this;

            return query;
        }

        public Query Query(string table)
        {
            return Query().From(table);
        }
        private static IEnumerable<T> handleIncludes<T>(Query query, IEnumerable<T> result)
        {
            if (!result.Any())
            {
                return result;
            }

            var canBeProcessed = query.Includes.Any() && result.ElementAt(0) is IDynamicMetaObjectProvider;

            if (!canBeProcessed)
            {
                return result;
            }

            var dynamicResult = result
                .Cast<IDictionary<string, object>>()
                .Select(x => new Dictionary<string, object>(x, StringComparer.OrdinalIgnoreCase))
                .ToList();

            foreach (var include in query.Includes)
            {

                if (include.IsMany)
                {
                    if (include.ForeignKey == null)
                    {
                        // try to guess the default key
                        // I will try to fetch the table name if provided and appending the Id as a convention
                        // Here am using Humanizer package to help getting the singular form of the table

                        var fromTable = query.GetOneComponent("from") as FromClause;

                        if (fromTable == null)
                        {
                            throw new InvalidOperationException($"Cannot guess the foreign key for the included relation '{include.Name}'");
                        }

                        var table = fromTable.Alias ?? fromTable.Table;

                        include.ForeignKey = table.Singularize(false) + "Id";
                    }

                    var localIds = dynamicResult.Where(x => x[include.LocalKey] != null)
                    .Select(x => x[include.LocalKey].ToString())
                    .ToList();

                    if (!localIds.Any())
                    {
                        continue;
                    }

                    var children = include
                        .Query
                        .WhereIn(include.ForeignKey, localIds)
                        .Get()
                        .Cast<IDictionary<string, object>>()
                        .Select(x => new Dictionary<string, object>(x, StringComparer.OrdinalIgnoreCase))
                        .GroupBy(x => x[include.ForeignKey].ToString())
                        .ToDictionary(x => x.Key, x => x.ToList());

                    foreach (var item in dynamicResult)
                    {
                        var localValue = item[include.LocalKey].ToString();
                        item[include.Name] = children.ContainsKey(localValue) ? children[localValue] : new List<Dictionary<string, object>>();
                    }

                    continue;
                }

                if (include.ForeignKey == null)
                {
                    include.ForeignKey = include.Name + "Id";
                }

                var foreignIds = dynamicResult
                    .Where(x => x[include.ForeignKey] != null)
                    .Select(x => x[include.ForeignKey].ToString())
                    .ToList();

                if (!foreignIds.Any())
                {
                    continue;
                }

                var related = include
                    .Query
                    .WhereIn(include.LocalKey, foreignIds)
                    .Get()
                    .Cast<IDictionary<string, object>>()
                    .Select(x => new Dictionary<string, object>(x, StringComparer.OrdinalIgnoreCase))
                    .ToDictionary(x => x[include.LocalKey].ToString());

                foreach (var item in dynamicResult)
                {
                    var foreignValue = item[include.ForeignKey]?.ToString();
                    item[include.Name] = foreignValue != null && related.ContainsKey(foreignValue) ? related[foreignValue] : null;
                }
            }

            return dynamicResult.Cast<T>();
        }

        public IEnumerable<T> Get<T>(Query query, IDbTransaction transaction = null, int? timeout = null)
        {
            var compiled = CompileAndLog(query);

            var result = this.Connection.Query<T>(
                compiled.Sql,
                compiled.NamedBindings,
                transaction: transaction,
                commandTimeout: timeout ?? this.QueryTimeout
            ).ToList();

            result = handleIncludes<T>(query, result).ToList();

            return result;
        }

        internal ResultData CompileAndLog(Query query)
        {
            var compiled = this.Compiler.Compile(query);

            return compiled;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 관리형 상태(관리형 개체)를 삭제합니다.
                }

                // TODO: 비관리형 리소스(비관리형 개체)를 해제하고 종료자를 재정의합니다.
                // TODO: 큰 필드를 null로 설정합니다.
                disposedValue = true;
            }
        }

        // // TODO: 비관리형 리소스를 해제하는 코드가 'Dispose(bool disposing)'에 포함된 경우에만 종료자를 재정의합니다.
        // ~QueryManager()
        // {
        //     // 이 코드를 변경하지 마세요. 'Dispose(bool disposing)' 메서드에 정리 코드를 입력합니다.
        //     Dispose(disposing: false);
        // }

        void IDisposable.Dispose()
        {
            // 이 코드를 변경하지 마세요. 'Dispose(bool disposing)' 메서드에 정리 코드를 입력합니다.
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
