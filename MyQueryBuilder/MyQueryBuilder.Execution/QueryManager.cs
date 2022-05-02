using MyQueryBuilder.Compilers;
using System;
using System.Collections.Generic;
using System.Data;
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

            //query.Logger = Logger;

            return query;
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
