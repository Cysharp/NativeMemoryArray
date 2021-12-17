using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Cysharp.Collections
{
    public sealed class HugeBuffer
    {
        // .NET 6 is 0x7FFFFFC7(Array.MaxLength)
        // before, byte is 0x7FFFFFC7, others is 0X7FEFFFFF
        const int MaxArrayLength = 0X7FEFFFFF;

        const int BlockSize = 64;

        readonly Block64[] buffer;
        readonly long length;

        public long Length => length;

        public HugeBuffer(long length)
        {
            // TODO:0 length
            this.length = length;
            this.buffer = new Block64[(length / BlockSize) + 1];
        }

        public Span<byte> AsSpan()
        {
            // TODO:can not do it.
            return MemoryMarshal.AsBytes<Block64>(buffer);
        }

        // TODO: GetPinnableReference
        // ref Unsafe.AsRef<T>(null)

        public byte this[long index]
        {
            get
            {
                var block = buffer[index / BlockSize];
                var offset = (int)(index % BlockSize);
                return Unsafe.Add(ref Unsafe.As<Block64, byte>(ref block), offset);
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        // TODO:Slice


        public WritableSequence GetEnumerator()
        {
            return new WritableSequence();
        }

        // TODO: use this?
        static long RoundUpToPowerOf2(long value)
        {
            --value;
            value |= value >> 1;
            value |= value >> 2;
            value |= value >> 4;
            value |= value >> 8;
            value |= value >> 16;
            value |= value >> 32;
            return value + 1;
        }

        // TODO:writable???
        public ref struct WritableSequence
        {
            HugeBuffer hugeBuffer;
            int sequenceIndex;

            WritableSequence(HugeBuffer hugeBuffer)
            {
                this.hugeBuffer = hugeBuffer;
                this.sequenceIndex = 0;
            }

            public Span<byte> Current => default;

            public bool MoveNext()
            {
                return false;
            }
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = 64)]
    internal struct Block64 { }





}
