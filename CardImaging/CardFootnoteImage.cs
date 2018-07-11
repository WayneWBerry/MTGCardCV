using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;

namespace CardImaging
{
    public class CardFootnoteImage : Image
    {
        /// <summary>
        /// Initialize a new instance of the CardFootnoteImage class
        /// </summary>
        /// <param name="image">Footnote Image</param>
        public CardFootnoteImage(Mat image) : base(image)
        {
        }

        /// <summary>
        /// Get the test thresolds for searching the image
        /// </summary>
        /// <returns>Enumeration of double[2] with first and second thresolds</returns>
        internal IEnumerable<CannyParam> GetCannyParameters()
        {
            yield return new CannyParam(150, 75, true, 3);
        }
    }
}
