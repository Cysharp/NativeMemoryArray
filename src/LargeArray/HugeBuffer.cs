#if !NETSTANDARD2_0

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Cysharp.Collections
{
    [StructLayout(LayoutKind.Explicit, Size = 1024)]
    internal struct Block1024 { }

    public sealed class HugeBuffer
    {
        // .NET 6 is 0x7FFFFFC7(Array.MaxLength)
        // before, byte is 0x7FFFFFC7, others is 0X7FEFFFFF
        const int MaxArrayLength = 0X7FEFFFFF;
        const int BlockSize = 1024;

        public static readonly HugeBuffer Empty = new HugeBuffer(0);

        readonly Block1024[] buffer;
        readonly long length;

        public long Length => length;

        public HugeBuffer(long length)
        {
            this.length = length;
            this.buffer = length == 0 ? Array.Empty<Block1024>() : new Block1024[(length / BlockSize) + 1];
        }

        public ref byte this[long index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ref var block = ref buffer[index / BlockSize];
                var offset = (int)(index % BlockSize);
                return ref Unsafe.Add(ref Unsafe.As<Block1024, byte>(ref block), offset);
            }
        }

        public Span<byte> Slice(long start, int length)
        {
            return MemoryMarshal.CreateSpan(ref this[start], length);
        }

        public ref byte GetPinnableReference()
        {
            if (length == 0)
            {
                return ref Unsafe.NullRef<byte>();
            }
            return ref this[0];
        }

        public bool TryGetFullSpan(out Span<byte> span)
        {
            if (length < MaxArrayLength)
            {
                span = MemoryMarshal.AsBytes<Block1024>(buffer);
                return true;
            }
            else
            {
                span = default;
                return false;
            }
        }

        public SpanSequence GetEnumerator()
        {
            return new SpanSequence(this);
        }

        public ref struct SpanSequence
        {
            HugeBuffer hugeBuffer;
            long index;
            long sliceStart;

            internal SpanSequence(HugeBuffer hugeBuffer)
            {
                this.hugeBuffer = hugeBuffer;
                this.index = 0;
                this.sliceStart = 0;
            }

            public Span<byte> Current
            {
                get
                {
                    return hugeBuffer.Slice(sliceStart, (int)Math.Min(MaxArrayLength, hugeBuffer.length - sliceStart));
                }
            }

            public bool MoveNext()
            {
                if (index < hugeBuffer.length)
                {
                    sliceStart = index;
                    index += MaxArrayLength;
                    return true;
                }
                return false;
            }
        }
    }
}

#endif