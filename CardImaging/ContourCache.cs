using System.Collections.Generic;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.Util;

namespace CardImaging
{
    internal class ContourCache
    {
        private HashSet<VectorOfPointAtAngle> _cache = new HashSet<VectorOfPointAtAngle>();

        internal void Add(VectorOfPoint vector, double angle)
        {
            var item = new VectorOfPointAtAngle(vector, angle);

            if (!_cache.Contains(item))
            {
                _cache.Add(item);
            }
         }

        internal bool Contains(VectorOfPoint vector, double angle)
        {
            var item = new VectorOfPointAtAngle(vector, angle);
            return _cache.Contains(item);
        }

        internal class VectorOfPointAtAngle
        {
            /// <summary>
            /// Angle
            /// </summary>
            internal double Angle { get; set; }

            /// <summary>
            /// Rotated Rect
            /// </summary>
            internal RotatedRect RotatedRect { get; set; }

            internal VectorOfPointAtAngle(VectorOfPoint vector, double angle)
            {
                Angle = angle;
                RotatedRect = CvInvoke.MinAreaRect(vector);
            }

            public override bool Equals(object obj)
            {
                if (obj==null)
                {
                    return false;
                }

                if (!(obj is VectorOfPointAtAngle))
                {
                    return false;
                }

                if (((VectorOfPointAtAngle)obj).Angle != this.Angle)
                {
                    return false;
                }

                if (!((VectorOfPointAtAngle)obj).RotatedRect.Equals(this.RotatedRect))
                {
                    return false;
                }

                return true;
            }

            public override int GetHashCode()
            {
                return RotatedRect.GetHashCode() ^ Angle.GetHashCode();
            }
        }
    }
}
