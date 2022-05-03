using MyQueryBuilder.Clauses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyQueryBuilder
{
    public abstract class AbstractQuery
    {
        public AbstractQuery Parent;
    }
    
    public abstract partial class BaseQuery<Q> : AbstractQuery where Q : BaseQuery<Q>
    {
        public List<AbstractClause> Clauses { get; set; } = new List<AbstractClause>();
        private bool notFlag = false;
        private bool orFlag = false;

        public string EngineScope = null;

        public Q SetEngineScope(string engine)
        {
            this.EngineScope = engine;

            return (Q)this;
        }

        public virtual Q Clone()
        {
            var q = NewQuery();

            q.Clauses = this.Clauses.Select(x => x.Clone()).ToList();

            return q;
        }

        public Q From(Query query, string alias = null)
        {
            query = query.Clone();
            query.SetParent((Q)this);

            if (alias != null)
            {
                query.As(alias);
            };

            return AddOrReplaceComponent("from", new QueryFromClause
            {
                Query = query
            });
        }

        public AbstractClause GetOneComponent(string component, string engineCode = null)
        {
            if (engineCode == null)
            {
                engineCode = EngineScope;
            }

            return GetOneComponent<AbstractClause>(component, engineCode);
        }

        public Q SetParent(AbstractQuery parent)
        {
            if (this == parent)
            {
                throw new ArgumentException($"Cannot set the same {nameof(AbstractQuery)} as a parent of itself");
            }

            this.Parent = parent;
            return (Q)this;
        }

        public Q Not(bool flag = true)
        {
            notFlag = flag;
            return (Q)this;
        }

        public abstract Q NewQuery();


        public C GetOneComponent<C>(string component, string engineCode = null) where C : AbstractClause
        {
            engineCode = engineCode ?? EngineScope;

            var all = GetComponents<C>(component, engineCode);
            return all.FirstOrDefault(c => c.Engine == engineCode) ?? all.FirstOrDefault(c => c.Engine == null);
        }

        public List<C> GetComponents<C>(string component, string engineCode = null) where C : AbstractClause
        {
            if (engineCode == null)
            {
                engineCode = EngineScope;
            }

            var clauses = Clauses
                .Where(x => x.Component == component)
                .Where(x => engineCode == null || x.Engine == null || engineCode == x.Engine)
                .Cast<C>();

            return clauses.ToList();
        }

        /// <summary>
        /// If the query already contains a clause for the given component
        /// and engine, replace it with the specified clause. Otherwise, just
        /// add the clause.
        /// </summary>
        /// <param name="component"></param>
        /// <param name="clause"></param>
        /// <param name="engineCode"></param>
        /// <returns></returns>
        public Q AddOrReplaceComponent(string component, AbstractClause clause, string engineCode = null)
        {
            engineCode = engineCode ?? EngineScope;

            var current = GetComponents(component).SingleOrDefault(c => c.Engine == engineCode);
            if (current != null)
                Clauses.Remove(current);

            return AddComponent(component, clause, engineCode);
        }

        public Q AddComponent(string component, AbstractClause clause, string engineCode = null)
        {
            if (engineCode == null)
            {
                engineCode = EngineScope;
            }

            clause.Engine = engineCode;
            clause.Component = component;
            Clauses.Add(clause);

            return (Q)this;
        }

        public List<AbstractClause> GetComponents(string component, string engineCode = null)
        {
            if (engineCode == null)
            {
                engineCode = EngineScope;
            }

            return GetComponents<AbstractClause>(component, engineCode);
        }

        /// <summary>
        /// Add a from Clause
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        public Q From(string table)
        {
            return AddOrReplaceComponent("from", new FromClause
            {
                Table = table,
            });
        }

        public bool HasComponent(string component, string engineCode = null)
        {
            if (engineCode == null)
            {
                engineCode = EngineScope;
            }

            return GetComponents(component, engineCode).Any();
        }

        public Q ClearComponent(string component, string engineCode = null)
        {
            if (engineCode == null)
            {
                engineCode = EngineScope;
            }

            Clauses = Clauses
                .Where(x => !(x.Component == component && (engineCode == null || x.Engine == null || engineCode == x.Engine)))
                .ToList();

            return (Q)this;
        }

        protected bool GetOr()
        {
            var ret = orFlag;

            // reset the flag
            orFlag = false;
            return ret;
        }

        protected bool GetNot()
        {
            var ret = notFlag;

            // reset the flag
            notFlag = false;
            return ret;
        }
    }



}
