// See https://aka.ms/new-console-template for more information
using Cysharp.Collections;
using System.Runtime.InteropServices;



//var buffer = new HugeBuffer((long)1024 * 1024 * 1024 * 1);


var buffer = new HugeBuffer(104);
//var buffer = new byte[10];


if (buffer.TryGetFullSpan(out var span))
{
    Console.WriteLine(span.Length);
}

