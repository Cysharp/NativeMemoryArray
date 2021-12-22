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