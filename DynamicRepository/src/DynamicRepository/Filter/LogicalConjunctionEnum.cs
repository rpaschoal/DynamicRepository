using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DynamicRepository.Filter
{
    /// <summary>
    /// Used to apply logical conjunction to dynamic linq expressions.
    /// </summary>
    public enum LogicalConjunctionEnum
    {
        AND = 0,
        OR = 1
    }
}
