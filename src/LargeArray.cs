using System;
using System.Buffers;
using System.Collections.Generic;

namespace Cysharp.Collections
{
    public sealed class LargeArray<T> : IBufferWriter<T>
    {
        const int MaxArrayLength = 0X7FEFFFFF; // 0x7FFFFFC7;

        public static readonly LargeArray<T> Empty = new LargeArray<T>(0);

        readonly List<Memory<T>> completedChunks;
        readonly long length;
        readonly long chunkSize;

        T[] currentBuffer;
        int currentBufferIndex;
        long written;

        public long Length => length;

        public LargeArray(long length)
            : this(length, MaxArrayLength)
        {
        }

        public LargeArray(long length, int chunkSize)
        {
            if (length == 0)
            {
                completedChunks = new List<Memory<T>>(0);
                currentBuffer = Array.Empty<T>();
            }
            else
            {
                completedChunks = new List<Memory<T>>((int)(length / chunkSize) + 1);
                currentBuffer = new T[Math.Min(chunkSize, length)];
            }
            this.length = length;
            this.chunkSize = chunkSize;

            currentBufferIndex = 0;
            written = 0;
        }

        // for Writer

        public void Advance(int count)
        {
            if (currentBuffer.Length < currentBufferIndex + count) throw new InvalidOperationException($"Advance count:{count} is over than current buffer size.");
            written += count;
            currentBufferIndex += count;
        }

        public Span<T> GetSpan(int sizeHint = 0)
        {
            ValidateCapacity(sizeHint);
            EnsureCapacity(sizeHint);
            return new Span<T>(currentBuffer, currentBufferIndex, currentBuffer.Length - currentBufferIndex);
        }

        public Memory<T> GetMemory(int sizeHint = 0)
        {
            ValidateCapacity(sizeHint);
            EnsureCapacity(sizeHint);
            return new Memory<T>(currentBuffer, currentBufferIndex, currentBuffer.Length - currentBufferIndex);
        }

        public void Reset()
        {
            completedChunks.Clear();
            Array.Clear(currentBuffer, 0, currentBuffer.Length);
            currentBufferIndex = 0;
            written = 0;
        }

        void ValidateCapacity(int sizeHint)
        {
            var rest = length - written;
            if (rest < sizeHint) throw new InvalidOperationException($"sizeHint:{sizeHint} is capacity:{length} over.");
        }

        void EnsureCapacity(int sizeHint)
        {
            if (currentBuffer.Length == currentBufferIndex || currentBuffer.Length - currentBufferIndex < sizeHint)
            {
                completedChunks.Add(new Memory<T>(currentBuffer, 0, currentBufferIndex));
                currentBuffer = new T[Math.Max(sizeHint, Math.Min(chunkSize, length - written))];
                currentBufferIndex = 0;
            }
        }

        // for Reader

        public bool TryGetSingleWrittenSpan(out ReadOnlySpan<T> span)
        {
            if (completedChunks.Count == 0 && written != 0)
            {
                span = currentBuffer.AsSpan(0, currentBufferIndex);
                return true;
            }
            else if (completedChunks.Count == 1 && currentBufferIndex == 0)
            {
                span = completedChunks[0].Span;
                return true;
            }

            span = Array.Empty<T>();
            return false;
        }

        public ReadOnlySequence<T> AsReadOnlySequence()
        {
            // length...
            if (written == 0) return ReadOnlySequence<T>.Empty;

            Segment lastSegment = null;
            Segment nextSegment = null;
            // TODO:setup empty sequences.


            if (currentBufferIndex != 0)
            {
                lastSegment = nextSegment = new Segment(new Memory<T>(currentBuffer, 0, currentBufferIndex), null);
            }
            for (int i = completedChunks.Count - 1; i >= 0; i--)
            {
                nextSegment = new Segment(completedChunks[i], nextSegment);
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

            return new ReadOnlySequence<T>(nextSegment, 0, lastSegment, lastSegment.Memory.Length);
        }

        class Segment : ReadOnlySequenceSegment<T>
        {
            public Segment(Memory<T> buffer, Segment nextSegment)
            {
                Memory = buffer;
                Next = nextSegment;
            }

            internal void SetRunningIndex(long runningIndex)
            {
                RunningIndex = runningIndex;
            }
        }
    }
}
