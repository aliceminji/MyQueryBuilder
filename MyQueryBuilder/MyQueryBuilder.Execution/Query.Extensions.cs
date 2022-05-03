using MyQueryBuilder.Compilers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace MyQueryBuilder.Execution
{
    public static class QueryExtensions
    {
        public static IEnumerable<T> Get<T>(this Query query, IDbTransaction transaction = null, int? timeout = null)
        {
            return CreateQueryFactory(query).Get<T>(query, transaction, timeout);
        }

        internal static QueryManager CreateQueryFactory(Query query)
        {
            return CreateQueryFactory(CastToXQuery(query));
        }

        public static IEnumerable<dynamic> Get(this Query query, IDbTransaction transaction = null, int? timeout = null)
        {
            return query.Get<dynamic>(transaction, timeout);
        }

        internal static XQuery CastToXQuery(Query query, string method = null)
        {
            var xQuery = query as XQuery;

            return xQuery;
        }
    }
}
