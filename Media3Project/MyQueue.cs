using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media3Project
{
    class MyQueue
    {
        float[] Queue = new float[3];
        public void Add(float enqueue)
        {
            Queue[0] = Queue[1];
            Queue[1] = Queue[2];
            Queue[2] = enqueue;
        }
        public float Fetch(int index)
        {
            return Queue[index];
        }
    }
}
