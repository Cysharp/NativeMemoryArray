using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace XtdArray
{
    public interface IExtendedArray<T>
    {
        // bool TryGetFullSpan(out Span<T> span);
        // AsSpanSeqeunce
        // AsMemorySeqeunce
        // AsReadOnlyList
        // ReadOnlySequence<byte> AsReadOnlySeqeunce(int chunkSize = int.MaxValue);
        // IBufferWriter<T> CreateBufferWriter();
    }
}
