using System;

namespace KuaFu.Iwencai
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
