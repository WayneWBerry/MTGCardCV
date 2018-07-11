using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using CardModel;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Tesseract;

namespace CardImaging
{
    public class CardTitleImage : Image
    {
        private const RetrType retrType = RetrType.List;

        private const ChainApproxMethod chainApproxMethod = ChainApproxMethod.ChainApproxNone;

        /// <summary>
        /// The OCR engine
        /// </summary>
        private static TesseractEngine _tesseractEngine;

        /// <summary>
        /// Unique card id to which this card belongs
        /// </summary>
        private Guid _cardId;

        /// <summary>
        /// Unique Card Title Id To This Title
        /// </summary>
        private Guid _cardTitleId;

        /// <summary>
        /// Card Frame
        /// </summary>
        private MTGCardFrame _cardFrame;

        /// <summary>
        /// Angle for the card to appear right side up, i.e. the title is readable
        /// </summary>
        private double _angle;

        /// <summary>
        /// Cached Card Text
        /// </summary>
        private string _name;

        /// <summary>
        /// Levenshtein Results
        /// </summary>
        private LevenshteinResults[] _levenshteinResults;

        static CardTitleImage()
        {
            _tesseractEngine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default);
        }

        /// <summary>
        /// Initilize a new instance of the CardTitleImage class
        /// </summary>
        /// <param name="image">Image</param>
        internal CardTitleImage(Guid cardId, Guid cardTitleId, Mat image, MTGCardFrame cardTitleType, IEnumerable<LevenshteinResults> levenshteinResults, double angle) : base(image)
        {
            _cardId = cardId;
            _cardTitleId = cardTitleId;
            _cardFrame = cardTitleType;
            _angle = angle;
            _levenshteinResults = levenshteinResults.ToArray();
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
        /// Card Frame
        /// </summary>
        internal MTGCardFrame CardFrame
        {
            get
            {
                return _cardFrame;
            }
        }

        /// <summary>
        /// Card Name That Represents the Image
        /// </summary>
        internal string Name
        {
            get
            {
                var uniqueNames = _levenshteinResults.Select(lr => lr.Card.Name).Distinct().ToArray();

                // Inconclusive
                if (uniqueNames.Length != 1)
                {
                    return null;
                }

                return uniqueNames.First();
            }
        }

        /// <summary>
        /// The Sureness that the name matches the image.
        /// </summary>
        internal double Surity
        {
            get
            {
                var uniqueNames = _levenshteinResults.Select(lr => lr.Card.Name).Distinct().ToArray();

                // Inconclusive
                if (uniqueNames.Length != 1)
                {
                    return 0;
                }

                var min = _levenshteinResults.Min(lr => lr.PercentDistance);

                return (1.0 - min) * 100;
            }
        }

        internal IEnumerable<LevenshteinResults> LevenshteinResults
        {
            get
            {
                return _levenshteinResults;
            }
        }

        internal static IEnumerable<LevenshteinResults> GetLowLevenshteinDistanceCards(Guid cardId, Guid cardTitleId, float X, float Y, MTGCardFrame cardTitleType, Image image)
        {
            List<LevenshteinResults> results = new List<LevenshteinResults>();

            foreach (var thresholdParameter in GetThresholdParamters())
            {
                foreach (var cannyParameter in GetCannyParameters())
                {
                    using (var filteredImage = image.GetFilteredImage(cardTitleType, angle: 0, thresoldParameter: thresholdParameter, cannyParameter: cannyParameter))
                    {
                        Image.FireImageEvent(null, cardId, cardTitleId, ImageType.TitleFiltered,
                            filteredImage, angle: 0, X: X, Y: Y,
                            cannyParameter: cannyParameter,
                            thresholdParmeter: thresholdParameter);
                        {
                            var lldc = GetLowLevenshteinDistanceCards(filteredImage);

                            // Short Circuit
                            if (lldc.Any(lr => lr.Distance < LevenshteinDistanceShortCircuit))
                            {
                                return lldc.Where(lr => lr.Distance < LevenshteinDistanceShortCircuit);
                            }

                            results.AddRange(lldc);
                        }

                        using (Mat erodeImage = new Mat())
                        {
                            CvInvoke.Erode(filteredImage, erodeImage, null, new Point(-1, -1), 1, BorderType.Constant,
                                cardTitleType == MTGCardFrame.M15 ? white : black);

                            Image.FireImageEvent(null, cardId, cardTitleId, ImageType.TitleErode, erodeImage, angle: 0, X: 0, Y: 0,
                                cannyParameter: cannyParameter, thresholdParmeter: thresholdParameter);

                            var lldc = GetLowLevenshteinDistanceCards(erodeImage);

                            // Short Circuit
                            if (lldc.Any(lr => lr.Distance < LevenshteinDistanceShortCircuit))
                            {
                                return lldc.Where(lr => lr.Distance < LevenshteinDistanceShortCircuit);
                            }

                            results.AddRange(lldc);
                        }

                        using (Mat dilateImage = new Mat())
                        {
                            CvInvoke.Dilate(filteredImage, dilateImage, null, new Point(-1, -1), 1, BorderType.Constant,
                                cardTitleType == MTGCardFrame.M15 ? white : black);

                            Image.FireImageEvent(null, cardId, cardTitleId, ImageType.TitleDilate, dilateImage, angle: 0, X: 0, Y: 0,
                                cannyParameter: cannyParameter, thresholdParmeter: thresholdParameter);

                            var lldc = GetLowLevenshteinDistanceCards(dilateImage);

                            // Short Circuit
                            if (lldc.Any(lr => lr.Distance < LevenshteinDistanceShortCircuit))
                            {
                                return lldc.Where(lr => lr.Distance < LevenshteinDistanceShortCircuit);
                            }

                            results.AddRange(lldc);
                        }
                    }
                }
            }

            // Find all cards with the minimum number of character changes (the result of the Levenshtein Distance)
            // alogorithm and return them
            double min = results.Min(result => result.PercentDistance);
            return results.Where(result => result.PercentDistance == min).Distinct();
        }

        /// <summary>
        /// Get all cards with the lowest Levenshtein distance score from the image 
        /// </summary>
        private static IEnumerable<LevenshteinResults> GetLowLevenshteinDistanceCards(Mat image)
        {
            using (var page = _tesseractEngine.Process(image.Bitmap))
            {
                // Get the text from the image
                string text = page.GetText();

                // The name is always single line and trimmed, clean it up a
                // bit after scanning so that we can get a good distance
                text = text.Replace("\n", "").Replace("\r", "").Trim();

                // Completly empty, not helpful
                if (string.IsNullOrWhiteSpace(text))
                {
                    return Enumerable.Empty<LevenshteinResults>();
                }

                var lowLevenshteinDistanceCards = GetLowLevenshteinDistanceCards(text);

                Debug.WriteLine(string.Format("Potential Title \"{0}\" : {1} ", text, string.Join(",", lowLevenshteinDistanceCards)));

                return lowLevenshteinDistanceCards;
            }
        }

        /// <summary>
        /// Get all cards with the lowest Levenshtein distance score from the text
        /// </summary>
        private static IEnumerable<LevenshteinResults> GetLowLevenshteinDistanceCards(string text)
        {
            List<LevenshteinResults> results = new List<LevenshteinResults>();

            double levenshteinDistance = double.MaxValue;

            foreach (MTGSet set in Sets.GetSets())
            {
                foreach (MTGCard card in set.Cards)
                {
                    double distance = LevenshteinDistance.Compute(card.Name, text);

                    if (distance <= levenshteinDistance)
                    {
                        levenshteinDistance = distance;

                        var result = new LevenshteinResults()
                        {
                            PercentDistance = (double)distance / (double)text.Length,
                            Distance = distance,
                            Card = card
                        };

                        results.Add(result);

                        // Short Circuit
                        if (distance < LevenshteinDistanceShortCircuit)
                        {
                            return new LevenshteinResults[] { result };
                        }
                    }
                }
            }

            double min = results.Min(result => result.PercentDistance);
            return results.Where(result => result.PercentDistance == min);
        }

        /// <summary>
        /// Gets the threshold parameters for reducing the title image so to recognize characters
        /// </summary>
        /// <returns></returns>
        private static IEnumerable<ThresholdParm> GetThresholdParamters()
        {
            yield return new ThresholdParm(threshold: 75, maxValue: 255);
            yield return new ThresholdParm(threshold: 100, maxValue: 255);
            yield return new ThresholdParm(threshold: 130, maxValue: 255);
            yield return new ThresholdParm(threshold: 170, maxValue: 255);
            yield return new ThresholdParm(threshold: 200, maxValue: 255);
        }

        /// <summary>
        /// Get the canny parameters used to search the title images for characters
        /// </summary>
        private static IEnumerable<CannyParam> GetCannyParameters()
        {
            yield return new CannyParam(150, 75, true, 3);

            yield return new CannyParam(200, 100, true, 3);

            yield return new CannyParam(100, 50, true, 3);
        }

        /// <summary>
        /// Dispose Images
        /// </summary>
        protected override void DisposeImages()
        {
            base.DisposeImages();
        }
    }
}
