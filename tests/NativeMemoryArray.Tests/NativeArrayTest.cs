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

            for (nuint i = 0; i < array.Length; i++)
            {
                array[i].Should().Be(refarray[i]);
            }
        }

#if NET48
        [Fact(Skip = "Reference type is not supported on netstandard2.0.")]
#else
        [Fact]
#endif   
        public void ReferenceType()
        {
            using var array = new NativeMemoryArray<ReferenceTypeSample>(15);

            foreach (var item in array.AsSpan())
            {
                item.Should().BeNull();
            }

            for (int i = 0; i < 15; i++)
            {
                array[(nuint)i] = new ReferenceTypeSample { MyProperty = i };
            }


            for (int i = 0; i < 15; i++)
            {
                array[(nuint)i].MyProperty.Should().Be(i);
            }
        }

        [Fact]
        public void ValueType()
        {
            using var array = new NativeMemoryArray<ValueTypeSample>(15);

            for (int i = 0; i < 15; i++)
            {
                array[(nuint)i] = new ValueTypeSample { X = i, Y = i * i, Z = i * i * i };
            }

            for (int i = 0; i < 15; i++)
            {
                var v = array[(nuint)i];
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