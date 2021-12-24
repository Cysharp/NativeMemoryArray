using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace Cysharp.Collections
{
    internal sealed unsafe class PointerMemoryManager<T> : MemoryManager<T>
        where T : unmanaged
    {
        byte* pointer;
        int length;
        bool usingMemory;

        internal PointerMemoryManager(byte* pointer, int length)
        {
            this.pointer = pointer;
            this.length = length;
            usingMemory = false;
        }

        protected override void Dispose(bool disposing)
        {
        }

        public override Span<T> GetSpan()
        {
            usingMemory = true;
            return new Span<T>(pointer, length);
        }

        public override MemoryHandle Pin(int elementIndex = 0)
        {
            if ((uint)elementIndex >= (uint)length) ThrowHelper.ThrowIndexOutOfRangeException();
            return new MemoryHandle(pointer + elementIndex * Unsafe.SizeOf<T>(), default, this);
        }

        public override void Unpin()
        {
        }

        public void AllowReuse()
        {
            usingMemory = false;
        }

        public void Reset(byte* pointer, int length)
        {
            if (usingMemory) throw new InvalidOperationException("Memory is using, can not reset.");
            this.pointer = pointer;
            this.length = length;
        }
    }
}