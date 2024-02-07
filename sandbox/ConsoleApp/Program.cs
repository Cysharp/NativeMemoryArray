using Cysharp.Collections;
using System;
using System.IO;
using System.Linq;


var yaki = new NativeMemoryArray<int>(100);
var tako = new NativeMemoryArray<int>(100, skipZeroClear: true, addMemoryPressure: true);

yaki.Dispose();
tako.Dispose();

var z = new NativeMemoryArray<int>(100);
_ = z[0];
//_ = z[-1]; // System.IndexOutOfRangeException: Index was outside the bounds of the array.


var bin1 = new NativeMemoryArray<byte>((long)int.MaxValue + 1024);
var bin2 = new NativeMemoryArray<byte>((long)int.MaxValue + 1024);
Fill(ref bin1);

{
    using var handle = File.OpenHandle("foo.bin", FileMode.Create, FileAccess.Write, options: FileOptions.Asynchronous);
    await RandomAccess.WriteAsync(handle, bin1.AsReadOnlyMemoryList(), 0);
}

{
    using var handle = File.OpenHandle("foo.bin", FileMode.Open, FileAccess.Read, options: FileOptions.Asynchronous);
    await RandomAccess.ReadAsync(handle, bin2.AsMemoryList(), 0);
}

var a = bin1.AsReadOnlyMemoryList();
var b = bin2.AsReadOnlyMemoryList();

Console.WriteLine(a[0].Span.SequenceEqual(b[0].Span));
Console.WriteLine(a[1].Span.SequenceEqual(b[1].Span));

bin1.Dispose();
bin2.Dispose();

// allow use Span<T> in async context.
static void Fill(ref NativeMemoryArray<byte> bin)
{
    var i = 0;
    foreach (var item in bin)
    {
        if (i++ == 0)
        {
            item.Fill(100);
        }
        else
        {
            item.Fill(200);
        }
    }
}