using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Cysharp.Collections
{
    internal sealed unsafe class PointerMemoryManager<T> : MemoryManager<T>
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

#if !NETSTANDARD2_0
            return MemoryMarshal.CreateSpan(ref Unsafe.AsRef<T>(pointer), length);
#else
            return new Span<T>(pointer, length);
#endif
        }

        public override MemoryHandle Pin(int elementIndex = 0)
        {
            return default;
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