using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;

namespace CardImaging
{
    public class CardExpansionSymbolImage : Image
    {
        public CardExpansionSymbolImage(Mat image) : base(image)
        {
        }

        /// <summary>
        /// Get the test thresolds for searching the image
        /// </summary>
        /// <returns>Enumeration of double[2] with first and second thresolds</returns>
        internal IEnumerable<CannyParam> GetCannyParameters()
        {
            yield return new CannyParam(200,100, true, 3);
        }
    }
}
