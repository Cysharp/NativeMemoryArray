using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Xunit.Sdk;

namespace XtdArray.Tests
{
    public class NativeBufferTest
    {
        [Fact]
        public void Lower2GB()
        {
            using var nbuffer = new NativeBuffer(1024);
            nbuffer.Length.Should().Be(1024);

            var rand = Enumerable.Range(0, (int)nbuffer.Length).Select(x => (byte)Random.Shared.Next(0, byte.MaxValue)).ToArray();

            // copy all
            for (uint i = 0; i < nbuffer.Length; i++)
            {
                nbuffer[i] = rand[i];
            }

            // check indexer
            for (uint i = 0; i < nbuffer.Length; i++)
            {
                nbuffer[i].Should().Be(rand[i]);
            }

            // indexer:out of range
            nbuffer[1023].Should().Be(rand.Last());
            Assert.Throws<IndexOutOfRangeException>(() => nbuffer[1024]);
            Assert.Throws<IndexOutOfRangeException>(() => nbuffer[9999]);

            // Single span
            nbuffer.TryGetFullSpan(out var fullspan).Should().BeTrue();
            fullspan.SequenceEqual(rand).Should().BeTrue();

            // Slice
            nbuffer.Slice(114, 99).SequenceEqual(rand.AsSpan().Slice(114, 99)).Should().BeTrue();
            nbuffer.Slice(35).SequenceEqual(rand.AsSpan().Slice(35)).Should().BeTrue();
            nbuffer.SliceMemory(114, 99).Span.SequenceEqual(rand.AsSpan().Slice(114, 99)).Should().BeTrue();
            nbuffer.SliceMemory(35).Span.SequenceEqual(rand.AsSpan().Slice(35)).Should().BeTrue();

            // Slice:out of range
            rand.AsSpan().Slice(14, 1010).SequenceEqual(rand.AsSpan().Slice(14, 1010)).Should().BeTrue();
            nbuffer.Slice(14, 1010).SequenceEqual(rand.AsSpan().Slice(14, 1010)).Should().BeTrue();
            nbuffer.SliceMemory(14, 1010).Span.SequenceEqual(rand.AsSpan().Slice(14, 1010)).Should().BeTrue();
            Assert.Throws<ArgumentOutOfRangeException>(() => rand.AsSpan().Slice(14, 1011));
            Assert.Throws<ArgumentOutOfRangeException>(() => nbuffer.Slice(14, 1011));
            Assert.Throws<ArgumentOutOfRangeException>(() => nbuffer.SliceMemory(14, 1011));

            // pointer
            unsafe
            {
                fixed (byte* p = nbuffer)
                {
                    (*p).Should().Be(rand[0]);
                    (*(p + 143)).Should().Be(rand[143]);
                }

                fixed (byte* p = &nbuffer[43])
                {
                    (*p).Should().Be(rand[43]);
                    (*(p + 143)).Should().Be(rand[43 + 143]);
                }
            }

            // AsSpanSeqeunce
            var ii = 0;
            foreach (var item in nbuffer.AsSpanSequence(333))
            {
                if (ii == 0) // 333
                {
                    item.Length.Should().Be(333);
                    item.SequenceEqual(rand.AsSpan(0, 333)).Should().BeTrue();
                }
                else if (ii == 1) // 666
                {
                    item.Length.Should().Be(333);
                    item.SequenceEqual(rand.AsSpan(333, 333)).Should().BeTrue();
                }
                else if (ii == 2) // 999
                {
                    item.Length.Should().Be(333);
                    item.SequenceEqual(rand.AsSpan(666, 333)).Should().BeTrue();
                }
                else if (ii == 3) // 1332
                {
                    item.Length.Should().Be(25);
                    item.SequenceEqual(rand.AsSpan(999)).Should().BeTrue();
                }
                else
                {
                    throw new XunitException();
                }
                ii++;
            }
            ii.Should().Be(4);

            foreach (var item in nbuffer)
            {
                item.SequenceEqual(rand).Should().BeTrue();
            }

            // AsMemorySequeunce
            ii = 0;
            foreach (var item in nbuffer.AsMemorySequence(333))
            {
                if (ii == 0) // 333
                {
                    item.Length.Should().Be(333);
                    item.Span.SequenceEqual(rand.AsSpan(0, 333)).Should().BeTrue();
                }
                else if (ii == 1) // 666
                {
                    item.Length.Should().Be(333);
                    item.Span.SequenceEqual(rand.AsSpan(333, 333)).Should().BeTrue();
                }
                else if (ii == 2) // 999
                {
                    item.Length.Should().Be(333);
                    item.Span.SequenceEqual(rand.AsSpan(666, 333)).Should().BeTrue();
                }
                else if (ii == 3) // 1332
                {
                    item.Length.Should().Be(25);
                    item.Span.SequenceEqual(rand.AsSpan(999)).Should().BeTrue();
                }
                else
                {
                    throw new XunitException();
                }
                ii++;
            }
            ii.Should().Be(4);

            // AsReadOnlyList
            ii = 0;
            foreach (var item in nbuffer.AsReadOnlyList(333))
            {
                if (ii == 0) // 333
                {
                    item.Length.Should().Be(333);
                    item.Span.SequenceEqual(rand.AsSpan(0, 333)).Should().BeTrue();
                }
                else if (ii == 1) // 666
                {
                    item.Length.Should().Be(333);
                    item.Span.SequenceEqual(rand.AsSpan(333, 333)).Should().BeTrue();
                }
                else if (ii == 2) // 999
                {
                    item.Length.Should().Be(333);
                    item.Span.SequenceEqual(rand.AsSpan(666, 333)).Should().BeTrue();
                }
                else if (ii == 3) // 1332
                {
                    item.Length.Should().Be(25);
                    item.Span.SequenceEqual(rand.AsSpan(999)).Should().BeTrue();
                }
                else
                {
                    throw new XunitException();
                }
                ii++;
            }
            ii.Should().Be(4);

            // AsReadOnlySeqeunce
            ii = 0;
            foreach (var item in nbuffer.AsReadOnlySequence(333))
            {
                if (ii == 0) // 333
                {
                    item.Length.Should().Be(333);
                    item.Span.SequenceEqual(rand.AsSpan(0, 333)).Should().BeTrue();
                }
                else if (ii == 1) // 666
                {
                    item.Length.Should().Be(333);
                    item.Span.SequenceEqual(rand.AsSpan(333, 333)).Should().BeTrue();
                }
                else if (ii == 2) // 999
                {
                    item.Length.Should().Be(333);
                    item.Span.SequenceEqual(rand.AsSpan(666, 333)).Should().BeTrue();
                }
                else if (ii == 3) // 1332
                {
                    item.Length.Should().Be(25);
                    item.Span.SequenceEqual(rand.AsSpan(999)).Should().BeTrue();
                }
                else
                {
                    throw new XunitException();
                }
                ii++;
            }
            ii.Should().Be(4);

            nbuffer.AsReadOnlySequence().Length.Should().Be(1024L);
            nbuffer.AsReadOnlySequence().ToArray().Should().Equal(rand);
            nbuffer.AsReadOnlySequence().Slice(555, 200).Should().Equals(rand.AsSpan(555, 200).ToArray());

            // CreateBufferWriter
            {
                var bufferWriter = nbuffer.CreateBufferWriter();

                var span = bufferWriter.GetSpan();
                span[0] = 100;
                span[1] = 200;
                bufferWriter.Advance(2);

                nbuffer[0].Should().Be(100);
                nbuffer[1].Should().Be(200);

                var memory = bufferWriter.GetMemory();
                memory.Span[0] = 201;
                memory.Span[1] = 202;
                bufferWriter.Advance(2);

                memory = bufferWriter.GetMemory();
                memory.Span[0] = 203;
                memory.Span[1] = 204;
                bufferWriter.Advance(2);

                span = bufferWriter.GetSpan();
                span[0] = 101;
                span[1] = 102;
                bufferWriter.Advance(2);

                nbuffer.Slice(0, 8).ToArray().Should().Equal(100, 200, 201, 202, 203, 204, 101, 102);

                // too large sizehint.
                Assert.Throws<InvalidOperationException>(() => bufferWriter.GetSpan(1017));
                Assert.Throws<InvalidOperationException>(() => bufferWriter.GetMemory(1017));

                bufferWriter.GetSpan(1016)[^1] = 255;
                bufferWriter.Advance(1016);

                nbuffer.TryGetFullSpan(out var ss).Should().BeTrue();
                ss[^1].Should().Be(255);
            }
        }

        [Fact]
        public unsafe void Over2GB()
        {
            var len = (nuint)int.MaxValue + 1024;
            using var nbuffer = new NativeBuffer(len);
            nbuffer.Length.Should().Be(len);


            nbuffer[len - 1] = 100;
            nbuffer[len - 1].Should().Be(100);

            // indexer:out of range
            Assert.Throws<IndexOutOfRangeException>(() => nbuffer[len]);

            // Single span
            nbuffer.TryGetFullSpan(out var fullspan).Should().BeFalse();


            var ii = 0;
            foreach (var item in nbuffer)
            {
                if (ii == 0)
                {
                    item.Length.Should().Be(int.MaxValue);
                }
                else if (ii == 1)
                {
                    item.Length.Should().Be(1024);
                }
                ii++;
            }
            ii.Should().Be(2);

            var bufferWriter = nbuffer.CreateBufferWriter();
            bufferWriter.GetSpan().Length.Should().Be(int.MaxValue);
            bufferWriter.Advance(int.MaxValue);
            bufferWriter.GetSpan().Length.Should().Be(1024);
            bufferWriter.Advance(1024);
            bufferWriter.GetSpan().Length.Should().Be(0);
        }

        [Fact]
        public unsafe void Empty()
        {
            var reference = Array.Empty<byte>();
            var nbuffer = NativeBuffer.Empty;

            Assert.Throws<IndexOutOfRangeException>(() => reference[0]);
            Assert.Throws<IndexOutOfRangeException>(() => nbuffer[0]);

            Assert.Throws<ArgumentOutOfRangeException>(() => reference.AsSpan().Slice(0, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => nbuffer.Slice(0, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => nbuffer.SliceMemory(0, 1));

            fixed (byte* p = reference)
            {
                if (p != null)
                {
                    throw new XunitException();
                }
            }

            fixed (byte* p = nbuffer)
            {
                if (p != null)
                {
                    throw new XunitException();
                }
            }

            nbuffer.TryGetFullSpan(out var span).Should().BeTrue();
            span.Length.Should().Be(0);

            var writer = nbuffer.CreateBufferWriter();
            writer.GetSpan().Length.Should().Be(0);
            writer.GetMemory().Length.Should().Be(0);

            nbuffer.AsSpanSequence().MoveNext().Should().BeFalse();
            nbuffer.AsMemorySequence().MoveNext().Should().BeFalse();
            nbuffer.AsReadOnlyList().Count.Should().Be(0);
        }
    }
}