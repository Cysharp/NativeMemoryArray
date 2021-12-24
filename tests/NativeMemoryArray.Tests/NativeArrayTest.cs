using System.Buffers;

namespace NativeMemoryArrayTests
{
    public class NativeArrayTest
    {
        [Fact]
        public void Int()
        {
            using var array = new NativeMemoryArray<int>(1024);

            array.TryGetFullSpan(out var span).Should().BeTrue();
            span.Length.Should().Be(1024);

            var refarray = Enumerable.Range(1, 1024).ToArray();
            refarray.CopyTo(span);

            array.AsSpan().SequenceEqual(refarray).Should().BeTrue();

            for (long i = 0; i < array.Length; i++)
            {
                array[i].Should().Be(refarray[i]);
            }
        }

        [Fact]
        public void ValueType()
        {
            using var array = new NativeMemoryArray<ValueTypeSample>(15);

            for (int i = 0; i < 15; i++)
            {
                array[(long)i] = new ValueTypeSample { X = i, Y = i * i, Z = i * i * i };
            }

            for (int i = 0; i < 15; i++)
            {
                var v = array[(long)i];
                v.X.Should().Be(i);
                v.Y.Should().Be(i * i);
                v.Z.Should().Be(i * i * i);
            }
        }

        [Fact]
        public void As()
        {
            using var array = new NativeMemoryArray<int>(10);
            var range = Enumerable.Range(0, 10).ToArray();
            range.AsSpan().CopyTo(array.AsSpan()); // 0..9

            range.AsSpan(3)[0].Should().Be(3);
            {
                var slice = array.AsSpan(3, 4);
                slice.Length.Should().Be(4);
                
                slice[0].Should().Be(3);
                slice[1].Should().Be(4);
                slice[2].Should().Be(5);
                slice[3].Should().Be(6);
            }
            {
                var slice = array.AsMemory(3, 4);
                slice.Length.Should().Be(4);

                slice.Span[0].Should().Be(3);
                slice.Span[1].Should().Be(4);
                slice.Span[2].Should().Be(5);
                slice.Span[3].Should().Be(6);
            }

            {
                var writer = array.CreateBufferWriter();
                var span = writer.GetSpan(4);
                span[0] = 1000;
                span[1] = 2000;
                span[2] = 3000;
                writer.Advance(3);
                span = writer.GetSpan(4);
                span[0] = 4000;
                span[1] = 5000;
                span[2] = 6000;
                writer.Advance(3);

                array.AsSpan(0, 6).ToArray().Should().Equal(1000, 2000, 3000, 4000, 5000, 6000);
            }

            {
                var writer = array.CreateBufferWriter();
                var span = writer.GetMemory(4).Span;
                span[0] = 1000;
                span[1] = 2000;
                span[2] = 3000;
                writer.Advance(3);
                span = writer.GetMemory(4).Span;
                span[0] = 4000;
                span[1] = 5000;
                span[2] = 6000;
                writer.Advance(3);

                array.AsSpan(0, 6).ToArray().Should().Equal(1000, 2000, 3000, 4000, 5000, 6000);
            }

        }
    }

    public class ReferenceTypeSample
    {
        public int MyProperty { get; set; }
    }

    public struct ValueTypeSample
    {
        public int X;
        public int Y;
        public int Z;
    }
}