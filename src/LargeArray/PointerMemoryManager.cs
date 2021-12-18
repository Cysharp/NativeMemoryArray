using System;
using System.Buffers;

namespace Cysharp.Collections
{
    internal sealed unsafe class PointerMemoryManager : MemoryManager<byte>
    {
        readonly byte* pointer;
        int length;
        bool usingMemory;

        internal PointerMemoryManager(byte* pointer, int length)
        {
            this.pointer = pointer;
            this.length = length;
            this.usingMemory = false;
        }

        protected override void Dispose(bool disposing)
        {
        }

        public override Span<byte> GetSpan()
        {
            usingMemory = true;
            return new Span<byte>(pointer, length);
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

        public void ResetLength(int length)
        {
            if (usingMemory) throw new InvalidOperationException("Memory is using, can not reset.");
            this.length = length;
        }
    }
}