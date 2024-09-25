using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NugetTestLocal
{
    public class MetricFactoryAttribute : Attribute
    {
        public MetricFactoryAttribute(string targetNamespace, string targetAccount)
        {
            this.Namespace = targetNamespace;
            this.AccountName = targetAccount;
        }

        public string Namespace { get; }

        public string AccountName { get; }
    }
}
