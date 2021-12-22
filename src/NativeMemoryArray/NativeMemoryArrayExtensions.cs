#if !NETSTANDARD2_0

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Cysharp.Collections
{
    public static class NativeMemoryArrayExtensions
    {
        public static async Task ReadFromAsync(this NativeMemoryArray<byte> buffer, Stream stream, IProgress<int>? progress = null, CancellationToken cancellationToken = default)
        {
            var writer = buffer.CreateBufferWriter();

            int read;
            while ((read = await stream.ReadAsync(writer.GetMemory(), cancellationToken).ConfigureAwait(false)) != 0)
            {
                progress?.Report(read);
                writer.Advance(read);
            }
        }

        public static async Task WriteToFileAsync(this NativeMemoryArray<byte> buffer, string path, FileMode mode = FileMode.Create, IProgress<int>? progress = null, CancellationToken cancellationToken = default)
        {
            using (var fs = new FileStream(path, mode, FileAccess.Write, FileShare.ReadWrite, 1, useAsync: true))
            {
                await buffer.WriteToAsync(fs, progress: progress, cancellationToken: cancellationToken);
            }
        }

        public static async Task WriteToAsync(this NativeMemoryArray<byte> buffer, Stream stream, int chunkSize = int.MaxValue, IProgress<int>? progress = null, CancellationToken cancellationToken = default)
        {
            foreach (var item in buffer.AsReadOnlyMemoryList(chunkSize))
            {
                await stream.WriteAsync(item, cancellationToken);
                progress?.Report(item.Length);
            }
        }
    }
}

#endif