﻿ using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JaiSeqX.JAI.Types
{
    public class JOscillator
    {
        public JOscillatorTarget target;
        public float rate;
        public JOscillatorVector[] vectors;
        public float Width;
        public float Vertex;
    }

    public class JOscillatorVector
    {
        public short mode;
        public short time;
        public short value;
    }

    public enum JOscillatorVectorMode
    {
        Linear = 1,
        Square = 2,
        SquareRoot = 3,
        SampleCell = 4,
        Loop = 5,
        Hold = 6,
        Stop = 7,
    }
    public enum JOscillatorTarget
    {
        Volume = 1,
        Pitch = 2,
        Pan = 3,
        FX  = 4,
        Dolby = 5
    }
}
