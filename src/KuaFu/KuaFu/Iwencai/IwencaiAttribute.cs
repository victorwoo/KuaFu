using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KuaFu
{
    class IwencaiAttribute : Attribute
    {
        public IwencaiAttribute(int order)
        {
            this.Order = order;
        }

        public int Order { get; set; }
    }
}
