using System;

namespace CityGenerator.Runtime.Visualization
{
    [Flags]
    public enum MapType
    {
        None = 0,
        Population = 1 << 0,
        Water = 1 << 1,
        LivePopulation = 1 << 2
    }
}