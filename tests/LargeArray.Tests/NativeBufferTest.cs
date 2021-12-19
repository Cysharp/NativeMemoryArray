using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LargeArray.Tests
{
    public class NativeBufferTest
    {
        [Fact]
        public void Foo()
        {
            using var nbuffer = new NativeBuffer((nuint)int.MaxValue * 2);


            nbuffer.TryGetFullSpan(out var span).Should().BeFalse();


            var i = 0;
            foreach (var item in nbuffer)
            {
                if (i++ == 0)
                {
                    item.Length.Should().Be(int.MaxValue);
                }
                else
                {
                    Console.WriteLine("foo");
                }
            }

        }
    }
}
