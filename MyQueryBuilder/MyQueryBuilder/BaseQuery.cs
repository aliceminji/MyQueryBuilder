using System;
using System.Collections.Generic;
using System.Text;

namespace MyQueryBuilder
{
    public abstract class AbstractQuery
    {
        public AbstractQuery Parent;
    }
    
    public abstract partial class BaseQuery<Q> : AbstractQuery where Q : BaseQuery<Q>
    {
    }
}
