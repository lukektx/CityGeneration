using System;
using UnityEngine;

namespace CityGenerator.Runtime.Visualization
{
    [Serializable]
    public class MapDisplayOptions
    {
        public MapType Type;
        public Color Color = Color.white;
        public int priority = 0;
    }
}