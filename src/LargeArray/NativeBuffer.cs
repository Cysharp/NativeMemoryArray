#if NET6_0_OR_GREATER

using System;
using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Cysharp.Collections
{
    public sealed unsafe class NativeBuffer : IDisposable
    {
        internal readonly byte* buffer;
        readonly nuint length;
        bool isDisposed;

        public nuint Length => length;

        public NativeBuffer(nuint length)
        {
            // TODO: for .NET STANDARD
            // Marshal.AllocHGlobal((IntPtr))
            this.length = length;
            this.buffer = (byte*)NativeMemory.Alloc(length);
        }

        public ref byte this[nuint index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (index >= length) ThrowArgumentOutOfRangeException(nameof(index));
                return ref buffer[index];
            }
        }

        public Span<byte> Slice(nuint start, int length)
        {
            if (start + checked((nuint)length) > this.length) ThrowArgumentOutOfRangeException(nameof(length));
            return new Span<byte>(buffer + start, length);
        }

        public Memory<byte> SliceMemory(nuint start, int length)
        {
            if (start + checked((nuint)length) > this.length) ThrowArgumentOutOfRangeException(nameof(length));
            return new PointerMemoryManager(buffer + start, length).Memory;
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
            if (length < int.MaxValue)
            {
                span = new Span<byte>(buffer, (int)length);
                return true;
            }
            else
            {
                span = default;
                return false;
            }
        }

        public IBufferWriter<byte> CreateBufferWriter()
        {
            return new NativeBufferWriter(this);
        }

        public SpanSequence AsSpanSequence(int chunkSize = int.MaxValue)
        {
            return new SpanSequence(this, chunkSize);
        }

        public MemorySequence AsMemorySequence(int chunkSize = int.MaxValue)
        {
            return new MemorySequence(this, chunkSize);
        }

        public ReadOnlySequence<byte> AsReadOnlySequence(int chunkSize = int.MaxValue)
        {
            // TODO:use chunkSize
            // TODO: length == 0
            // TODO: chunksize == 0?


            // var array = new Segment[length / chunkSize]
            foreach (var item in this.AsMemorySequence(chunkSize))
            {
                new Segment(item);
            }




            Segment? lastSegment = null;
            Segment? nextSegment = null;

            nuint start = length;
            while (start != 0)
            {
                var last = start;
                start = (nuint)Math.Max(0, (long)last - int.MaxValue);

                var memory = new PointerMemoryManager(buffer + start, (int)(last - start)).Memory;
                nextSegment = new Segment(memory, nextSegment);
                if (lastSegment == null)
                {
                    lastSegment = nextSegment;
                }
            }

            var firstSegment = nextSegment;
            var segment = firstSegment;
            var index = 0;
            while (segment != null)
            {
                segment.SetRunningIndex(index);
                index += segment.Memory.Length;
                segment = segment.Next as Segment;
            }

            Debug.Assert(nextSegment != null);
            Debug.Assert(lastSegment != null);
            return new ReadOnlySequence<byte>(nextSegment, 0, lastSegment, lastSegment!.Memory.Length);
        }

        public SpanSequence GetEnumerator()
        {
            return this.AsSpanSequence(int.MaxValue);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [DoesNotReturn]
        void ThrowArgumentOutOfRangeException(string paramName)
        {
            throw new ArgumentOutOfRangeException(paramName);
        }

        public void Dispose()
        {
            DisposeCore();
            GC.SuppressFinalize(this);
        }

        void DisposeCore()
        {
            if (!isDisposed)
            {
                isDisposed = true;
                NativeMemory.Free(buffer);
            }
        }

        ~NativeBuffer()
        {
            DisposeCore();
        }

        public ref struct SpanSequence
        {
            readonly NativeBuffer nativeBuffer;
            readonly int chunkSize;
            nuint index;
            nuint sliceStart;

            internal SpanSequence(NativeBuffer nativeBuffer, int chunkSize)
            {
                this.nativeBuffer = nativeBuffer;
                this.index = 0;
                this.sliceStart = 0;
                this.chunkSize = chunkSize;
            }

            public SpanSequence GetEnumerator() => this;

            public Span<byte> Current
            {
                get
                {
                    return nativeBuffer.Slice(sliceStart, (int)Math.Min(checked((nuint)chunkSize), nativeBuffer.length - sliceStart));
                }
            }

            public bool MoveNext()
            {
                if (index < nativeBuffer.length)
                {
                    sliceStart = index;
                    index += int.MaxValue;
                    return true;
                }
                return false;
            }
        }

        public ref struct MemorySequence
        {
            readonly NativeBuffer nativeBuffer;
            readonly int chunkSize;
            nuint index;
            nuint sliceStart;

            internal MemorySequence(NativeBuffer nativeBuffer, int chunkSize)
            {
                this.nativeBuffer = nativeBuffer;
                this.index = 0;
                this.sliceStart = 0;
                this.chunkSize = chunkSize;
            }

            public MemorySequence GetEnumerator() => this;

            public Memory<byte> Current
            {
                get
                {
                    return nativeBuffer.SliceMemory(sliceStart, (int)Math.Min(checked((nuint)chunkSize), nativeBuffer.length - sliceStart));
                }
            }

            public bool MoveNext()
            {
                if (index < nativeBuffer.length)
                {
                    sliceStart = index;
                    index += int.MaxValue;
                    return true;
                }
                return false;
            }
        }

        class Segment : ReadOnlySequenceSegment<byte>
        {
            public Segment(Memory<byte> buffer)
            {
                Memory = buffer;
            }

            internal void SetRunningIndexAndNext(long runningIndex, Segment? nextSegment)
            {
                RunningIndex = runningIndex;
                Next = nextSegment;
            }
        }
    }

    internal sealed unsafe class NativeBufferWriter : IBufferWriter<byte>
    {
        readonly NativeBuffer nativeBuffer;
        PointerMemoryManager? pointerMemoryManager;
        nuint written;

        internal NativeBufferWriter(NativeBuffer nativeBuffer)
        {
            this.nativeBuffer = nativeBuffer;
            this.pointerMemoryManager = null;
        }

        public void Advance(int count)
        {
            written += checked((nuint)count);
            if (pointerMemoryManager != null)
            {
                pointerMemoryManager.AllowReuse();
            }
        }

        public Span<byte> GetSpan(int sizeHint = 0)
        {
            if (nativeBuffer.Length - written < checked((nuint)sizeHint)) throw new InvalidOperationException($"sizeHint:{sizeHint} is capacity:{nativeBuffer.Length} - written:{written} over");
            var length = (int)Math.Min(int.MaxValue, (nativeBuffer.Length - written));

            return new Span<byte>(nativeBuffer.buffer, length);
        }

        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            if (nativeBuffer.Length - written < checked((nuint)sizeHint)) throw new InvalidOperationException($"sizeHint:{sizeHint} is capacity:{nativeBuffer.Length} - written:{written} over");
            var length = (int)Math.Min(int.MaxValue, (nativeBuffer.Length - written));
            if (pointerMemoryManager == null)
            {
                pointerMemoryManager = new PointerMemoryManager(nativeBuffer.buffer, length);
            }
            else
            {
                pointerMemoryManager.ResetLength(length);
            }

            return pointerMemoryManager.Memory;
        }
    }
}

#endif