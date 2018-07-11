using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;

namespace CardImaging
{
    /// <summary>
    /// A wrapper classes around an image that can find cards in the image
    /// </summary>
    public class CardField : Image
    {
        /// <summary>
        /// Minimum Aspect Ratio of Card
        /// </summary>
        private const double MinCardAspectRatio = .70;

        /// <summary>
        /// Maximum Aspect Ratio Of Card
        /// </summary>
        private const double MaxCardAspectRatio = .81;

        /// <summary>
        /// Unique Identifier For the Field Photo
        /// </summary>
        private Guid _fieldId;

        /// <summary>
        /// Initialize a new instance of the Card Field class with the image.
        /// </summary>
        /// <param name="image"></param>
        public CardField(Mat image) : base(image)
        {
            _fieldId = Guid.NewGuid();
        }

        /// <summary>
        /// Find All Cards In Field
        /// </summary>
        /// <returns>List of Cards</returns>
        public IEnumerable<CardImage> FindCards(Guid cardId)
        {
            ContourCache contourCache = new ContourCache();

            // Before contouring field image for cards, try using the whole field image
            // assuming it is a card
            using (Mat image = GetImage())
            {
                float aspectRatio = (float)image.Width / (float)image.Height;

                // Find the Card Aspect Raito
                if ((aspectRatio >= MinCardAspectRatio) && (aspectRatio <= MaxCardAspectRatio))
                {
                    Image.FireImageEvent(null, cardId, cardId, ImageType.CardCropped, image, angle: 0,
                        X: image.Size.Width / 2.0F, Y: image.Size.Height / 2.0F);

                    var cardTitleImage = CardImage.FindBestTitleImage(cardId, this,
                        X: image.Size.Width / 2.0F, Y: image.Size.Height / 2.0F,
                        cannyParameters: CardImage.CannyParameters(), angles: CardImage.Angles());

                    if (cardTitleImage != null)
                    {
                        var cardImage = new CardImage(cardId, image, cardTitleImage);
                        yield return cardImage;
                    }
                }
            }

            // Iterate though canny parameters looking for contours that will yield cards
            foreach (var cannyParameter in GetCannyParameters())
            {
                FireImageEvent(this, cardId, _fieldId, ImageType.FieldContoured, angle: 0, X: 0, Y: 0, cannyParameter: cannyParameter);

                Debug.WriteLine("Find Card Image: {0}...", cannyParameter);

                using (Mat cannyImage = GetCannyImage(() => { return GetGreyImage(angle: 0); }, angle: 0, cannyParameter: cannyParameter))
                {
                    using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
                    {
                        CvInvoke.FindContours(cannyImage, contours, hierarchy: null, mode: retrType, method: chainApproxMethod);

                        var sortedContours = new List<VectorOfPoint>();
                        for (int idx = 0; idx < contours.Size; idx++)
                        {
                            sortedContours.Add(contours[idx]);
                        }

                        // Sort Each Contour By Area, Largest First
                        sortedContours.Sort((v1, v2) =>
                                                {
                                                    RotatedRect rotatedRect1 = CvInvoke.MinAreaRect(v1);
                                                    float area1 = rotatedRect1.Size.Width * rotatedRect1.Size.Height;

                                                    RotatedRect rotatedRect2 = CvInvoke.MinAreaRect(v2);
                                                    float area2 = rotatedRect2.Size.Width * rotatedRect2.Size.Height;

                                                    return area2.CompareTo(area1);
                                                });

                        // Iterate Each Contour Trying To Determine If It Is a Card
                        foreach (VectorOfPoint countour in sortedContours)
                        {
                            // Keep a list of contours that have been tested and don't test those again
                            // that allows us to quickly go through a bunch of test thresholds to determine
                            // if they will yield new contours.
                            if (contourCache.Contains(countour, angle: 0))
                            {
                                continue;
                            }

                            // Store all the rotated rects that have been examined
                            contourCache.Add(countour, angle: 0);

                            Mat imageResult = null;

                            try
                            {
                                if (TryFindCard(cardId, this.Size, countour, result: out imageResult))
                                {
                                    RotatedRect rotatedRect1 = CvInvoke.MinAreaRect(countour);

                                    Image.FireImageEvent(null, cardId, cardId, ImageType.CardCropped, imageResult, angle: 0,
                                                                            X: rotatedRect1.Center.X, Y: rotatedRect1.Center.Y, cannyParameter: cannyParameter);

                                    // The card image (result) is in portrait mode, however it could be upsidedown
                                    using (Image image = new Image(imageResult))
                                    {
                                        var cardTitleImage = CardImage.FindBestTitleImage(cardId, image,
                                            X: rotatedRect1.Center.X, Y: rotatedRect1.Center.Y,
                                            cannyParameters: CardImage.CannyParameters(), angles: CardImage.Angles());

                                        if (cardTitleImage != null)
                                        {
                                            var cardImage = new CardImage(cardId, imageResult, cardTitleImage);
                                            yield return cardImage;
                                        }
                                    }
                                }
                            }
                            finally
                            {
                                if (imageResult != null)
                                {
                                    imageResult.Dispose();
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Determine if the countor represents a card.
        /// </summary>
        public bool TryFindCard(Guid cardId, Size fieldImageSize, VectorOfPoint countor, out Mat result)
        {
            result = null;

            RotatedRect rotatedRect = CvInvoke.MinAreaRect(countor);

            // Prevent Divide By Zero
            if (rotatedRect.Size.Width == 0)
            {
                return false;
            }

            float angle = 0F;
            float width = rotatedRect.Size.Width;
            float height = rotatedRect.Size.Height;
            float area = width * height;
            float heightRatio = rotatedRect.Size.Height / (float)fieldImageSize.Height;
            float widthRatio = rotatedRect.Size.Width / (float)fieldImageSize.Width;
            float relativeCenterX = rotatedRect.Center.X / (float)fieldImageSize.Width;
            float relativeCenterY = rotatedRect.Center.Y / (float)fieldImageSize.Height;

            Rectangle box = rotatedRect.MinAreaRect();
            float boxAspectRatio = box.Size.Width > box.Size.Height ? (float)box.Size.Height / (float)box.Size.Width : (float)box.Size.Width / (float)box.Size.Height;

            // Prevent Divide By Zero
            if ((rotatedRect.Size.Height == 0) || (rotatedRect.Size.Width == 0))
            {
                return false;
            }

            float aspectRatio = (float)width / (float)height;

            // Rotate card if it is on it's side
            if (width > height)
            {
                aspectRatio = (float)height / (float)width;
                angle = -90.0F;
            }

            // Card should have a height
            if (height < 1.0F)
            {
                return false;
            }

            // Too small to parse
            if (height < 500.0F)
            {
                return false;
            }

            // Too small to parse
            if (width < 500.0F)
            {
                return false;
            }

            Debug.WriteLine("Potential Card Contour - Center: {0}/{1} Relative Center: ({9:0.00}%)/({10:0.00}%) Width: {2} ({11:0.00}%) Height: {3} ({12:0.00}%) Area: {4} : AspectRatio: {5}, Angle: {6} Image Size: {7}/{8}",
                rotatedRect.Center.X, rotatedRect.Center.Y,
                rotatedRect.Size.Width, rotatedRect.Size.Height,
                area, aspectRatio, rotatedRect.Angle,
                fieldImageSize.Width, fieldImageSize.Height,
                relativeCenterX * 100.0F,
                relativeCenterY * 100.0F,
                widthRatio * 100.0F,
                heightRatio * 100.0F);

            // Find the Card Aspect Raito
            if (aspectRatio < MinCardAspectRatio || aspectRatio > MaxCardAspectRatio)
            {
                return false;
            }

            using (Mat image = GetImage())
            {
                using (Mat rot_mat = new RotationMatrix2D(rotatedRect.Center, rotatedRect.Angle + angle, 1.0))
                {
                    using (Mat rotated = new Mat())
                    {
                        // Rotate
                        CvInvoke.WarpAffine(image, rotated, rot_mat, image.Size, interpMethod: Inter.Cubic);

                        // Adjust For Rotation
                        Size size;
                        if (rotatedRect.Angle + angle < -90)
                        {
                            size = new Size((int)rotatedRect.Size.Height, (int)rotatedRect.Size.Width);
                        }
                        else
                        {
                            size = new Size((int)rotatedRect.Size.Width, (int)rotatedRect.Size.Height);
                        }

                        using (Mat cropped = new Mat())
                        {
                            CvInvoke.GetRectSubPix(rotated, size, rotatedRect.Center, cropped);
                            result = cropped.Clone();
                            return true;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Get the test canny parameters for searching the image
        /// </summary>
        internal IEnumerable<CannyParam> GetCannyParameters()
        {
            // Thresolds To Search
            double[,] _thresholds = new double[,] { { 250.0, 125.0 }, { 170.0, 170.0 / 2 }, { 200.0, 100 }, { 100, 50 }, { 50, 0 } };

            // Apertures To Try
            int[] apertures = new int[] { 3 };

            foreach (int aperture in apertures)
            {
                for (int thresholdIndex = 0; thresholdIndex < _thresholds.GetLength(0); thresholdIndex++)
                {
                    yield return new CannyParam(_thresholds[thresholdIndex, 0], _thresholds[thresholdIndex, 1], true, aperture);
                    yield return new CannyParam(_thresholds[thresholdIndex, 0], _thresholds[thresholdIndex, 1], false, aperture);
                }
            }
        }
    }
}
