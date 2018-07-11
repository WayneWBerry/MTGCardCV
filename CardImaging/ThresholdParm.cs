using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardImaging
{
    public class ThresholdParm
    {
        public double Threshold { get; set; }
        public double MaxValue { get; set; }
        /// <summary>
        /// Initialize a new instance of the ThresholdParm class
        /// </summary>
        /// <param name="threshold">Threshold</param>
        /// <param name="maxValue">MaxValue</param>
        /// <param name="angle">Angle</param>
        public ThresholdParm(double threshold, double maxValue)
        {
            Threshold = threshold;
            MaxValue = maxValue;
        }

        public override string ToString()
        {
            return string.Format("{0}-{1}", Threshold, MaxValue);
        }

        public override int GetHashCode()
        {
            return Threshold.GetHashCode() ^ MaxValue.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (!(obj is ThresholdParm))
            {
                return false;
            }

            if (((ThresholdParm)obj).Threshold != Threshold)
            {
                return false;
            }

            if (((ThresholdParm)obj).MaxValue != MaxValue)
            {
                return false;
            }

            return true;
        }
    }
}
