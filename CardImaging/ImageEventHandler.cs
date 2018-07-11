using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;

namespace CardImaging
{
    public enum ImageType
    {
        /// <summary>
        /// Field Photo Contoured Looking For Cards
        /// </summary>
        FieldContoured,

        /// <summary>
        /// Card Options Cropped From Field
        /// </summary>
        CardCropped,

        /// <summary>
        /// Card Options Contoured Looking For Titles
        /// </summary>
        CardContoured,

        /// <summary>
        /// Title Options Cropped From Card
        /// </summary>
        TitleCropped,
        ArtCropped,
        TitleFiltered,
        TitleThreshold,
        TitleMask,
        TitleDilate,
        TitleErode,
        SetIcon,
    }

    public delegate void PropertyChangeHandler(object sender, ImageEventArgs data);

    public class ImageEventArgs : EventArgs, IDisposable
    {
        /// <summary>
        /// Flag: Has Dispose already been called?
        /// </summary>
        private bool _disposed = false;

        public ImageEventArgs(Guid cardId, Guid imageId, ImageType imageType, 
            Mat image, 
            double angle,
            float x,
            float y,
            CannyParam cannyParameter = null,
            ThresholdParm thresholdParamter = null)
        {
            CardId = cardId;
            ImageId = imageId;
            ImageType = imageType;
            Image = image.Clone();
            Angle = angle;
            X = x;
            Y = y;
            CannyParameters = cannyParameter;
            ThresholdParamters = thresholdParamter;
        }

        /// <summary>
        /// Unique Card Identifier
        /// </summary>
        public Guid CardId { get; private set; }

        /// <summary>
        /// Unique Image Identifier
        /// </summary>
        public Guid ImageId { get; private set; }

        /// <summary>
        /// Type of Image
        /// </summary>
        public ImageType ImageType { get; private set; }

        /// <summary>
        /// Image
        /// </summary>
        public Mat Image { get; private set; }

        /// <summary>
        /// Angle of Image From Origin
        /// </summary>
        public double Angle { get; private set; }

        /// <summary>
        /// X of Image From Origin
        /// </summary>
        public float X { get; private set; }

        /// <summary>
        /// X of Image From Origin
        /// </summary>
        public float Y { get; private set; }

        /// <summary>
        /// Canny parameters that made up this image
        /// </summary>
        public CannyParam CannyParameters { get; private set; }

        /// <summary>
        /// Thresold parameters that made up this image
        /// </summary>
        public ThresholdParm ThresholdParamters { get; private set; }

        /// <summary>
        /// Public implementation of Dispose pattern callable by consumers.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected implementation of Dispose pattern.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                Image.Dispose();
                Image = null;
            }

            // Free any unmanaged objects here.
            //

            _disposed = true;
        }
    }
}
