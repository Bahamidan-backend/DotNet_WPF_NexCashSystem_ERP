using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POSSystem.WPF.StaticFunction
{
    public class PriceHelper
    {
        public static decimal RoundToNearest(decimal value, decimal step = 500)
        {
            return Math.Round(value / step, MidpointRounding.AwayFromZero) * step;
        }
        public static (decimal Half, decimal Quarter) GetFractionPrices(decimal originalPrice)
        {
            decimal half = RoundToNearest(originalPrice / 2);
            decimal quarter = RoundToNearest(originalPrice / 4);

            return (half, quarter);
        }

    }
}
