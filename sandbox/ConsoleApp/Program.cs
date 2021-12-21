// See https://aka.ms/new-console-template for more information

using System;
using System.IO.Pipelines;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Cysharp.Collections;


using var array0 = new NativeMemoryArray<int>(4);
using var array1 = new NativeMemoryArray<int>(4, true);

foreach (var item in array0.AsSpan())
{
    Console.WriteLine(item);
}


foreach (var item in array1.AsSpan())
{
    Console.WriteLine(item);
}

var test = new[] { 1, 10, 100, 1000 };
var test2 = new[] { 1, 10, 100, 1000 }.ToList();
var spa = new[] { 1, 10, 100, 1000 }.AsSpan();

using var array = new NativeMemoryArray<MyClass>(4);

//array[0] = new MyClass { MyProperty = 10 };
//array[1] = new MyClass { MyProperty = 20 };
//array[2] = new MyClass { MyProperty = 30 };
//array[3] = new MyClass { MyProperty = 40 };


//Unsafe.InitBlockUnaligned(





var span = array.AsSpan();
Console.WriteLine("LEN!" + span.Length);
foreach (var item in span)
{
    Console.WriteLine(item.MyProperty);
}

Console.WriteLine("----");

Console.WriteLine(array[0].MyProperty);
Console.WriteLine(array[1].MyProperty);
Console.WriteLine(array[2].MyProperty);
Console.WriteLine(array[3].MyProperty);


public class MyClass
{
    public int MyProperty { get; set; }
}
