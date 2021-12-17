// See https://aka.ms/new-console-template for more information
using Cysharp.Collections;



//var buffer = new HugeBuffer((long)1024 * 1024 * 1024 * 1);


var buffer = new HugeBuffer(104);

var span = buffer.AsSpan();
for (int i = 0; i < 104; i++)
{
    span[i] = (byte)i;
}

for (int i = 0; i < 104; i++)
{
    Console.WriteLine(buffer[i]);
}



