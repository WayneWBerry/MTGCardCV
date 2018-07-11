using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardImaging
{
    /// <summary>
    /// Test Thresolds And Angle To Apply To Images For Detection
    /// </summary>
    public class CannyParam
    {
        public double Threshold1 { get; private set; }
        public double Threshold2 { get; private set; }
        public bool L2Graident { get; private set; }
        public int Aperture { get; private set; }

        public CannyParam(double threshold1, double threshold2, bool l2Graident, int aperture)
        {
            if ((aperture & 1) == 0 || (aperture != -1 && (aperture < 3 || aperture > 7)))
            {
                throw new ArgumentException("aperture");
            }

            Threshold1 = threshold1;
            Threshold2 = threshold2;
            L2Graident = l2Graident;
            Aperture = aperture;
        }

        public override string ToString()
        {
            return string.Format("{0}-{1}-{2}-{3}", Threshold1, Threshold2, L2Graident, Aperture);
        }

        public override int GetHashCode()
        {
            return Threshold1.GetHashCode() ^ Threshold2.GetHashCode() ^ L2Graident.GetHashCode() ^ Aperture.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (!(obj is CannyParam))
            {
                return false;
            }

            if (((CannyParam)obj).Threshold1 != Threshold1)
            {
                return false;
            }

            if (((CannyParam)obj).Threshold2 != Threshold2)
            {
                return false;
            }

            if (((CannyParam)obj).L2Graident != L2Graident)
            {
                return false;
            }

            if (((CannyParam)obj).Aperture != Aperture)
            {
                return false;
            }

            return true;
        }
    }
}
