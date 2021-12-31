﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewHorizons.External
{
    public class AsteroidBeltModule : Module
    {
        public float InnerRadius { get; set; }
        public float OuterRadius { get; set; }
        public float Inclination { get; set; }
        public float LongitudeOfAscendingNode { get; set; }
        public int RandomSeed { get; set; }
    }
}