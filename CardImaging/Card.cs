using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.OCR;
using Emgu.CV.Structure;
using Emgu.CV.Util;

namespace CardImaging
{/// <summary>

    public class Card
    {
        /// <summary>
        /// Card Image
        /// </summary>
        private Mat _image;

        public Card(Mat image)
        {
            _image = image;
        }

        /// <summary>
        /// Orient Card So That It Is In Portait
        /// </summary>
        public Mat OrientCard()
        {
            if (_image.Cols > _image.Rows)
            {
                _image = RotateMat(_image, angle: -90);
            }

            return _image;
        }

        /// <summary>
        /// Card Image Bitmap
        /// </summary>
        public Bitmap Bitmap
        {
            get
            {
                return _image.Bitmap;
            }
        }

        public Mat RotateMat(Mat source, double angle)
        {
            PointF src_center = new PointF(source.Cols / 2.0F, source.Rows / 2.0F);
            Mat rot_mat = new RotationMatrix2D(src_center, angle, 1.0);
            Mat dst = new Mat();
            CvInvoke.WarpAffine(source, dst, rot_mat, source.Size);
            return dst;
        }

        public Mat GetGrayImage()
        {
            Mat gray = new Mat();
            CvInvoke.CvtColor(_image, gray, ColorConversion.Bgr2Gray);
            return gray;
        }

        public Mat GetCannyImage(double threshold1, double threshold2)
        {
            Mat canny = new Mat();

            using (Mat gray = GetGrayImage())
            {
                CvInvoke.Canny(gray, canny, threshold1: threshold1, threshold2: threshold2, apertureSize: 3, l2Gradient: false);
            }

            return canny;
        }

        public Mat GetContoursImage(double threshold1, double threshold2)
        {
            using (Mat canny = GetCannyImage(threshold1, threshold2))
            {
                using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
                {
                    CvInvoke.FindContours(canny, contours, null, RetrType.Tree, ChainApproxMethod.ChainApproxSimple);

                    Mat counterImage = new Mat(canny.Size, DepthType.Cv8U, canny.NumberOfChannels);
                    MCvScalar color = new MCvScalar(255, 255, 255);
                    for (int i = 0; i < contours.Size; i++)
                    {
                        CvInvoke.DrawContours(counterImage, contours, i, color);
                    }

                    return counterImage;
                }
            }
        }

        public Mat GetBoxImages(double threshold1, double threshold2)
        {
            Mat template = GetGrayImage();
            MCvScalar color = new MCvScalar(255, 255, 255);

            using (VectorOfVectorOfPoint contours = GetContours(threshold1, threshold2))
            {
                for (int i = 0; i < contours.Size; i++)
                {
                    RotatedRect box = CvInvoke.MinAreaRect(contours[i]);
                    Rectangle rect = box.MinAreaRect();
                    int area = rect.Size.Height * rect.Size.Width;

                    if (area < 200000)
                    {
                        continue;
                    }

                    if (rect.Size.Width > 0)
                    {
                        Debug.WriteLine("{0}:{1} / {2}/{3} : {4} : {5}", rect.Top, rect.Left, rect.Bottom, rect.Right, area, (double)rect.Size.Height / (double)rect.Size.Width);
                    }

                    CvInvoke.Rectangle(template, box.MinAreaRect(), color);
                }
            }

            return template;
        }

        public Mat CreateTemplate()
        {
            return new Mat(_image.Size, DepthType.Cv8U, _image.NumberOfChannels);
        }

        public VectorOfVectorOfPoint GetContours(double threshold1, double threshold2)
        {
            using (Mat canny = GetCannyImage(threshold1, threshold2))
            {
                VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
                CvInvoke.FindContours(canny, contours, null, RetrType.Tree, ChainApproxMethod.ChainApproxSimple);
                return contours;
            }
        }

        public void Test()
        {
            List<IInputOutputArray> licensePlateImagesList = new List<IInputOutputArray>();
            List<IInputOutputArray> filteredLicensePlateImagesList = new List<IInputOutputArray>();
            List<RotatedRect> detectedLicensePlateRegionList = new List<RotatedRect>();

            var licenses = DetectLicensePlate(licensePlateImagesList, filteredLicensePlateImagesList, detectedLicensePlateRegionList);
        }

        /// <summary>
        /// Detect license plate from the given image
        /// </summary>
        /// <param name="licensePlateImagesList">A list of images where the detected license plate regions are stored</param>
        /// <param name="filteredLicensePlateImagesList">A list of images where the detected license plate regions (with noise removed) are stored</param>
        /// <param name="detectedLicensePlateRegionList">A list where the regions of license plate (defined by an MCvBox2D) are stored</param>
        /// <returns>The list of words for each license plate</returns>
        public List<String> DetectLicensePlate(
           List<IInputOutputArray> licensePlateImagesList,
           List<IInputOutputArray> filteredLicensePlateImagesList,
           List<RotatedRect> detectedLicensePlateRegionList)
        {
            List<String> licenses = new List<String>();

            using (Mat gray = new Mat())
            {
                CvInvoke.CvtColor(_image, gray, ColorConversion.Bgr2Gray);

                double threshold1 = 100;

                while (true)
                {
                    using (Mat canny = new Mat())
                    {
                        CvInvoke.Canny(gray, canny, threshold1: threshold1, threshold2: threshold1 / 2.0, apertureSize: 3, l2Gradient: false);

                        using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
                        {
                            int[,] hierachy = CvInvoke.FindContourTree(canny, contours, ChainApproxMethod.ChainApproxSimple);

                            if (GetNumberOfChildren(hierachy, 0) == 0)
                            {
                                threshold1 += 50;
                                continue;
                            }

                            FindLicensePlate(contours, hierachy, 0, gray, canny, licensePlateImagesList, filteredLicensePlateImagesList, detectedLicensePlateRegionList, licenses);

                            return licenses;
                        }
                    }
                }
            }
        }

        private static int GetNumberOfChildren(int[,] hierachy, int idx)
        {
            // First child
            idx = hierachy[idx, 2];

            if (idx < 0)
            {
                return 0;
            }

            int count = 1;

            while (hierachy[idx, 0] > 0)
            {
                count++;
                idx = hierachy[idx, 0];
            }

            return count;
        }

        private void FindLicensePlate(
                 VectorOfVectorOfPoint contours,
                 int[,] hierachy,
                 int idx,
                 IInputArray gray,
                 IInputArray canny,
                List<IInputOutputArray> licensePlateImagesList,
                List<IInputOutputArray> filteredLicensePlateImagesList,
                List<RotatedRect> detectedLicensePlateRegionList,
                List<String> licenses)
        {
            for (; idx >= 0; idx = hierachy[idx, 0])
            {
                int numberOfChildren = GetNumberOfChildren(hierachy, idx);

                // if it does not contains any children (charactor), it is not a license plate region
                if (numberOfChildren == 0)
                {
                    continue;
                }

                using (VectorOfPoint contour = contours[idx])
                {
                    if (CvInvoke.ContourArea(contour) > 400)
                    {
                        if (numberOfChildren < 3)
                        {
                            // If the contour has less than 3 children, it is not a license plate (assuming license plate has at least 3 charactor)
                            // However we should search the children of this contour to see if any of them is a license plate
                            FindLicensePlate(contours, hierachy, hierachy[idx, 2], gray, canny, licensePlateImagesList,
                               filteredLicensePlateImagesList, detectedLicensePlateRegionList, licenses);

                            continue;
                        }


                        RotatedRect box = CvInvoke.MinAreaRect(contour);

                        string text;
                        if (!this.TryParseBox(gray, box, out text))
                        {
                            if (hierachy[idx, 2] > 0)
                                FindLicensePlate(contours, hierachy, hierachy[idx, 2], gray, canny, licensePlateImagesList,
                                   filteredLicensePlateImagesList, detectedLicensePlateRegionList, licenses);

                            continue;
                        }

                        licenses.Add(text);
                        //licensePlateImagesList.Add(plate);
                        //filteredLicensePlateImagesList.Add(filteredPlate);
                        detectedLicensePlateRegionList.Add(box);
                    }
                }
            }
        }

        public bool TryParseBox(IInputArray gray, RotatedRect box, out string text)
        {
            text = null;

            if (box.Angle < -45.0)
            {
                float tmp = box.Size.Width;
                box.Size.Width = box.Size.Height;
                box.Size.Height = tmp;
                box.Angle = 90.0f;
            }
            else if (box.Angle > 45.0)
            {
                float tmp = box.Size.Width;
                box.Size.Width = box.Size.Height;
                box.Size.Height = tmp;
                box.Angle -= 90.0f;
            }

            /*
            double whRatio = (double)box.Size.Width / box.Size.Height;

            if (!(3.0 < whRatio && whRatio < 10.0))
            {
                return false;
            }
            */

            using (UMat tmp1 = new UMat())
            {
                using (UMat tmp2 = new UMat())
                {
                    PointF[] srcCorners = box.GetVertices();

                    PointF[] destCorners = new PointF[] {
                        new PointF(0, box.Size.Height - 1),
                        new PointF(0, 0),
                        new PointF(box.Size.Width - 1, 0),
                        new PointF(box.Size.Width - 1, box.Size.Height - 1)};

                    using (Mat rot = CameraCalibration.GetAffineTransform(srcCorners, destCorners))
                    {
                        CvInvoke.WarpAffine(gray, tmp1, rot, Size.Round(box.Size));
                    }

                    // resize the license plate such that the front is ~ 10-12. This size of front results in better accuracy from tesseract
                    Size approxSize = new Size(240, 180);
                    double scale = Math.Min(approxSize.Width / box.Size.Width, approxSize.Height / box.Size.Height);
                    Size newSize = new Size((int)Math.Round(box.Size.Width * scale), (int)Math.Round(box.Size.Height * scale));
                    CvInvoke.Resize(tmp1, tmp2, newSize, 0, 0, Inter.Cubic);

                    // removes some pixels from the edge
                    int edgePixelSize = 2;

                    Rectangle newRoi = new Rectangle(new Point(edgePixelSize, edgePixelSize),
                       tmp2.Size - new Size(2 * edgePixelSize, 2 * edgePixelSize));

                    UMat plate = new UMat(tmp2, newRoi);

                    UMat filteredPlate = FilterPlate(plate);

                    /*
                   Tesseract.Character[] words;

                    StringBuilder strBuilder = new StringBuilder();

                    using (UMat tmp = filteredPlate.Clone())
                    {
                        //_ocr.Recognize(tmp);
                       // words = _ocr.GetCharacters();

                        if (words.Length == 0)
                        {
                            return true;
                        }

                        for (int i = 0; i < words.Length; i++)
                        {
                            strBuilder.Append(words[i].Text);
                        }
                    }

                    text = strBuilder.ToString();
                    */
                }
            }

            return true;
        }


        /// <summary>


        /// Filter the license plate to remove noise


        /// </summary>


        /// <param name="plate">The license plate image</param>


        /// <returns>License plate image without the noise</returns>


        private static UMat FilterPlate(UMat plate)


        {


            UMat thresh = new UMat();


            CvInvoke.Threshold(plate, thresh, 120, 255, ThresholdType.BinaryInv);


            //Image<Gray, Byte> thresh = plate.ThresholdBinaryInv(new Gray(120), new Gray(255));





            Size plateSize = plate.Size;


            using (Mat plateMask = new Mat(plateSize.Height, plateSize.Width, DepthType.Cv8U, 1))


            using (Mat plateCanny = new Mat())


            using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())


            {


                plateMask.SetTo(new MCvScalar(255.0));


                CvInvoke.Canny(plate, plateCanny, 100, 50);


                CvInvoke.FindContours(plateCanny, contours, hierarchy: null, mode: RetrType.External, method: ChainApproxMethod.ChainApproxSimple);





                int count = contours.Size;


                for (int i = 1; i < count; i++)



                {


                    using (VectorOfPoint contour = contours[i])


                    {





                        Rectangle rect = CvInvoke.BoundingRectangle(contour);


                        if (rect.Height > (plateSize.Height >> 1))


                        {


                            rect.X -= 1; rect.Y -= 1; rect.Width = 2; rect.Height = 2;


                            Rectangle roi = new Rectangle(Point.Empty, plate.Size);


                            rect.Intersect(roi);


                            CvInvoke.Rectangle(plateMask, rect, new MCvScalar(), -1);


                            //plateMask.Draw(rect, new Gray(0.0), -1);


                        }


                    }





                }





                thresh.SetTo(new MCvScalar(), plateMask);


            }





            CvInvoke.Erode(thresh, thresh, null, new Point(-1, -1), 1, BorderType.Constant, CvInvoke.MorphologyDefaultBorderValue);


            CvInvoke.Dilate(thresh, thresh, null, new Point(-1, -1), 1, BorderType.Constant, CvInvoke.MorphologyDefaultBorderValue);





            return thresh;


        }

        /*
        public IEnumerable<Mat> FindAndFocus()
        {
            int focus = 94;
            int focusSteps = 5;
            bool direciton = true;

            Mat[] results = null;

            CameraController cameraController = new CameraController();

            do
            {
                Debug.WriteLine("Capture At Focus: {0}", focus);

                var image = cameraController.CaptureAsync(focus).Result;

                results = FindImages(image).ToArray();

                if (results.Length == 0)
                {
                    if (focus < 190 && direciton)
                    {
                        focus += focusSteps;
                    }
                    if (focus >= 190 && direciton)
                    {
                        direciton = false;
                        focus -= focusSteps;
                    }
                    if (focus > 35 && !direciton)
                    {
                        focus -= focusSteps;
                    }
                    if (focus <= 35 && !direciton)
                    {
                        direciton = true;
                        focus += focusSteps;
                    }
                }
            } while (results.Length == 0);

            return results;
        }
        */


        /*
                public static Mat FindTitleImage(Mat image)
                {
                    using (Mat gray = new Mat())
                    {
        //                CvInvoke.CvtColor(image, gray, ColorConversion.Bgr2Gray);
                        CvInvoke.Threshold(image, gray, 150, 255, ThresholdType.Binary);

                        using (Mat canny = new Mat())
                        {
                            CvInvoke.Canny(gray, canny, threshold1: Titlehreshold1, threshold2: Threshold2, apertureSize: 3, l2Gradient: true);

                            VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();

                            CvInvoke.FindContours(canny, contours, null, RetrType.Tree, ChainApproxMethod.ChainApproxSimple);

                            for (int i = 0; i < contours.Size; i++)
                            {
                                RotatedRect rotatedRect = CvInvoke.MinAreaRect(contours[i]);

                                if (rotatedRect.Size.Width == 0)
                                {
                                    continue;
                                }

                                double area = (double)rotatedRect.Size.Height * (double)rotatedRect.Size.Width;
                                double aspectRatio;
                                float angle =  0.0F;

                                if (rotatedRect.Size.Width > rotatedRect.Size.Height)
                                {
                                    aspectRatio = (double)rotatedRect.Size.Height / (double)rotatedRect.Size.Width;
                                    angle =  -90.0F;
                                }
                                else
                                {
                                    aspectRatio = (double)rotatedRect.Size.Width / (double)rotatedRect.Size.Height;
                                }

                                MCvScalar color = new MCvScalar(0, 255, 255);
                                //CvInvoke.DrawContours(gray, contours, i, color, thickness: 1);

                                if (area < 3000)
                                {
                                    continue;
                                }

                                Debug.WriteLine("Width: {0} Height: {1} Area: {2} : AspectRatio: {3}, Angle: {4}",
                                    rotatedRect.Size.Width, rotatedRect.Size.Height, area, aspectRatio, rotatedRect.Angle + angle);


                                if (area < 6000)
                                {
                                    continue;
                                }

                                if (aspectRatio < MinTitleAspectRatio || aspectRatio > MaxTitleAspectRatio)
                                {
                                    continue;
                                }

                                using (Mat rot_mat = new RotationMatrix2D(rotatedRect.Center, rotatedRect.Angle + angle, 1.0))
                                {
                                    using (Mat rotated = new Mat())
                                    {
                                        // Rotate
                                        CvInvoke.WarpAffine(gray, rotated, rot_mat, image.Size, interpMethod: Inter.Cubic);

                                        // Find the Box To Crop Too
                                        Rectangle box = rotatedRect.MinAreaRect();

                                        // Adjust For Rotation

                                        Size size;
                                        double widthTrim = 10.0;
                                        double heightTrim = 10.0;

                                        if (rotatedRect.Angle + angle < -90)
                                        {
                                            size = new Size((int)(rotatedRect.Size.Height - widthTrim), (int)(rotatedRect.Size.Width - heightTrim));
                                        }
                                        else
                                        {
                                            size = new Size((int)(rotatedRect.Size.Width - widthTrim), (int)(rotatedRect.Size.Height - heightTrim));
                                        }

                                        //CvInvoke.Rectangle(rotated, box, color, thickness: 3);

                                        using (Mat cropped = new Mat())
                                        {
                                            // Cropped To Match The 
                                            CvInvoke.GetRectSubPix(rotated, size, rotatedRect.Center, cropped);

                                            //Mat thresh = new Mat();
                                            //CvInvoke.Threshold(cropped, thresh, 150, 200, ThresholdType.Binary);

                                            _ocr.Recognize(cropped);
                                             Console.WriteLine("Text : {0}", _ocr.GetText());

                                            return cropped.Clone();
                                        }

                                        return rotated.Clone();
                                    }
                                }
                            }
                        }

                        return gray.Clone();
                    }

                    return image;
                }
                */
    }
}
