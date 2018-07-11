using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using CardModel;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;

namespace CardImaging
{
    public class CardImage : Image
    {
        /// <summary>
        /// Minimum Aspect Ratio of Card
        /// </summary>
        private const double MinTitleAspectRatio = .84;

        /// <summary>
        /// Maximum Aspect Ratio Of Card
        /// </summary>
        private const double MaxTitleAspectRatio = 1.0;

        /// <summary>
        /// Unique Card Id For This Image
        /// </summary>
        private Guid _cardId;

        /// <summary>
        /// Angle for the card to appear right side up, i.e. the title is readable
        /// </summary>
        private double _angle;

        /// <summary>
        /// Cached Title Image
        /// </summary>
        private CardTitleImage _cardTitleImage;

        /// <summary>
        /// Card Frame
        /// </summary>
        private MTGCardFrame _cardFrame;

        /// <summary>
        /// Test Angles To Pivot Card
        /// </summary>
        internal static Func<IEnumerable<double>> Angles = new Func<IEnumerable<double>>(() =>
        {
            // Cards might be rotated just slight because of photo schew, check angles just off of 0 and 180
            return new double[] { 0.0, 180.0, 359.5, .5, 180.5, 179.5, 359.0, 1.0, 181.0, 179.0 };
        });

        /// <summary>
        /// Test Angles To Pivot Card
        /// </summary>
        internal static Func<IEnumerable<double>> IconAngles = new Func<IEnumerable<double>>(() =>
        {
            return new double[] { 0.0 };
        });

        /// <summary>
        /// Test Set Used To Look For Contours Inside the Card Image
        /// </summary>
        internal static Func<IEnumerable<CannyParam>> CannyParameters = new Func<IEnumerable<CannyParam>>(() =>
        {
            List<CannyParam> result = new List<CannyParam>();

            double[,] thresholds = new double[,] { { 200, 100 }, { 100, 50 }, { 150, 75 }, { 250, 125 } };

            for (int thresholdIndex = 0; thresholdIndex < thresholds.GetLength(0); thresholdIndex++)
            {
                result.Add(new CannyParam(thresholds[thresholdIndex, 0], thresholds[thresholdIndex, 1], true, 3));
            }

            return result;
        });

        /// <summary>
        /// Test Set Used To Look For Contours Inside the Card Image
        /// </summary>
        internal static Func<IEnumerable<CannyParam>> CannyIconParameters = new Func<IEnumerable<CannyParam>>(() =>
        {
            List<CannyParam> result = new List<CannyParam>();

            double[,] thresholds = new double[,] { { 250, 125 } };

            for (int thresholdIndex = 0; thresholdIndex < thresholds.GetLength(0); thresholdIndex++)
            {
                result.Add(new CannyParam(thresholds[thresholdIndex, 0], thresholds[thresholdIndex, 1], l2Graident: false, aperture: 3));
            }

            return result;
        });

        /// <summary>
        /// Initilize a new instance of the CardImage class
        /// </summary>
        public CardImage(Guid cardId, Mat image, double angle, MTGCardFrame cardFrame) : base(image)
        {
            _cardId = cardId;
            _angle = angle;
            _cardFrame = cardFrame;
        }

        /// <summary>
        /// Initilize a new instance of the CardImage class
        /// </summary>
        /// <param name="image">Card Image</param>
        /// <param name="cardTitleImage">Card Title Image (Sub Image of image)</param>
        public CardImage(Guid cardId, Mat image, CardTitleImage cardTitleImage) : base(image)
        {
            _cardId = cardId;
            _cardTitleImage = cardTitleImage;
            _angle = cardTitleImage.Angle;
            _cardFrame = cardTitleImage.CardFrame;
        }

        /// <summary>
        /// Angle for the card to appear right side up, i.e. the title is readable
        /// </summary>
        internal double Angle
        {
            get
            {
                return _angle;
            }
        }

        /// <summary>
        /// Get the Image Right Side Up, i.e. Readable
        /// </summary>
        public Mat GetRotatedImage()
        {
            return GetRotatedImage(angle: _angle);
        }

        /// <summary>
        /// Get the Gray Image Right Side Up, i.e. Readable
        /// </summary>
        public Mat GetGreyImage()
        {
            return GetGreyImage(angle: _angle);
        }

        public CardTitleImage GetCardTitleImage()
        {
            return _cardTitleImage;
        }

        /// <summary>
        /// Get the Name Of the Card In This Image
        /// </summary>
        public string GetName()
        {
            CardTitleImage cardTitleImage = GetCardTitleImage();
            return cardTitleImage == null ? null : cardTitleImage.Name;
        }

        /// <summary>
        /// The Sureness that the name matches the image.
        /// </summary>
        public double GetNameSurity()
        {
            CardTitleImage cardTitleImage = GetCardTitleImage();
            return cardTitleImage == null ? 0 : cardTitleImage.Surity;
        }

        /// <summary>
        /// Search the Card Image For The Title And Result It As a Wrapped Class (CardTitleImage)
        /// </summary>
        internal static CardTitleImage FindBestTitleImage(Guid cardId, Image cardImage, float X, float Y, IEnumerable<CannyParam> cannyParameters, IEnumerable<double> angles)
        {
            List<CardTitleImage> potentialCardTitleImages = new List<CardTitleImage>();

            ContourCache contourCache = new ContourCache();

            using (var contours = new VectorOfVectorOfPoint())
            {
                foreach (var angle in angles)
                {
                    foreach (var cannyParameter in cannyParameters)
                    {
                        Debug.WriteLine(string.Format("Find Title Image Canny {0} Angle {1}...", cannyParameter, angle));

                        using (Mat cannyImage = cardImage.GetCannyImage(() => { return cardImage.GetGreyImage(angle); }, angle, cannyParameter))
                        {
                            CvInvoke.FindContours(cannyImage, contours, hierarchy: null, mode: RetrType.List, method: ChainApproxMethod.ChainApproxNone);

                            var sortedContours = new List<VectorOfPoint>();
                            for (int idx = 0; idx < contours.Size; idx++)
                            {
                                sortedContours.Add(contours[idx]);
                            }

                            double optimalAspectRatio = ((MaxTitleAspectRatio - MinTitleAspectRatio) / 2.0) + MinTitleAspectRatio;

                            // Sort Each Contour By Delta From the Optiomal Aspect Ratio, Smallest First
                            sortedContours.Sort((c1, c2) =>
                            {
                                RotatedRect rotatedRect1 = CvInvoke.MinAreaRect(c1);
                                double aspectRatio1 = (double)(rotatedRect1.Size.Height / rotatedRect1.Size.Width);

                                RotatedRect rotatedRect2 = CvInvoke.MinAreaRect(c2);
                                double aspectRatio2 = (double)(rotatedRect2.Size.Height / rotatedRect2.Size.Width);

                                double diff1 = Math.Abs(aspectRatio1 - optimalAspectRatio);
                                double diff2 = Math.Abs(aspectRatio2 - optimalAspectRatio);

                                return diff1.CompareTo(diff2);
                            });

                            using (Mat rotatedImage = cardImage.GetRotatedImage(angle))
                            {
                                cardImage.FireImageEvent(null, cardId, cardId, ImageType.CardContoured, angle: angle, X: X, Y: Y,
                                    contours: contours, cannyParameter: cannyParameter);

                                // Find The Contour That Matchs What A Title Should Look Like
                                foreach (VectorOfPoint countour in sortedContours)
                                {
                                    // Keep a list of contours that have been tested and don't test those again
                                    // that allows us to go through a bunch of test thresholds to determine
                                    // if they will yield new contours.
                                    if (contourCache.Contains(countour, angle))
                                    {
                                        continue;
                                    }

                                    // Store all the rotated rects that have been examined
                                    contourCache.Add(countour, angle);

                                    Mat result = null;
                                    MTGCardFrame cardTitleType;

                                    try
                                    {
                                        Guid cardTitleId = Guid.NewGuid();

                                        if (TryFindTitle(cardId, cardTitleId, rotatedImage, angle, cannyParameter, countour, result: out result, cardTitleType: out cardTitleType))
                                        {
                                            // Wrap the Mat Found
                                            Image image = new Image(result);

                                            // Get the MTG card with the minimum Levenshtein distance for the text parsed
                                            // from the image, this will be the closet MTG card to the image
                                            var levenshteinDistanceResults = CardTitleImage.GetLowLevenshteinDistanceCards(cardId, cardTitleId, X, Y, cardTitleType, image);

                                            // Create a class from the card title image, the card title type, and the minimum levenshtien distance cards
                                            CardTitleImage cardTitleImage = new CardTitleImage(cardId, cardTitleId, result, cardTitleType, levenshteinDistanceResults, angle);

                                            // Direct Match Short Circuit
                                            if (levenshteinDistanceResults.Any(ldr => ldr.Distance < LevenshteinDistanceShortCircuit))
                                            {
                                                return cardTitleImage;
                                            }

                                            // Add it to the potential card titles
                                            potentialCardTitleImages.Add(cardTitleImage);
                                        }
                                    }
                                    finally
                                    {
                                        if (result != null)
                                        {
                                            result.Dispose();
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (potentialCardTitleImages.Count == 0)
            {
                return null;
            }

            // Find The minimum number of character changes (the result of the Levenshtein Distance) alogorithm 
            double minLevenshteinResults = potentialCardTitleImages.Min(cti => cti.LevenshteinResults.Min(lr => lr.PercentDistance));

            // Find all unique cards that have this distance
            var potentialCardNames = potentialCardTitleImages.SelectMany(cti => cti.LevenshteinResults)
                .Where(lr => lr.PercentDistance == minLevenshteinResults)
                .Select(lr => lr.Card.Name)
                .Distinct()
                .ToArray();

            if (potentialCardNames.Length == 0)
            {
                // There are no cards that match the image
                return null;
            }
            else if (potentialCardNames.Length > 1)
            {
                // Inconclusive Results
                // There is more than one card with a low Levenshtein score that matches the images presented.
                return null;
            }
            else
            {
                // Winner Winner Chicken Dinner
                string name = potentialCardNames[0];
                return potentialCardTitleImages.FirstOrDefault(cti => cti.LevenshteinResults.Any(lr => lr.Card.Name.Equals(name)));
            }
        }

        public IEnumerable<Mat> FindSetIcons()
        {
            using (Mat image = GetGreyImage())
            {
                Rectangle rectangle = CardExpansionSymbolFilter.BoundingRectangle(_cardFrame, image.Size);

                using (Mat cropped = new Mat(image, rectangle))
                {
                    // Output cropped image for debugging.
                    Image.FireImageEvent(this, _cardId, _cardId, ImageType.SetIcon,
                        cropped, angle: 0, X: rectangle.X + (rectangle.Width / 2), Y: rectangle.Y + (rectangle.Height / 2),
                        cannyParameter: null);

                   yield return cropped.Clone();
                }
            }
        }

        //public IEnumerable<Mat> FindSetIcons()
        //{
        //    List<Mat> results = new List<Mat>();

        //    using (var contours = new VectorOfVectorOfPoint())
        //    {
        //        foreach (var angle in CardImage.IconAngles())
        //        {
        //            foreach (var cannyParameter in CardImage.CannyIconParameters())
        //            {
        //                Debug.WriteLine("Find Set Icon Image Angle: {0} Canny: {1} ...", angle, cannyParameter);

        //                using (Mat cannyImage = GetCannyImage(() => { return GetGreyImage(angle); }, angle, cannyParameter))
        //                {
        //                    // Find All the Contours
        //                    CvInvoke.FindContours(cannyImage, contours, hierarchy: null, mode: RetrType.List, method: ChainApproxMethod.ChainApproxNone);

        //                    List<Rectangle> rectangleList = new List<Rectangle>();

        //                    // Create a List of Rectangles From the Contours
        //                    for (int idx = 0; idx < contours.Size; idx++)
        //                    {
        //                        Rectangle rectangle = CvInvoke.MinAreaRect(contours[idx]).MinAreaRect();
        //                        rectangleList.Add(rectangle);
        //                    }

        //                    // Only add those which fall in the Icon "Zone"
        //                    rectangleList = rectangleList.Where(r => CardExpansionSymbolFilter.ExpansionSymbolFilterPass1(_cardFrame, cannyImage.Size, r)).ToList();

        //                    // Output Contoured Image For Debugging
        //                    this.FireImageEvent(null, _cardId, _cardId, ImageType.CardContoured, angle: angle, X: 0, Y: 0,
        //                        rectangles: rectangleList, color: blue, thickness: 1, cannyParameter: cannyParameter, postFunc: new Action<Mat>((image) =>
        //                        {
        //                            // After Drawing the Rectangles, Add The Margin Lines
        //                            CvInvoke.Line(image, new Point((int)(image.Width * CardExpansionSymbolFilter.LeftMargin[_cardFrame]), 0),
        //                                new Point((int)(image.Width * CardExpansionSymbolFilter.LeftMargin[_cardFrame]), image.Height), white, thickness: 1);

        //                            CvInvoke.Line(image, new Point((int)(image.Width * CardExpansionSymbolFilter.RightMargin[_cardFrame]), 0),
        //                                new Point((int)(image.Width * CardExpansionSymbolFilter.RightMargin[_cardFrame]), image.Height), white, thickness: 1);

        //                            CvInvoke.Line(image, new Point(0, (int)(image.Height * CardExpansionSymbolFilter.TopMargin[_cardFrame])),
        //                                new Point(image.Width, (int)(image.Height * CardExpansionSymbolFilter.TopMargin[_cardFrame])), white, thickness: 1);

        //                            CvInvoke.Line(image, new Point(0, (int)(image.Height * CardExpansionSymbolFilter.BottomMargin[_cardFrame])),
        //                                new Point(image.Width, (int)(image.Height * CardExpansionSymbolFilter.BottomMargin[_cardFrame])), white, thickness: 1);

        //                        }));

        //                    // Merge all possible rectangles that could be icons together
        //                    var intersectedRectangles = MergedRectangles(rectangleList.ToArray())
        //                        .Distinct()
        //                        .Except(rectangleList);

        //                    rectangleList.AddRange(intersectedRectangles);

        //                    // Make a second pass at filtering to reduce the rectangles into only those
        //                    // that could possibly be icons based on aspect ratio, size, etc...
        //                    rectangleList = rectangleList.Where(r => CardExpansionSymbolFilter.ExpansionSymbolFilterPass2(_cardFrame, cannyImage.Size, r)).ToList();

        //                    // Sort Each Rectangle From Biggest To Smallest
        //                    rectangleList.Sort((r1, r2) =>
        //                    {
        //                        float area1 = r1.Width * r1.Height;
        //                        float area2 = r2.Width * r2.Height;

        //                        return area2.CompareTo(area1);
        //                    });

        //                    // Skip mat creation if there are no rectangles
        //                    if (rectangleList.Count > 0)
        //                    {
        //                        using (Mat image = GetGreyImage(angle))
        //                        {
        //                            foreach (Rectangle rectangle in rectangleList)
        //                            {
        //                                using (Mat cropped = new Mat(image, rectangle))
        //                                {
        //                                    // Output cropped image for debugging.
        //                                    Image.FireImageEvent(this, _cardId, _cardId, ImageType.SetIcon,
        //                                        cropped, angle: angle, X: rectangle.X + (rectangle.Width / 2), Y: rectangle.Y + (rectangle.Height / 2),
        //                                        cannyParameter: cannyParameter);

        //                                    results.Add(cropped.Clone());
        //                                }
        //                            }
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //    }

        //    // Sort to biggest first, this make sure FirstDefault() take the most likely one
        //    results.Sort((m1, m2) =>
        //    {
        //        float area1 = m1.Size.Height * m1.Size.Width;
        //        float area2 = m2.Size.Height * m2.Size.Width;

        //        return area2.CompareTo(area1);
        //    });

        //    return results;
        //}

        /// <summary>
        /// Try To Find The Title In the Image
        /// </summary>
        /// <param name="cardImage">Image</param>
        /// <param name="countor">Contour To Test</param>
        /// <param name="result">Resulting Title Image</param>
        /// <returns>True If Title Is Found</returns>
        private static bool TryFindTitle(Guid cardId, Guid cardTitleId,
            Mat cardImage, double angle, CannyParam cannyParameter,
            VectorOfPoint countor, out Mat result, out MTGCardFrame cardTitleType)
        {
            result = null;
            cardTitleType = MTGCardFrame.M15;

            RotatedRect rotatedRect = CvInvoke.MinAreaRect(countor);

            // Prevent Divide By Zero
            if (rotatedRect.Size.Height == 0)
            {
                return false;
            }

            float width = rotatedRect.Size.Width;
            float height = rotatedRect.Size.Height;
            float heightRatio = rotatedRect.Size.Height / (float)cardImage.Size.Height;
            float relativeCenterY = rotatedRect.Center.Y / (float)cardImage.Size.Height;

            Rectangle box = rotatedRect.MinAreaRect();

            // Prevent Divide By Zero
            if (box.Size.Width == 0)
            {
                return false;
            }

            float widthRatio = (float)box.Size.Width / (float)cardImage.Size.Width;
            float aspectRatio = (float)box.Size.Height / (float)box.Size.Width;
            float area = (float)box.Size.Height * (float)box.Size.Width;
            float imageArea = (float)cardImage.Size.Height * (float)cardImage.Size.Width;
            float relativeArea = area / imageArea;

            // Title bar should have a height
            if (height < 1.0F)
            {
                return false;
            }

            // Box Should Be Inside the Image
            if ((box.Y < 0) || (box.X < 0) || ((box.X + box.Width) > cardImage.Size.Width) || ((box.Y + box.Height) > cardImage.Size.Height))
            {
                return false;
            }


            // Name bar should center in the top 15% of the image, this is the new style cards with the name "boxed" in the image
            if (relativeCenterY < .15F)
            {
                // Title Bar Should Be Wider Than 80% of the Image Width
                if (widthRatio < .80F)
                {
                    return false;
                }

                using (Mat cropped = new Mat(cardImage, box))
                {
                    Image.FireImageEvent(null, cardId, cardId, ImageType.TitleCropped, cropped, angle: angle, X: box.X, Y: box.Y, cannyParameter: cannyParameter);
                }

                // Title Bar Should Be 6% of the Card Height
                if (heightRatio < .048 || heightRatio > .077)
                {
                    return false;
                }

                Debug.WriteLine("Title Contour ({14}) - Center: {0}/{1} Relative Center: ({9}%)/({10}%) Width: {2} ({11}%) Height: {3} ({12}%) Area: {4} ({13}%) : AspectRatio: {5}, Angle: {6} Image Size: {7}/{8}",
                    rotatedRect.Center.X, rotatedRect.Center.Y,
                    rotatedRect.Size.Width, rotatedRect.Size.Height,
                    area, aspectRatio, rotatedRect.Angle,
                    cardImage.Size.Width, cardImage.Size.Height,
                    (rotatedRect.Center.X / cardImage.Size.Width) * 100.0,
                    relativeCenterY * 100.0,
                    widthRatio * 100.0,
                    heightRatio * 100.0,
                    relativeArea * 100.0,
                    cardTitleId);

                using (Mat cropped = new Mat(cardImage, box))
                {
                    Image.FireImageEvent(null, cardId, cardTitleId, ImageType.TitleCropped, cropped, angle: angle, X: box.X, Y: box.Y, cannyParameter: cannyParameter);

                    result = cropped.Clone();
                    return true;
                }
            }
            else if (relativeCenterY < .50F)
            {
                // Assume that this card is the older style card with the name above the photo, but not "boxed"
                //
                cardTitleType = MTGCardFrame.Original;

                // Using the Aspect Ratio Find the Art Box
                if (aspectRatio < .75F || aspectRatio > .85F)
                {
                    return false;
                }

                // The art relative area to the card should be above 35%
                if (relativeArea < .35F)
                {
                    return false;
                }

                Debug.WriteLine("Title Contour ({14}) - Center: {0}/{1} Relative Center: ({9}%)/({10}%) Width: {2} ({11}%) Height: {3} ({12}%) Area: {4} ({13}%) : AspectRatio: {5}, Angle: {6} Image Size: {7}/{8}",
                    rotatedRect.Center.X, rotatedRect.Center.Y,
                    rotatedRect.Size.Width, rotatedRect.Size.Height,
                    area, aspectRatio, rotatedRect.Angle,
                    cardImage.Size.Width, cardImage.Size.Height,
                    (rotatedRect.Center.X / cardImage.Size.Width) * 100.0,
                    relativeCenterY * 100.0,
                    widthRatio * 100.0,
                    heightRatio * 100.0,
                    relativeArea * 100.0,
                    cardTitleId);

                int borderHeight = (int)((double)box.Y * .45);

                // Create a box that is as wide as the art, and directly above the art to 
                // the top of the card
                Rectangle titleBox = new Rectangle(box.X, borderHeight, box.Width, box.Y - borderHeight);

                heightRatio = titleBox.Size.Height / (float)cardImage.Size.Height;

                // Title Bar Should Be 6% of the Card Height
                if (heightRatio < .050 || heightRatio > .065)
                {
                    return false;
                }

                using (Mat cropped = new Mat(cardImage, titleBox))
                {
                    Image.FireImageEvent(null, cardId, cardId, ImageType.TitleCropped, cropped, angle: angle, X: titleBox.X, Y: titleBox.Y, cannyParameter: cannyParameter);

                    result = cropped.Clone();
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Merge all rectangles that could be icons together, sometimes icons are made of two contours
        /// </summary>
        private static IEnumerable<Rectangle> MergedRectangles(IEnumerable<Rectangle> input)
        {
            foreach (var r1 in input)
            {
                foreach (var r2 in MergedRectangles(input, r1))
                {
                    yield return r2;
                }
            }
        }

        private static IEnumerable<Rectangle> MergedRectangles(IEnumerable<Rectangle> input, Rectangle r1)
        {
            List<Rectangle> result = new List<Rectangle>();

            foreach (var r2 in input)
            {
                if (r2.Equals(r1))
                {
                    continue;
                }

                int X1 = Math.Min(r2.X, r1.X);
                int X2 = Math.Max(r2.X + r2.Width, r1.X + r1.Width);
                int Y1 = Math.Min(r2.Y, r1.Y);
                int Y2 = Math.Max(r2.Y + r2.Height, r1.Y + r1.Height);

                var ri = new Rectangle(X1, Y1, X2 - X1, Y2 - Y1);

                if (ri.Equals(r2) || ri.Equals(r1))
                {
                    continue;
                }

                result.Add(ri);

                if (!result.Contains(ri))
                {
                    foreach (var rp in MergedRectangles(input, ri))
                    {
                        result.Add(rp);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Dipose Of Images
        /// </summary>
        protected override void DisposeImages()
        {
            base.DisposeImages();

            if (_cardTitleImage != null)
            {
                _cardTitleImage.Dispose();
                _cardTitleImage = null;
            }
        }
    }
}
