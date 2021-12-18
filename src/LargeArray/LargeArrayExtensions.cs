using System;
using System.Buffers;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Cysharp.Collections
{
    public static class LargeArrayExtensions
    {
        public static async Task CopyFromAsync(this LargeArray<byte> buffer, Stream stream, IProgress<int>? progress = null, CancellationToken cancellationToken = default)
        {
            int read;
            var memory = buffer.GetMemory();
            MemoryMarshal.TryGetArray((ReadOnlyMemory<byte>)memory, out var array);
            while ((read = await stream.ReadAsync(array.Array!, array.Offset, array.Count, cancellationToken).ConfigureAwait(false)) != 0)
            {
                progress?.Report(read);
                buffer.Advance(read);
                memory = buffer.GetMemory();
                MemoryMarshal.TryGetArray((ReadOnlyMemory<byte>)memory, out array);
            }
        }

        public static async Task WriteToFileAsync(this LargeArray<byte> buffer, string path, IProgress<int>? progress = null, CancellationToken cancellationToken = default)
        {
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite, 1, useAsync: true))
            {
                await buffer.WriteToAsync(fs, progress, cancellationToken);
            }
        }

        public static async Task WriteToAsync(this LargeArray<byte> buffer, Stream stream, IProgress<int>? progress = null, CancellationToken cancellationToken = default)
        {
            foreach (var item in buffer.AsReadOnlySequence())
            {
                MemoryMarshal.TryGetArray(item, out var array);
                await stream.WriteAsync(array.Array!, array.Offset, array.Count, cancellationToken).ConfigureAwait(false);
                progress?.Report(item.Length);
            }
        }




    }
}
