using MyQueryBuilder.Clauses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyQueryBuilder.Compilers
{
    public partial class Compiler
    {
        protected virtual string parameterPlaceholder { get; set; } = "?";
        protected virtual string parameterPrefix { get; set; } = "@p";
        protected virtual string OpeningIdentifier { get; set; } = "\"";
        protected virtual string ClosingIdentifier { get; set; } = "\"";
        protected virtual string ColumnAsKeyword { get; set; } = "AS ";
        protected virtual string TableAsKeyword { get; set; } = "AS ";
        protected virtual string LastId { get; set; } = "";
        protected virtual string EscapeCharacter { get; set; } = "\\";

        public virtual string EngineCode { get; }

        public virtual string CompileLimit(ResultData ctx)
        {
            var limit = ctx.Query.GetLimit(EngineCode);
            var offset = ctx.Query.GetOffset(EngineCode);

            if (limit == 0 && offset == 0)
            {
                return null;
            }

            if (offset == 0)
            {
                ctx.Bindings.Add(limit);
                return "LIMIT ?";
            }

            if (limit == 0)
            {
                ctx.Bindings.Add(offset);
                return "OFFSET ?";
            }

            ctx.Bindings.Add(limit);
            ctx.Bindings.Add(offset);

            return "LIMIT ? OFFSET ?";
        }

        protected virtual ResultData CompileRaw(Query query)
        {
            ResultData ctx;

            if (query.Method == "insert")
            {
                ctx = CompileInsertQuery(query);
            }
            //else if (query.Method == "update")
            //{
            //    ctx = CompileUpdateQuery(query);
            //}
            //else if (query.Method == "delete")
            //{
            //    ctx = CompileDeleteQuery(query);
            //}
            else
            {
                if (query.Method == "aggregate")
                {
                    query.ClearComponent("limit")
                        .ClearComponent("order")
                        .ClearComponent("group");

                    query = TransformAggregateQuery(query);
                }

                ctx = CompileSelectQuery(query);
            }

            // handle CTEs
            if (query.HasComponent("cte", EngineCode))
            {
                ctx = CompileCteQuery(ctx, query);
            }

            ctx.RawSql = Helper.ExpandParameters(ctx.RawSql, "?", ctx.Bindings.ToArray());

            return ctx;
        }

        protected virtual ResultData CompileCteQuery(ResultData ctx, Query query)
        {
            var cteFinder = new CteFinder(query, EngineCode);
            var cteSearchResult = cteFinder.Find();

            var rawSql = new StringBuilder("WITH ");
            var cteBindings = new List<object>();

            foreach (var cte in cteSearchResult)
            {
                var cteCtx = CompileCte(cte);

                cteBindings.AddRange(cteCtx.Bindings);
                rawSql.Append(cteCtx.RawSql.Trim());
                rawSql.Append(",\n");
            }

            rawSql.Length -= 2; // remove last comma
            rawSql.Append('\n');
            rawSql.Append(ctx.RawSql);

            ctx.Bindings.InsertRange(0, cteBindings);
            ctx.RawSql = rawSql.ToString();

            return ctx;
        }

        public virtual ResultData CompileCte(AbstractFrom cte)
        {
            var ctx = new ResultData();

            if (null == cte)
            {
                return ctx;
            }

            if (cte is RawFromClause raw)
            {
                ctx.Bindings.AddRange(raw.Bindings);
                ctx.RawSql = $"{WrapValue(raw.Alias)} AS ({WrapIdentifiers(raw.Expression)})";
            }
            else if (cte is QueryFromClause queryFromClause)
            {
                var subCtx = CompileSelectQuery(queryFromClause.Query);
                ctx.Bindings.AddRange(subCtx.Bindings);

                ctx.RawSql = $"{WrapValue(queryFromClause.Alias)} AS ({subCtx.RawSql})";
            }
            else if (cte is AdHocTableFromClause adHoc)
            {
                var subCtx = CompileAdHocQuery(adHoc);
                ctx.Bindings.AddRange(subCtx.Bindings);

                ctx.RawSql = $"{WrapValue(adHoc.Alias)} AS ({subCtx.RawSql})";
            }

            return ctx;
        }
        protected virtual string SingleRowDummyTableName { get => null; }

        protected virtual ResultData CompileAdHocQuery(AdHocTableFromClause adHoc)
        {
            var ctx = new ResultData();

            var row = "SELECT " + string.Join(", ", adHoc.Columns.Select(col => $"? AS {Wrap(col)}"));

            var fromTable = SingleRowDummyTableName;

            if (fromTable != null)
            {
                row += $" FROM {fromTable}";
            }

            var rows = string.Join(" UNION ALL ", Enumerable.Repeat(row, adHoc.Values.Count / adHoc.Columns.Count));

            ctx.RawSql = rows;
            ctx.Bindings = adHoc.Values;

            return ctx;
        }

        private Query TransformAggregateQuery(Query query)
        {
            var clause = query.GetOneComponent<AggregateClause>("aggregate", EngineCode);

            if (clause.Columns.Count == 1 && !query.IsDistinct) return query;

            if (query.IsDistinct)
            {
                query.ClearComponent("aggregate", EngineCode);
                query.ClearComponent("select", EngineCode);
                query.Select(clause.Columns.ToArray());
            }
            else
            {
                foreach (var column in clause.Columns)
                {
                    query.WhereNotNull(column);
                }
            }

            var outerClause = new AggregateClause()
            {
                Columns = new List<string> { "*" },
                Type = clause.Type
            };

            return new Query()
                .AddComponent("aggregate", outerClause)
                .From(query, $"{clause.Type}Query");
        }

        public virtual ResultData Compile(Query query)
        {
            var ctx = CompileRaw(query);

            ctx = PrepareResult(ctx);

            return ctx;
        }

        protected ResultData PrepareResult(ResultData ctx)
        {
            ctx.NamedBindings = generateNamedBindings(ctx.Bindings.ToArray());
            ctx.Sql = Helper.ReplaceAll(ctx.RawSql, parameterPlaceholder, i => parameterPrefix + i);
            return ctx;
        }

        protected Dictionary<string, object> generateNamedBindings(object[] bindings)
        {
            return Helper.Flatten(bindings).Select((v, i) => new { i, v })
                .ToDictionary(x => parameterPrefix + x.i, x => x.v);
        }

        protected virtual ResultData CompileInsertQuery(Query query)
        {
            var ctx = new ResultData
            {
                Query = query
            };

            if (!ctx.Query.HasComponent("from", EngineCode))
            {
                throw new InvalidOperationException("No table set to insert");
            }

            var fromClause = ctx.Query.GetOneComponent<AbstractFrom>("from", EngineCode);

            if (fromClause is null)
            {
                throw new InvalidOperationException("Invalid table expression");
            }

            string table = null;

            if (fromClause is FromClause fromClauseCast)
            {
                table = Wrap(fromClauseCast.Table);
            }

            if (fromClause is RawFromClause rawFromClause)
            {
                table = WrapIdentifiers(rawFromClause.Expression);
                ctx.Bindings.AddRange(rawFromClause.Bindings);
            }

            if (table is null)
            {
                throw new InvalidOperationException("Invalid table expression");
            }

            var inserts = ctx.Query.GetComponents<AbstractInsertClause>("insert", EngineCode);

            if (inserts[0] is InsertClause insertClause)
            {
                var columns = string.Join(", ", WrapArray(insertClause.Columns));
                var values = string.Join(", ", Parameterize(ctx, insertClause.Values));

                ctx.RawSql = $"INSERT INTO {table} ({columns}) VALUES ({values})";

                if (insertClause.ReturnId && !string.IsNullOrEmpty(LastId))
                {
                    ctx.RawSql += ";" + LastId;
                }
            }
            else
            {
                var clause = inserts[0] as InsertQueryClause;

                var columns = "";

                if (clause.Columns.Any())
                {
                    columns = $" ({string.Join(", ", WrapArray(clause.Columns))}) ";
                }

                var subCtx = CompileSelectQuery(clause.Query);
                ctx.Bindings.AddRange(subCtx.Bindings);

                ctx.RawSql = $"INSERT INTO {table}{columns}{subCtx.RawSql}";
            }

            if (inserts.Count > 1)
            {
                foreach (var insert in inserts.GetRange(1, inserts.Count - 1))
                {
                    var clause = insert as InsertClause;

                    ctx.RawSql += ", (" + string.Join(", ", Parameterize(ctx, clause.Values)) + ")";

                }
            }


            return ctx;
        }

        protected virtual ResultData CompileSelectQuery(Query query)
        {
            var ctx = new ResultData
            {
                Query = query.Clone(),
            };

            var results = new[] {
                    this.CompileColumns(ctx),
                    this.CompileFrom(ctx),
                }
               .Where(x => x != null)
               .Where(x => !string.IsNullOrEmpty(x))
               .ToList();

            string sql = string.Join(" ", results);

            ctx.RawSql = sql;

            return ctx;
        }

        public virtual string CompileTableExpression(ResultData ctx, AbstractFrom from)
        {
            if (from is RawFromClause raw)
            {
                ctx.Bindings.AddRange(raw.Bindings);
                return WrapIdentifiers(raw.Expression);
            }

            if (from is QueryFromClause queryFromClause)
            {
                var fromQuery = queryFromClause.Query;

                var alias = string.IsNullOrEmpty(fromQuery.QueryAlias) ? "" : $" {TableAsKeyword}" + WrapValue(fromQuery.QueryAlias);

                var subCtx = CompileSelectQuery(fromQuery);

                ctx.Bindings.AddRange(subCtx.Bindings);

                return "(" + subCtx.RawSql + ")" + alias;
            }

            if (from is FromClause fromClause)
            {
                return Wrap(fromClause.Table);
            }

            throw InvalidClauseException("TableExpression", from);
        }

        private InvalidCastException InvalidClauseException(string section, AbstractClause clause)
        {
            return new InvalidCastException($"Invalid type \"{clause.GetType().Name}\" provided for the \"{section}\" clause.");
        }

        public virtual string CompileFrom(ResultData ctx)
        {
            if (ctx.Query.HasComponent("from", EngineCode))
            {
                var from = ctx.Query.GetOneComponent<AbstractFrom>("from", EngineCode);

                return "FROM " + CompileTableExpression(ctx, from);
            }

            return string.Empty;
        }

        protected virtual string CompileColumns(ResultData ctx)
        {
            if (ctx.Query.HasComponent("aggregate", EngineCode))
            {
                var aggregate = ctx.Query.GetOneComponent<AggregateClause>("aggregate", EngineCode);

                var aggregateColumns = aggregate.Columns
                    .Select(x => CompileColumn(ctx, new Column { Name = x }))
                    .ToList();

                string sql = string.Empty;

                if (aggregateColumns.Count == 1)
                {
                    sql = string.Join(", ", aggregateColumns);

                    if (ctx.Query.IsDistinct)
                    {
                        sql = "DISTINCT " + sql;
                    }

                    return "SELECT " + aggregate.Type.ToUpperInvariant() + "(" + sql + $") {ColumnAsKeyword}" + WrapValue(aggregate.Type);
                }

                return "SELECT 1";
            }

            var columns = ctx.Query
                .GetComponents<AbstractColumn>("select", EngineCode)
                .Select(x => CompileColumn(ctx, x))
                .ToList();

            var distinct = ctx.Query.IsDistinct ? "DISTINCT " : "";

            var select = columns.Any() ? string.Join(", ", columns) : "*";

            return $"SELECT {distinct}{select}";

        }

        public virtual string CompileColumn(ResultData ctx, AbstractColumn column)
        {
            if (column is RawColumn raw)
            {
                ctx.Bindings.AddRange(raw.Bindings);
                return WrapIdentifiers(raw.Expression);
            }

            if (column is QueryColumn queryColumn)
            {
                var alias = "";

                if (!string.IsNullOrWhiteSpace(queryColumn.Query.QueryAlias))
                {
                    alias = $" {ColumnAsKeyword}{WrapValue(queryColumn.Query.QueryAlias)}";
                }

                var subCtx = CompileSelectQuery(queryColumn.Query);

                ctx.Bindings.AddRange(subCtx.Bindings);

                return "(" + subCtx.RawSql + $"){alias}";
            }

            return Wrap((column as Column).Name);

        }


        public virtual string Parameterize<T>(ResultData ctx, IEnumerable<T> values)
        {
            return string.Join(", ", values.Select(x => Parameter(ctx, x)));
        }

        public virtual string Parameter(ResultData ctx, object parameter)
        {
            // if we face a literal value we have to return it directly
            if (parameter is UnsafeLiteral literal)
            {
                return literal.Value;
            }

            // if we face a variable we have to lookup the variable from the predefined variables
            if (parameter is Variable variable)
            {
                var value = ctx.Query.FindVariable(variable.Name);
                ctx.Bindings.Add(value);
                return "?";
            }

            ctx.Bindings.Add(parameter);
            return "?";
        }

        public virtual List<string> WrapArray(List<string> values)
        {
            return values.Select(x => Wrap(x)).ToList();
        }

        public virtual string WrapValue(string value)
        {
            if (value == "*") return value;

            var opening = this.OpeningIdentifier;
            var closing = this.ClosingIdentifier;

            return opening + value.Replace(closing, closing + closing) + closing;
        }

        public virtual string Wrap(string value)
        {

            if (value.ToLowerInvariant().Contains(" as "))
            {
                var index = value.ToLowerInvariant().IndexOf(" as ");
                var before = value.Substring(0, index);
                var after = value.Substring(index + 4);

                return Wrap(before) + $" {ColumnAsKeyword}" + WrapValue(after);
            }

            if (value.Contains("."))
            {
                return string.Join(".", value.Split('.').Select((x, index) =>
                {
                    return WrapValue(x);
                }));
            }

            // If we reach here then the value does not contain an "AS" alias
            // nor dot "." expression, so wrap it as regular value.
            return WrapValue(value);
        }

        public virtual string WrapIdentifiers(string input)
        {
            return input

                // deprecated
                .ReplaceIdentifierUnlessEscaped(this.EscapeCharacter, "{", this.OpeningIdentifier)
                .ReplaceIdentifierUnlessEscaped(this.EscapeCharacter, "}", this.ClosingIdentifier)

                .ReplaceIdentifierUnlessEscaped(this.EscapeCharacter, "[", this.OpeningIdentifier)
                .ReplaceIdentifierUnlessEscaped(this.EscapeCharacter, "]", this.ClosingIdentifier);
        }
    }
}
