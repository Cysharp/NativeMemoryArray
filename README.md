NativeMemoryArray
===
[![GitHub Actions](https://github.com/Cysharp/NativeMemoryArray/workflows/Build-Debug/badge.svg)](https://github.com/Cysharp/NativeMemoryArray/actions) [![Releases](https://img.shields.io/github/release/Cysharp/NativeMemoryArray.svg)](https://github.com/Cysharp/NativeMemoryArray/releases)

NativeMemoryArray is a native-memory backed array for .NET and Unity. The array size of C# is limited to maximum index of 0x7FFFFFC7(2,147,483,591), [Array.MaxLength](https://docs.microsoft.com/en-us/dotnet/api/system.array.maxlength). In terms of `bytes[]`, it is about 2GB. This is very cheep in the modern world. We handle the 4K/8K videos, large data set of deep-learning, huge 3D scan data of point cloud, etc.

`NativeMemoryArray<T>` provides the native-memory backed array, it supports **infinity** length, `Span<T>` and `Memory<T>` slices, `IBufferWriter<T>`, `ReadOnlySeqeunce<T>` and .NET 6's new Scatter/Gather I/O API.

For example, easy to read huge data in-memory.

```csharp
// for example, load large file.
using var handle = File.OpenHandle("4GBfile.bin", FileMode.Open, FileAccess.Read, options: FileOptions.Asynchronous);
var size = RandomAccess.GetLength(handle);

// via .NET 6 Scatter/Gather API
using var array = new NativeMemoryArray<byte>(size);
await RandomAccess.ReadAsync(handle, array.AsMemoryList(), 0);
```

For example, easy to read/write huge data in streaming via `IBufferWriter<T>`, `MemorySequence`.

```csharp
public static async Task ReadFromAsync(NativeMemoryArray<byte> buffer, Stream stream, CancellationToken cancellationToken = default)
{
    var writer = buffer.CreateBufferWriter();

    int read;
    while ((read = await stream.ReadAsync(writer.GetMemory(), cancellationToken).ConfigureAwait(false)) != 0)
    {
        writer.Advance(read);
    }
}

public static async Task WriteToAsync(NativeMemoryArray<byte> buffer, Stream stream, CancellationToken cancellationToken = default)
{
    foreach (var item in buffer.AsMemorySequence())
    {
        await stream.WriteAsync(item, cancellationToken);
    }
}
```

Even if you don't need to deal with huge data, this uses native-memory, so it doesn't use the C# heap. If you are in a situation where you can manage the memory properly, you will have a performance advantage.

Getting Started
---
For .NET, use NuGet. For Unity, please read [Unity](#Unity) section.

> PM> Install-Package [NativeMemoryArray](https://www.nuget.org/packages/NativeMemoryArray)

NativeMemoryArray provides only simple `Cysharp.Collections.NativeMemoryArray<T>` class. It has `where T : unmanaged` constraint so you can only use struct that not includes reference type.

```csharp
// call ctor with length, when Dispose free memory.
using var buffer = new NativeMemoryArray<byte>(10);

buffer[0] = 100;
buffer[1] = 100;

// T allows all unmanaged(struct that not includes reference type) type.
using var mesh = new NativeMemoryArray<Vector3>(100);

// AsSpan() can create Span view so you can use all Span APIs(CopyTo/From, Write/Read etc.).
var otherMeshArray = new Vector3[100];
otherMeshArray.CopyTo(mesh.AsSpan());
```

The difference with `Span<T>` is that `NativeMemoryArray<T>` itself is a class, so it can be placed in a field. This means that, unlike `Span<T>`, it is possible to ensure some long lifetime. Since you can make a slice of `Memory<T>`, you can also pass it into Async methods. Also, the length limit of `Span<T>` is up to int.MaxValue (roughly 2GB), however `NativeMemoryArray<T>` can be larger than that.

The main advantages are as follows

* Allocates from native memory, so it does not use the C# heap.
* There is no limit of 2GB, and infinite length can be allocated as long as memory allows.
* Can pass directly via `IBufferWriter<T>` to `MessagePackSerializer`, `System.Text.Json.Utf8JsonWriter`, `System.IO.Pipelines`, etc.
* Can pass directly via `ReadOnlySequence<T>` to `Utf8JsonWriter`, `System.IO.Pipelines`, etc.
* Can pass huge data directly via `IReadOnlyList<(ReadOnly)Memory<T>>` to `RandomAccess` (Scatter/Gather API).

All `NativeMemoryArray<T>` APIs are as follows

* `NativeMemoryArray(long length, bool skipZeroClear = false, bool addMemoryPressure = false)`
* `long Length`
* `ref T this[long index]`
* `ref T GetPinnableReference()`
* `Span<T> AsSpan()`
* `Span<T> AsSpan(long start)`
* `Span<T> AsSpan(long start, int length)`
* `Memory<T> AsMemory()`
* `Memory<T> AsMemory(long start)`
* `Memory<T> AsMemory(long start, int length)`
* `bool TryGetFullSpan(out Span<T> span)`
* `IBufferWriter<T> CreateBufferWriter()`
* `SpanSequence AsSpanSequence(int chunkSize = int.MaxValue)`
* `MemorySequence AsMemorySequence(int chunkSize = int.MaxValue)`
* `IReadOnlyList<Memory<T>> AsMemoryList(int chunkSize = int.MaxValue)`
* `IReadOnlyList<ReadOnlyMemory<T>> AsReadOnlyMemoryList(int chunkSize = int.MaxValue)`
* `ReadOnlySequence<T> AsReadOnlySequence(int chunkSize = int.MaxValue)`
* `SpanSequence GetEnumerator()`
* `void Dispose()`

`NativeMemoryArray<T>` allocates memory by [NativeMemory.Alloc/AllocZeroed](https://docs.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.nativememory) so you need to call `Dispose()` or use `using scope`. In the default, allocated memory is zero-cleared. You can configure via `bool skipZeroClear`. When `bool addMemoryPressure` is true, calls [GC.AddMemoryPressure](https://docs.microsoft.com/en-us/dotnet/api/system.gc.addmemorypressure) and [GC.RemoveMemoryPressure](https://docs.microsoft.com/en-us/dotnet/api/system.gc.removememorypressure) at alloc/free memory. Default is false but if you want to inform allocated memory size to managed GC, set to true.

`AsSpan()` and `AsMemory()` are APIs for Slice. Returned `Span` and `Memory` possible to allow write operation so you can pass to the Span operation methods. `Span` and `Memory` have limitation of length(int.MaxValue) so if length is omitted, throws exception if array is larger. Using `TryGetFullSpan()` detect can get single full span or not. `AsSpanSequence()` and `AsMemorySequence()` are iterate chunked all data via foreach. Using foreach directly as same as `AsSpanSequence()`.

```csharp
long written = 0;
foreach (var chunk in array)
{
    // do anything
    written += chunk.Length;
}
```

Getting a pointer is almost the same as getting an array. It can be passed as is or with an indexer.

```csharp
// buffer = NativeArray<byte>

fixed (byte* p = buffer)
{
}

fixed (byte* p = &buffer[42])
{
}
```

`CreateBufferWriter()` allows you to get an `IBufferWriter<T>`. This can be passed directly to `MessagePackSerializer.Serialize`, etc., or used in cases such as reading from a `Stream`, where it is retrieved and written chunk by chunk from the beginning.

The `ReadOnlySequence<T>` you can get with `AsReadOnlySequence()` can be passed directly to `MessagePackSerializer.Deserialize`, and [SequenceReader](https://docs.microsoft.com/en-us/dotnet/api/system.buffers.sequencereader-1) is useful to processing large data via streaming.

`AsMemoryList()` and `AsReadOnlySequence()` are convinient data structure for [RandomAccess](https://docs.microsoft.com/en-us/dotnet/api/system.io.randomaccess).`Read/Write` API.

For the simple buffer processing, we provide some utility extension methods.

```csharp
public static async Task ReadFromAsync(this NativeMemoryArray<byte> buffer, Stream stream, IProgress<int>? progress = null, CancellationToken cancellationToken = default)
public static async Task WriteToFileAsync(this NativeMemoryArray<byte> buffer, string path, FileMode mode = FileMode.Create, IProgress<int>? progress = null, CancellationToken cancellationToken = default)
public static async Task WriteToAsync(this NativeMemoryArray<byte> buffer, Stream stream, int chunkSize = int.MaxValue, IProgress<int>? progress = null, CancellationToken cancellationToken = default)
```

This utility is excluded the .NET Standard 2.0 environment since runtime API limitation.

Unity
---
You can install via UPM git URL package or asset package(NativeMemoryArray.*.unitypackage) available in [NativeMemoryArray/releases](https://github.com/Cysharp/NativeMemoryArray/releases) page.

* `https://github.com/Cysharp/NativeMemoryArray.git?path=src/NativeMemoryArray.Unity/Assets/Plugins/NativeMemoryArray`

NativeMemoryArray requires `System.Memory.dll`, `System.Buffer.dll`, `System.Runtime.CompilerServices.Unsafe.dll`. It is not included in git URL so you need get from others or install via .unitypackage only once.

The difference between `NativeArray<T>` and `NativeArray<T>` in Unity is that `NativeArray<T>` is a container for efficient interaction with the Unity Engine(C++) side. `NativeMemoryArray<T>` has a different role because it is for C# side only.

License
---
This library is licensed under the MIT License.
