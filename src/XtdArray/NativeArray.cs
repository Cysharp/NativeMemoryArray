//#if !NETSTANDARD2_0

//using System;
//using System.Collections.Generic;
//using System.Runtime.CompilerServices;
//using System.Runtime.InteropServices;
//using System.Text;

//namespace Cysharp.Collections
//{
//    // TODO: now implementing.

//    public sealed unsafe class NativeArray<T> : IDisposable
//        where T : struct
//    {
//        internal readonly byte* buffer;



//        public NativeArray(nuint length)
//        {
//            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
//            {
//                throw new ArgumentException("");
//            }

//            var size = Unsafe.SizeOf<T>();

//            NativeMemory.Alloc(length, checked((nuint)size));
//        }

//        public ref T this[nuint index]
//        {
//            [MethodImpl(MethodImplOptions.AggressiveInlining)]
//            get
//            {
//                // TODO: check index
//                //if (index >= length) ThrowArgumentOutOfRangeException(nameof(index));
//                var memoryIndex = checked((long)index) * Unsafe.SizeOf<T>();
//                return ref Unsafe.AsRef<T>(buffer + memoryIndex);
//            }
//        }

//        // GetPinnable
//        // TryGetFullSpan

//        // CreateBufferWriter
//        // etc...



//        public void Dispose()
//        {
//            throw new NotImplementedException();
//        }
//    }
//}

//#endif