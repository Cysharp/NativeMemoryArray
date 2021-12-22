using Cysharp.Collections;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sample : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        using (var array = new NativeMemoryArray<int>(5))
        {
            array[0] = 100;
            array[1] = 200;
            array[2] = 300;
            array[3] = 400;
            array[4] = 500;


            foreach (var item in array.AsSpan())
            {
                Debug.Log(item);
            }
        }
    }
}
