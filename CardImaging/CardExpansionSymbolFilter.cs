using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CardModel;

namespace CardImaging
{
    internal static class CardExpansionSymbolFilter
    {

        public static Dictionary<MTGCardFrame, float> RightMargin = new Dictionary<MTGCardFrame, float>();
        public static Dictionary<MTGCardFrame, float> LeftMargin = new Dictionary<MTGCardFrame, float>();
        public static Dictionary<MTGCardFrame, float> TopMargin = new Dictionary<MTGCardFrame, float>();
        public static Dictionary<MTGCardFrame, float> BottomMargin = new Dictionary<MTGCardFrame, float>();

        static CardExpansionSymbolFilter()
        {
            LeftMargin.Add(MTGCardFrame.Original, .77F);
            RightMargin.Add(MTGCardFrame.Original, .925F);
            TopMargin.Add(MTGCardFrame.Original, .558F);
            BottomMargin.Add(MTGCardFrame.Original, .605F);

            LeftMargin.Add(MTGCardFrame.M15, .820F);
            RightMargin.Add(MTGCardFrame.M15, .943F);
            TopMargin.Add(MTGCardFrame.M15, .573F);
            BottomMargin.Add(MTGCardFrame.M15, .615F);

            LeftMargin.Add(MTGCardFrame.Modern, .80F);
            RightMargin.Add(MTGCardFrame.Modern, .948F);
            TopMargin.Add(MTGCardFrame.Modern, .573F);
            BottomMargin.Add(MTGCardFrame.Modern, .62F);

        }

        internal static Rectangle BoundingRectangle(MTGCardFrame mcf, Size cardSize)
        {
            return new Rectangle(
                (int)(LeftMargin[mcf] * cardSize.Width),
                (int)(TopMargin[mcf] * cardSize.Height),
                (int)((RightMargin[mcf] - LeftMargin[mcf]) * cardSize.Width),
                (int)((BottomMargin[mcf] - TopMargin[mcf]) * cardSize.Height));
        }

        /// <summary>
        /// Functional Filter To Find Icon Contours
        /// </summary>
        internal static Func<MTGCardFrame, Size, Rectangle, bool> ExpansionSymbolFilterPass1 = new Func<MTGCardFrame, Size, Rectangle, bool>((mcf, s, r) =>
        {
            if (r.Width <= 0)
            {
                return false;
            }

            if (r.Height <= 0)
            {
                return false;
            }

            if (s.Width <= 0)
            {
                return false;
            }

            if (s.Height <= 0)
            {
                return false;
            }

            float relativeLeft = r.X / (float)s.Width;
            float relativeRight = (r.X + r.Width) / (float)s.Width;
            float relativeTop = r.Y / (float)s.Height;
            float relativeBottom = (r.Y + r.Height) / (float)s.Height;

            //  Icon Should Fall Within Margins
            if (relativeLeft < LeftMargin[mcf])
            {
                return false;
            }

            if (relativeRight > RightMargin[mcf])
            {
                return false;
            }

            if (relativeTop < TopMargin[mcf])
            {
                return false;
            }

            if (relativeBottom > BottomMargin[mcf])
            {
                return false;
            }

            return true;
        });

        /// <summary>
        /// Functional Filter To Find Icon Contours
        /// </summary>
        internal static Func<MTGCardFrame, Size, Rectangle, bool> ExpansionSymbolFilterPass2 = new Func<MTGCardFrame, Size, Rectangle, bool>((mcf, s, r) =>
        {
            if (s.Width <= 0)
            {
                return false;
            }

            if (s.Height <= 0)
            {
                return false;
            }

            float width = r.Width;
            float height = r.Height;
            float aspectRatio = width / height;
            float area = width * height;
            float widthRatio = r.Width / (float)s.Width;
            float heightRatio = r.Height / (float)s.Height;
            float rotatedWidthRatio = r.Width / (float)s.Width;
            float centerY = (r.Y + (r.Height / 2.0F));
            float centerX = (r.X + (r.Width / 2.0F));
            float relativeCenterY = centerY / (float)s.Height;
            float relativeCenterX = (r.X + (r.Width / 2.0F)) / (float)s.Width;
            float relativeLeft = r.X / (float)s.Width;
            float relativeRight = (r.X + r.Width) / (float)s.Width;
            float relativeTop = r.Y / (float)s.Height;
            float relativeBottom = (r.Y + r.Height) / (float)s.Height;

            if (r.Width <= 5)
            {
                // To small to classify
                return false;
            }

            if (r.Height <= 5)
            {
                // To small to classify
                return false;
            }

            //  Icon Should Fall Within Margins
            if (relativeLeft < LeftMargin[mcf])
            {
                return false;
            }

            if (relativeRight > RightMargin[mcf])
            {
                return false;
            }

            if (relativeTop < TopMargin[mcf])
            {
                return false;
            }

            if (relativeBottom > BottomMargin[mcf])
            {
                return false;
            }

            // Icons need to fit into the known aspect ratios
            bool withInSquareAspectRatio = (aspectRatio > .99F) && (aspectRatio < 1.44F);
            bool withInRectAspectRatiro = (aspectRatio > 1.55F) && (aspectRatio < 2.9F);

            if (!withInSquareAspectRatio && !withInRectAspectRatiro)
            {
                return false;
            }

            Debug.WriteLine("Set Icon Contour - Center: {0}/{1} Relative Center: ({8}%)/({9}%) Width: {2} ({10}%) Height: {3} ({11}%) Area: {4} AspectRatio: {5} Image Size: {6}/{7}",
              centerX, centerY,
              r.Width, r.Height,
              area, aspectRatio,
              s.Width, s.Height,
              relativeCenterX * 100.0,
              relativeCenterY * 100.0,
              widthRatio * 100.0,
              heightRatio * 100.0);

            return true;
        });
    }
}
