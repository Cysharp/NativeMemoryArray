using System;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Cysharp.Collections;

var z  = new NativeMemoryArray<int>(100);
_ = z[-1];


using var bin1 = new NativeMemoryArray<byte>((long)int.MaxValue + 1024);
using var bin2 = new NativeMemoryArray<byte>((long)int.MaxValue + 1024);

var i = 0;
foreach (var item in bin1)
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