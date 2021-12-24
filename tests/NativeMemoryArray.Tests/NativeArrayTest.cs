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

        [Fact]
        public void Stream()
        {
            using var array = new NativeMemoryArray<byte>(1024);
            Enumerable.Range(1, 1024).Select(x => (byte)x).ToArray().CopyTo(array.AsSpan());

            var readStream = array.AsStream();
            var writeStream = array.AsStream(FileAccess.Write);

            var buffer = new byte[1024];
            readStream.Read(buffer, 0, 10);

            var span = array.AsSpan();
            span[0].Should().Be(1);
            span[1].Should().Be(2);
            span[2].Should().Be(3);
            span[3].Should().Be(4);
            span[4].Should().Be(5);
            span[5].Should().Be(6);

            writeStream.WriteByte(10);
            writeStream.WriteByte(20);
            writeStream.WriteByte(30);
            writeStream.WriteByte(40);
            writeStream.Flush();
            span[0].Should().Be(10);
            span[1].Should().Be(20);
            span[2].Should().Be(30);
            span[3].Should().Be(40);
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