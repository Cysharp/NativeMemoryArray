using System.Buffers;

namespace LargeArray.Tests
{
    public class LargeArrayTest
    {
        [Fact]
        public void Test1()
        {
            var array = new LargeArray<int>(1024, 100);
            array.Length.Should().Be(1024);
            array.AsReadOnlySequence().Length.Should().Be(1024);

            // size hint standard
            var span = array.GetSpan(5);
            span[0] = 99;
            span[1] = 98;
            span[2] = 97;
            span[3] = 96;
            span[4] = 95;
            array.Advance(5);
            array.Length.Should().Be(5);
            array.AsReadOnlySequence().ToArray().Should().Equal(99, 98, 97, 96, 95);

            // size hint over chunk
            array.GetSpan(150);


        }


        [Fact]
        public void EmptyArray()
        {
        }
    }
}