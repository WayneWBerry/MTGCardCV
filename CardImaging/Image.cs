using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using CardModel;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;

namespace CardImaging
{
    public class Image : IDisposable
    {
        protected const double LevenshteinDistanceShortCircuit = 1.0;

        protected const RetrType retrType = RetrType.List;

        protected const ChainApproxMethod chainApproxMethod = ChainApproxMethod.ChainApproxNone;

        protected static MCvScalar yellow = new MCvScalar(0, 255, 255);
        protected static MCvScalar green = new MCvScalar(255, 255, 0);
        protected static MCvScalar blue = new MCvScalar(0, 255, 0);
        protected static MCvScalar black = new MCvScalar(0, 0, 0);
        protected static MCvScalar white = new MCvScalar(255, 255, 255);

        /// <summary>
        /// Original Image
        /// </summary>
        private Mat _image;

        /// <summary>
        /// Original Image Size
        /// </summary>
        private Size _imageSize;

        /// <summary>
        /// Rotated Image
        /// </summary>
        private CachedRotatedImage _rotatedImage;

        /// <summary>
        /// Get Gray Image
        /// </summary>
        private CachedRotatedImage _grayImage;

        /// <summary>
        /// Canny Image At Thresold
        /// </summary>
        private CachedImage _cannyImage;

        /// <summary>
        /// Contour Image At Thresold
        /// </summary>
        private CachedImage _contouredImage;

        /// <summary>
        /// Canny Image At Thresold
        /// </summary>
        private CachedThresoldImage _thresholdImage;

        /// <summary>
        /// Cached Filtered Image
        /// </summary>
        private CachedImage _filteredImage;

        /// <summary>
        /// Flag: Has Dispose already been called?
        /// </summary>
        private bool _disposed = false;

        /// <summary>
        /// MTG Sets
        /// </summary>
        protected static MTGAllSets Sets = new MTGAllSets();

        /// <summary>
        /// Valid Characters In the Name
        /// </summary>
        protected static IEnumerable<char> ValidNameCharacters;

        /// <summary>
        /// Maximum Lenght of a Name
        /// </summary>
        protected static int MaxNameLength;

        /// <summary>
        /// Minimum Name Length
        /// </summary>
        protected static int MinNameLength;

        /// <summary>
        /// Image Event Handler
        /// </summary>
        public static event PropertyChangeHandler ImageEvent;

        static Image()
        {
            // Maximum Name Length
            MaxNameLength = Sets.GetSets().Max(set => set.Cards
                .Where(card => card.Name != "Our Market Research Shows That Players Like Really Long Card Names So We Made this Card to Have the Absolute Longest Card Name Ever Elemental" &&
                    card.Name != "The Ultimate Nightmare of Wizards of the Coast® Customer Service").Max(card => card.Name.Length));

            // Minimum Name Length
            MinNameLength = Sets.GetSets().Min(set => set.Cards.Min(card => card.Name.Length));

            string longestName = Sets.GetSets().SelectMany(set => set.Cards.Where(card => card.Name.Length == MaxNameLength)).First().Name;

            // All Valid Characters
            ValidNameCharacters = Sets.GetSets()
                .SelectMany(set => set.Cards.SelectMany(card => card.Name.ToArray().Distinct()).Distinct())
                .Distinct()
                .OrderBy((a) => { return a; }).ToArray();
        }

        /// <summary>
        /// Initilize a new instance of the Image class
        /// </summary>
        /// <param name="image">Image</param>
        public Image(Mat image)
        {
            if (image.Height == 0 || image.Width == 0)
            {
                throw new ArgumentException("image");
            }

            _image = image.Clone();
            _imageSize = image.Size;
        }

        /// <summary>
        /// Original Image Size
        /// </summary>
        public Size Size
        {
            get
            {
                return _imageSize;
            }
        }

        /// <summary>
        /// Image Aspect Ratio
        /// </summary>
        public float AspectRatio
        {
            get
            {
                return Size.Width / Size.Height;
            }
        }

        /// <summary>
        /// Image Area In Pixels
        /// </summary>
        public float Area
        {
            get
            {
                return Size.Width * Size.Height;
            }
        }

        /// <summary>
        /// Card Image
        /// </summary>
        public Mat GetImage()
        {
            return _image.Clone();
        }

        /// <summary>
        /// Get the Gray Image Of The Image
        /// </summary>
        /// <returns></returns>
        public Mat GetGreyImage(double angle)
        {
            if (_grayImage.image == null || _grayImage.angle != angle)
            {
                if (_grayImage.image != null)
                {
                    _grayImage.image.Dispose();
                    _grayImage.image = null;
                }

                using (Mat grayImage = new Mat())
                {
                    using (Mat rotatedImage = GetRotatedImage(angle))
                    {
                        // Convert To Gray Scale
                        CvInvoke.CvtColor(rotatedImage, grayImage, ColorConversion.Bgr2Gray, dstCn: 0);

                        _grayImage.image = grayImage.Clone();
                        _grayImage.angle = angle;
                    }
                }
            }

            return _grayImage.image.Clone();
        }

        /// <summary>
        /// Get the Canny Image Of the Gray Image
        /// </summary>
        public Mat GetCannyImage(Func<Mat> getCannyBase, double angle, CannyParam cannyParameter)
        {
            if (cannyParameter == null)
            {
                throw new ArgumentNullException("cannyParameter");
            }

            if (_cannyImage.image == null || _cannyImage.angle != angle || !_cannyImage.cannyParam.Equals(cannyParameter))
            {
                if (_cannyImage.image != null)
                {
                    _cannyImage.image.Dispose();
                    _cannyImage.image = null;
                }

                using (Mat grayImage = getCannyBase())
                {
                    using (Mat cannyImage = new Mat())
                    {
                        CvInvoke.Canny(grayImage, cannyImage,
                            threshold1: cannyParameter.Threshold1,
                            threshold2: cannyParameter.Threshold2,
                            apertureSize: cannyParameter.Aperture, l2Gradient: cannyParameter.L2Graident);

                        _cannyImage.image = cannyImage.Clone();
                        _cannyImage.angle = angle;
                        _cannyImage.cannyParam = cannyParameter;
                    }
                }
            }

            return _cannyImage.image.Clone();
        }

        /// <summary>
        /// Get Thresold Image
        /// </summary>
        /// <returns></returns>
        public Mat GetThresholdImage(double angle, ThresholdParm thresholdParam)
        {
            //double thresold = 130;
            //double maxValue = 255;

            if (_thresholdImage.image == null || _thresholdImage.angle != angle || !_thresholdImage.thresholdParm.Equals(thresholdParam))
            {
                using (Mat thresoldImage = new Mat())
                {
                    using (Mat rotatedImage = GetRotatedImage(angle))
                    {
                        CvInvoke.Threshold(rotatedImage, thresoldImage, threshold: thresholdParam.Threshold, maxValue: thresholdParam.MaxValue, thresholdType: ThresholdType.BinaryInv);

                        _thresholdImage.image = thresoldImage.Clone();
                        _thresholdImage.angle = angle;
                        _thresholdImage.thresholdParm = thresholdParam;
                    }
                }
            }

            return _thresholdImage.image.Clone();
        }

        /// <summary>
        /// Get Original Title Image With Contour Lines Drawn
        /// </summary>
        public Mat GetContouredImage(double angle, CannyParam cannyParameter, float minArea = 5000.0F)
        {
            if (cannyParameter == null)
            {
                throw new ArgumentNullException("cannyParameter");
            }

            if (_contouredImage.image == null || !_contouredImage.cannyParam.Equals(cannyParameter))
            {
                if (_contouredImage.image != null)
                {
                    _contouredImage.image.Dispose();
                    _contouredImage.image = null;
                }

                _contouredImage.image = GetGreyImage(angle);

                using (Mat titleCannyImage = GetCannyImage(() => { return GetGreyImage(angle); }, angle, cannyParameter))
                {
                    using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
                    {
                        CvInvoke.FindContours(titleCannyImage, contours, hierarchy: null, mode: RetrType.List, method: ChainApproxMethod.ChainApproxNone);

                        for (int i = 1; i < contours.Size; i++)
                        {

                            RotatedRect rotatedRect = CvInvoke.MinAreaRect(contours[i]);
                            float area = rotatedRect.Size.Width * rotatedRect.Size.Height;

                            if (area > minArea)
                            {
                                CvInvoke.DrawContours(_contouredImage.image, contours, i, yellow, thickness: 2);
                                Rectangle box = rotatedRect.MinAreaRect();
                                CvInvoke.Rectangle(_contouredImage.image, box, green, thickness: 2);
                            }
                        }

                        _contouredImage.cannyParam = cannyParameter;
                    }
                }
            }

            return _contouredImage.image.Clone();
        }

        /// <summary>
        /// Rotate Image
        /// </summary>
        /// <param name="angle">Degrees Of Rotation</param>
        public Mat GetRotatedImage(double angle)
        {
            if (angle < 0 || angle >= 360)
            {
                throw new ArgumentException("angle");
            }

            if (_rotatedImage.image == null || _rotatedImage.angle != angle)
            {
                if (_rotatedImage.image != null)
                {
                    _rotatedImage.image.Dispose();
                    _rotatedImage.image = null;
                }

                if (angle == 0)
                {
                    _rotatedImage.image = _image.Clone();
                    _rotatedImage.angle = angle;
                }
                else
                {
                    PointF src_center = new PointF(_image.Cols / 2.0F, _image.Rows / 2.0F);

                    using (Mat rot_mat = new RotationMatrix2D(src_center, angle, 1.0))
                    {
                        using (Mat destinationImage = new Mat())
                        {
                            CvInvoke.WarpAffine(_image, destinationImage, rot_mat, _image.Size, interpMethod: Inter.Cubic);

                            _rotatedImage.image = destinationImage.Clone();
                            _rotatedImage.angle = angle;
                        }
                    }
                }
            }

            return _rotatedImage.image.Clone();
        }

        /// <summary>
        /// Get a filtered image that is good for OCR
        /// </summary>
        public Mat GetFilteredImage(MTGCardFrame cardTitleType, double angle, ThresholdParm thresoldParameter, CannyParam cannyParameter)
        {
            if (_filteredImage.image == null || _filteredImage.cannyParam != cannyParameter || _filteredImage.angle != angle)
            {
                using (Mat titleImage = GetImage())
                {
                    Size titleSize = titleImage.Size;

                    int minX = titleImage.Width;
                    int maxX = titleImage.Height;
                    int minY = 0;
                    int maxY = 0;

                    // Get the Canny Image To Make A Contour Set For the Letters
                    using (Mat titleCannyImage = GetCannyImage(() => { return GetThresholdImage(angle, thresoldParameter); }, angle, cannyParameter))
                    {
                        using (Mat maskedImage = new Mat(titleSize, DepthType.Cv8U, 1))
                        {
                            // White/black out the mask.  Legacy cards are white letters on color, the new style is black letters in a colored title box.
                            maskedImage.SetTo(cardTitleType == MTGCardFrame.M15 ? black : white);

                            using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
                            {
                                CvInvoke.FindContours(titleCannyImage, contours, hierarchy: null, mode: retrType, method: chainApproxMethod);

                                for (int i = 1; i < contours.Size; i++)
                                {
                                    using (VectorOfPoint contour = contours[i])
                                    {
                                        Rectangle rect = CvInvoke.BoundingRectangle(contour);

                                        double relativeY = (double)rect.Y / (double)titleSize.Height;
                                        double relativeX = (double)rect.X / (double)titleSize.Width;
                                        double relativeBottom = ((double)rect.Y + (double)rect.Height) / (double)titleSize.Height;
                                        double relativeRight = ((double)rect.X + (double)rect.Width) / (double)titleSize.Width;
                                        double relativeWidth = (double)rect.Width / (double)titleSize.Width;
                                        double relativeHeight = (double)rect.Height / (double)titleSize.Height;

                                        // The Characters Start In the 10 % of the Title Image, and Complete Within the 
                                        if (((relativeY > .10) && (relativeY < .80) &&
                                            (relativeBottom > .30) && (relativeBottom < .95) &&
                                            (relativeRight > .025) && (relativeX < .95))
                                            && (relativeWidth < .09) && (relativeHeight > .07))
                                        {
                                            // Make a mask of rectangles that represent the title.
                                            CvInvoke.Rectangle(maskedImage, rect, new MCvScalar(255, 255, 255), -1);

                                            // Keep Track Of the Border Area Which Comprises The Letters
                                            minX = Math.Min(rect.X, minX);
                                            minY = Math.Min(rect.Y, minY);
                                            maxX = Math.Max(rect.X + rect.Width, maxX);
                                            maxY = Math.Max(rect.Y + rect.Height, maxY);
                                        }
                                    }
                                }
                            }

                            // The result is the same depth/channels as the thresold
                            using (Mat result = GetThresholdImage(angle, thresoldParameter))
                            {
                                // Clear the result
                                result.SetTo(cardTitleType == MTGCardFrame.M15 ? black : white);

                                // Use the Mask To Take Just The Title From the thresoldImage
                                GetThresholdImage(angle, thresoldParameter).CopyTo(result, maskedImage);

                                _filteredImage.image = result.Clone();
                                _filteredImage.cannyParam = cannyParameter;
                                _filteredImage.angle = angle;
                            }
                        }
                    }
                }
            }

            return _filteredImage.image.Clone();
        }

        /// <summary>
        /// Public implementation of Dispose pattern callable by consumers.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Fire An Image Event
        /// </summary>
        /// <param name="image">Image To Event</param>
        protected static void FireImageEvent(object sender, Guid cardId, Guid imageId,
            ImageType imageType,
            Mat image,
            double angle,
            float X,
            float Y,
            CannyParam cannyParameter = null,
            ThresholdParm thresholdParmeter = null)
        {
            ImageEventArgs eventArgs = new ImageEventArgs(cardId, imageId, imageType, image, angle, X, Y,
                cannyParameter: cannyParameter, thresholdParamter: thresholdParmeter);

            if (ImageEvent != null)
            {
                ImageEvent(sender: sender, data: eventArgs);
            }
        }

        /// <summary>
        /// Fire the Image Event And Send a Contoured Image
        /// </summary>
        protected void FireImageEvent(object sender, Guid cardId, Guid imageId, ImageType imageType,
            double angle, float X, float Y, CannyParam cannyParameter = null, ThresholdParm thresholdParmeter = null)
        {
            if (ImageEvent != null)
            {
                using (var image = GetContouredImage(angle, cannyParameter))
                {
                    Image.FireImageEvent(null, cardId, imageId, imageType, image, angle, X, Y, cannyParameter);
                }
            }
        }

        /// <summary>
        /// Draw All the Contours On An Image
        /// </summary>
        internal void FireImageEvent(object sender, Guid cardId, Guid imageId, ImageType imageType, double angle, float X, float Y,
            VectorOfVectorOfPoint contours, CannyParam cannyParameter = null, Func<Size, Rectangle, bool> contourFilter = null)
        {
            if (ImageEvent != null)
            {
                using (Mat rotatedImage = GetRotatedImage(angle))
                {
                    for (int i = 0; i < contours.Size; i++)
                    {
                        RotatedRect rotatedRect = CvInvoke.MinAreaRect(contours[i]);
                        Rectangle box = rotatedRect.MinAreaRect();

                        if (contourFilter == null || (contourFilter != null && contourFilter(rotatedImage.Size, box)))
                        {
                            CvInvoke.DrawContours(rotatedImage, contours, i, yellow);
                            CvInvoke.Rectangle(rotatedImage, box, green, thickness: 2);

                            //if (tagged)
                            //{
                            //    CvInvoke.PutText(rotatedImage, string.Format("HR: {0:0.00} AR: {0:0.00}", heightRatio, aspectRatio),
                            //        new Point((int)box.X + (box.Width / 2), (int)box.Y + (box.Height / 2)),
                            //        FontFace.HersheyPlain, fontScale: 1, color: black, thickness: 2);
                            //}
                        }
                    }

                    Image.FireImageEvent(null, cardId, imageId, imageType, rotatedImage, angle, X, Y, cannyParameter: cannyParameter);
                }
            }
        }

        /// <summary>
        /// Fire Image Event With Rectangles Drawn On Image
        /// </summary>
        internal void FireImageEvent(object sender, Guid cardId, Guid imageId, ImageType imageType, double angle, float X, float Y,
            IEnumerable<Rectangle> rectangles, MCvScalar color, int thickness = 1, CannyParam cannyParameter = null, Action<Mat> postFunc = null)
        {
            if (ImageEvent != null)
            {
                using (Mat rotatedImage = GetRotatedImage(angle))
                {
                    foreach (Rectangle rectangle in rectangles)
                    {
                        CvInvoke.Rectangle(rotatedImage, rectangle, color, thickness: thickness);
                    }

                    if (postFunc != null)
                    {
                        postFunc(rotatedImage);
                    }



                    Image.FireImageEvent(null, cardId, imageId, imageType, rotatedImage, angle, X, Y, cannyParameter: cannyParameter);
                }
            }
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
                DisposeImages();
            }

            // Free any unmanaged objects here.
            //

            _disposed = true;
        }

        /// <summary>
        /// Dispose Of All Images
        /// </summary>
        protected virtual void DisposeImages()
        {
            if (_image != null)
            {
                _image.Dispose();
                _image = null;
            }

            if (_rotatedImage.image == null)
            {
                _rotatedImage.image.Dispose();
                _rotatedImage.image = null;
            }

            if (_grayImage.image != null)
            {
                _grayImage.image.Dispose();
                _grayImage.image = null;
            }

            if (_cannyImage.image != null)
            {
                _cannyImage.image.Dispose();
                _cannyImage.image = null;
            }

            if (_contouredImage.image != null)
            {
                _contouredImage.image.Dispose();
                _contouredImage.image = null;
            }

            if (_thresholdImage.image != null)
            {
                _thresholdImage.image.Dispose();
                _thresholdImage.image = null;
            }

            if (_filteredImage.image != null)
            {
                _filteredImage.image.Dispose();
                _filteredImage.image = null;
            }
        }



        protected struct CachedThresoldImage
        {
            internal Mat image;
            internal double angle;
            internal ThresholdParm thresholdParm;
        }

        protected struct CachedImage
        {
            internal Mat image;
            internal double angle;
            internal CannyParam cannyParam;
        }
    }
}
