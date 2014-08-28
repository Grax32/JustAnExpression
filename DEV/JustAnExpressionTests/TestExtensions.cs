using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustAnExpressionTests
{
    public static class TestExtensions
    {
        public static IEnumerable<T> Repeat<T>(this T item, int count)
        {
            for (int i = 0; i < count; i++)
            {
                yield return item;
            }
        }
    }
}
