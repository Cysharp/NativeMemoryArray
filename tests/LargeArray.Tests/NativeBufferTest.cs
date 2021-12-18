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
            using var nbuffer = new NativeBuffer(124);



            nbuffer[10] = 124;
        }
    }
}
