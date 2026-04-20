using System;

namespace CityGenerator.Runtime.Visualization
{
    [Flags]
    public enum MapType
    {
        None = 0,
        Population = 1 << 0,
        Water = 1 << 1,
        Background = 1 << 2,
        LivePopulation = 1 << 3
    }
}