#nullable enable

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Cysharp.Collections
{
    [DebuggerTypeProxy(typeof(NativeMemoryArrayDebugView<>))]
    public sealed unsafe class NativeMemoryArray<T> : IDisposable
        where T : unmanaged
    {
        public static readonly NativeMemoryArray<T> Empty;

        readonly long length;
        readonly bool addMemoryPressure;
        internal readonly byte* buffer;
        bool isDisposed;
        bool isStolen;

        public long Length => length;

        static NativeMemoryArray()
        {
            Empty = new NativeMemoryArray<T>(0);
            Empty.Dispose();
        }

        public NativeMemoryArray(long length, bool skipZeroClear = false, bool addMemoryPressure = false)
        {
            this.length = length;
            this.addMemoryPressure = addMemoryPressure;

            if (length == 0)
            {
#if UNITY_2019_1_OR_NEWER
                buffer = (byte*)Unsafe.AsPointer(ref Unsafe.AsRef<byte>(null));
#else
                buffer = (byte*)Unsafe.AsPointer(ref Unsafe.NullRef<byte>());
#endif
            }
            else
            {
                var allocSize = length * Unsafe.SizeOf<T>();
#if NET6_0_OR_GREATER
                if (skipZeroClear)
                {
                    buffer = (byte*)NativeMemory.Alloc(checked((nuint)length), (nuint)Unsafe.SizeOf<T>());
                }
                else
                {
                    buffer = (byte*)NativeMemory.AllocZeroed(checked((nuint)length), (nuint)Unsafe.SizeOf<T>());
                }
#else
                buffer = (byte*)Marshal.AllocHGlobal((IntPtr)allocSize);
                if (!skipZeroClear)
                {
                    foreach (var span in this)
                    {
                        span.Clear();
                    }
                }
#endif
                if (addMemoryPressure)
                {
                    GC.AddMemoryPressure(allocSize);
                }
            }
        }

        public ref T this[long index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if ((ulong)index >= (ulong)length) ThrowHelper.ThrowIndexOutOfRangeException();
                var memoryIndex = index * Unsafe.SizeOf<T>();
                return ref Unsafe.AsRef<T>(buffer + memoryIndex);
            }
        }

        public Span<T> AsSpan()
        {
            return AsSpan(0);
        }

        public Span<T> AsSpan(long start)
        {
            if ((ulong)start > (ulong)length) ThrowHelper.ThrowArgumentOutOfRangeException(nameof(start));
            return AsSpan(start, checked((int)(length - start)));
        }

        public Span<T> AsSpan(long start, int length)
        {
            if ((ulong)(start + length) > (ulong)this.length) ThrowHelper.ThrowArgumentOutOfRangeException(nameof(length));
            return new Span<T>(buffer + start * Unsafe.SizeOf<T>(), length);
        }

        public Memory<T> AsMemory()
        {
            return AsMemory(0);
        }

        public Memory<T> AsMemory(long start)
        {
            if ((ulong)start > (ulong)length) ThrowHelper.ThrowArgumentOutOfRangeException(nameof(start));
            return AsMemory(start, checked((int)(length - start)));
        }

        public Memory<T> AsMemory(long start, int length)
        {
            if ((ulong)(start + length) > (ulong)(this.length)) ThrowHelper.ThrowArgumentOutOfRangeException(nameof(length));
            return new PointerMemoryManager<T>(buffer + start * Unsafe.SizeOf<T>(), length).Memory;
        }

        public Stream AsStream()
        {
            return new UnmanagedMemoryStream(buffer, length * Unsafe.SizeOf<T>());
        }

        public Stream AsStream(long offset)
        {
            if ((ulong)offset > (ulong)length) ThrowHelper.ThrowArgumentOutOfRangeException(nameof(offset));
            return new UnmanagedMemoryStream(buffer + offset * Unsafe.SizeOf<T>(), length * Unsafe.SizeOf<T>());
        }

        public Stream AsStream(FileAccess fileAccess)
        {
            var len = length * Unsafe.SizeOf<T>();
            return new UnmanagedMemoryStream(buffer, len, len, fileAccess);
        }

        public Stream AsStream(long offset, FileAccess fileAccess)
        {
            if ((ulong)offset > (ulong)length) ThrowHelper.ThrowArgumentOutOfRangeException(nameof(offset));
            var len = length * Unsafe.SizeOf<T>();
            return new UnmanagedMemoryStream(buffer + offset * Unsafe.SizeOf<T>(), len, len, fileAccess);
        }

        public Stream AsStream(long offset, long length)
        {
            if ((ulong)offset > (ulong)this.length) ThrowHelper.ThrowArgumentOutOfRangeException(nameof(offset));
            if (offset + length > this.length) ThrowHelper.ThrowArgumentOutOfRangeException(nameof(length));

            return new UnmanagedMemoryStream(buffer + offset * Unsafe.SizeOf<T>(), length * Unsafe.SizeOf<T>());
        }

        public Stream AsStream(long offset, long length, FileAccess fileAccess)
        {
            if ((ulong)offset > (ulong)this.length) ThrowHelper.ThrowArgumentOutOfRangeException(nameof(offset));
            if (offset + length > this.length) ThrowHelper.ThrowArgumentOutOfRangeException(nameof(length));

            var len = length * Unsafe.SizeOf<T>();
            return new UnmanagedMemoryStream(buffer + offset * Unsafe.SizeOf<T>(), len, len, fileAccess);
        }

        public ref T GetPinnableReference()
        {
            if (length == 0)
            {
#if UNITY_2019_1_OR_NEWER
                return ref Unsafe.AsRef<T>(null);
#else
                return ref Unsafe.NullRef<T>();
#endif
            }
            return ref this[0];
        }

        public byte* StealPointer()
        {
            isStolen = true;
            return buffer;
        }

        public bool TryGetFullSpan(out Span<T> span)
        {
            if (length < int.MaxValue)
            {
                span = new Span<T>(buffer, (int)length);
                return true;
            }
            else
            {
                span = default;
                return false;
            }
        }

        public IBufferWriter<T> CreateBufferWriter()
        {
            return new NativeMemoryArrayBufferWriter<T>(this);
        }

        public SpanSequence AsSpanSequence(int chunkSize = int.MaxValue)
        {
            return new SpanSequence(this, chunkSize);
        }

        public MemorySequence AsMemorySequence(int chunkSize = int.MaxValue)
        {
            return new MemorySequence(this, chunkSize);
        }

        public IReadOnlyList<Memory<T>> AsMemoryList(int chunkSize = int.MaxValue)
        {
            if (length == 0) return Array.Empty<Memory<T>>();

            var array = new Memory<T>[(long)length <= chunkSize ? 1 : (long)length / chunkSize + 1];
            {
                var i = 0;
                foreach (var item in AsMemorySequence(chunkSize))
                {
                    array[i++] = item;
                }
            }

            return array;
        }

        public IReadOnlyList<ReadOnlyMemory<T>> AsReadOnlyMemoryList(int chunkSize = int.MaxValue)
        {
            if (length == 0) return Array.Empty<ReadOnlyMemory<T>>();

            var array = new ReadOnlyMemory<T>[(long)length <= chunkSize ? 1 : (long)length / chunkSize + 1];
            {
                var i = 0;
                foreach (var item in AsMemorySequence(chunkSize))
                {
                    array[i++] = item;
                }
            }

            return array;
        }

        public ReadOnlySequence<T> AsReadOnlySequence(int chunkSize = int.MaxValue)
        {
            if (length == 0) return ReadOnlySequence<T>.Empty;

            var array = new Segment[(long)length <= chunkSize ? 1 : (long)length / chunkSize + 1];
            {
                var i = 0;
                foreach (var item in AsMemorySequence(chunkSize))
                {
                    array[i++] = new Segment(item);
                }
            }

            long running = 0;
            for (int i = 0; i < array.Length; i++)
            {
                var next = i < array.Length - 1 ? array[i + 1] : null;
                array[i].SetRunningIndexAndNext(running, next);
                running += array[i].Memory.Length;
            }

            var firstSegment = array[0];
            var lastSegment = array[array.Length - 1];
            return new ReadOnlySequence<T>(firstSegment, 0, lastSegment, lastSegment.Memory.Length);
        }

        public SpanSequence GetEnumerator()
        {
            return AsSpanSequence(int.MaxValue);
        }

        public override string ToString()
        {
            return typeof(T).Name + "[" + length + "]";
        }

        public void Dispose()
        {
            DisposeCore();
            GC.SuppressFinalize(this);
        }

        void DisposeCore()
        {
            if (!isDisposed && !isStolen)
            {
                isDisposed = true;
#if UNITY_2019_1_OR_NEWER
                if (buffer == null) return;
#else
                if (Unsafe.IsNullRef(ref Unsafe.AsRef<byte>(buffer))) return;
#endif

#if NET6_0_OR_GREATER
                NativeMemory.Free(buffer);
#else
                Marshal.FreeHGlobal((IntPtr)buffer);
#endif
                if (addMemoryPressure)
                {
                    GC.RemoveMemoryPressure(length * Unsafe.SizeOf<T>());
                }
            }
        }

        ~NativeMemoryArray()
        {
            DisposeCore();
        }

        public struct SpanSequence
        {
            readonly NativeMemoryArray<T> nativeArray;
            readonly int chunkSize;
            long index;
            long sliceStart;

            internal SpanSequence(NativeMemoryArray<T> nativeArray, int chunkSize)
            {
                this.nativeArray = nativeArray;
                index = 0;
                sliceStart = 0;
                this.chunkSize = chunkSize;
            }

            public SpanSequence GetEnumerator() => this;

            public Span<T> Current
            {
                get
                {
                    return nativeArray.AsSpan(sliceStart, (int)Math.Min(chunkSize, nativeArray.length - sliceStart));
                }
            }

            public bool MoveNext()
            {
                if (index < nativeArray.length)
                {
                    sliceStart = index;
                    index += chunkSize;
                    return true;
                }
                return false;
            }
        }

        public struct MemorySequence
        {
            readonly NativeMemoryArray<T> nativeArray;
            readonly int chunkSize;
            long index;
            long sliceStart;

            internal MemorySequence(NativeMemoryArray<T> nativeArray, int chunkSize)
            {
                this.nativeArray = nativeArray;
                index = 0;
                sliceStart = 0;
                this.chunkSize = chunkSize;
            }

            public MemorySequence GetEnumerator() => this;

            public Memory<T> Current
            {
                get
                {
                    return nativeArray.AsMemory(sliceStart, (int)Math.Min(chunkSize, nativeArray.length - sliceStart));
                }
            }

            public bool MoveNext()
            {
                if (index < nativeArray.length)
                {
                    sliceStart = index;
                    index += chunkSize;
                    return true;
                }
                return false;
            }
        }

        class Segment : ReadOnlySequenceSegment<T>
        {
            public Segment(Memory<T> buffer)
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

    internal sealed unsafe class NativeMemoryArrayBufferWriter<T> : IBufferWriter<T>
        where T : unmanaged
    {
        readonly NativeMemoryArray<T> nativeArray;
        PointerMemoryManager<T>? pointerMemoryManager;
        long written;

        internal NativeMemoryArrayBufferWriter(NativeMemoryArray<T> nativeArray)
        {
            this.nativeArray = nativeArray;
            pointerMemoryManager = null;
        }

        public void Advance(int count)
        {
            written += count;
            if (pointerMemoryManager != null)
            {
                pointerMemoryManager.AllowReuse();
            }
        }

        public Span<T> GetSpan(int sizeHint = 0)
        {
            if (sizeHint < 0) throw new InvalidOperationException($"sizeHint:{sizeHint} is invalid range.");
            if (nativeArray.Length - written < sizeHint) throw new InvalidOperationException($"sizeHint:{sizeHint} is capacity:{nativeArray.Length} - written:{written} over");
            var length = (int)Math.Min(int.MaxValue, nativeArray.Length - written);

            if (length == 0) return Array.Empty<T>();
            return new Span<T>(nativeArray.buffer + written * Unsafe.SizeOf<T>(), length);
        }

        public Memory<T> GetMemory(int sizeHint = 0)
        {
            if (sizeHint < 0) throw new InvalidOperationException($"sizeHint:{sizeHint} is invalid range.");
            if (nativeArray.Length - written < sizeHint) throw new InvalidOperationException($"sizeHint:{sizeHint} is capacity:{nativeArray.Length} - written:{written} over");
            var length = (int)Math.Min(int.MaxValue, nativeArray.Length - written);
            if (length == 0) return Array.Empty<T>();

            if (pointerMemoryManager == null)
            {
                pointerMemoryManager = new PointerMemoryManager<T>(nativeArray.buffer + written * Unsafe.SizeOf<T>(), length);
            }
            else
            {
                pointerMemoryManager.Reset(nativeArray.buffer + written * Unsafe.SizeOf<T>(), length);
            }

            return pointerMemoryManager.Memory;
        }
    }

    internal sealed class NativeMemoryArrayDebugView<T>
        where T : unmanaged
    {
        private readonly NativeMemoryArray<T> array;

        public NativeMemoryArrayDebugView(NativeMemoryArray<T> array)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }
            this.array = array;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public Span<T> Items
        {
            get
            {
                if (array.TryGetFullSpan(out var span))
                {
                    return span;
                }
                else
                {
                    return array.AsSpan(0, 1000000); // limit
                }
            }
        }
    }
}
