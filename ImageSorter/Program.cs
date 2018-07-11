using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CardImaging;
using Emgu.CV;
using Emgu.CV.CvEnum;

namespace ImageSorter
{
    class Program
    {
        static void Main(string[] args)
        {
            int count = 0;
            int success = 0;
            foreach (string fileName in Directory.GetFiles(@"C:\Temp\cards", "*.bmp"))
            {
                count++;

                using (Mat image = new Mat(fileName, LoadImageType.Color))
                {
                    try
                    {
                        Console.WriteLine(string.Format("Processing: {0}", fileName));

                        FileInfo info = new FileInfo(fileName);
                        var guid = info.Name.Substring(0, info.Name.Length - 4);
                        Guid cardId = Guid.Parse(guid);

                        CardField cardField = new CardField(image);

                        Stopwatch stopWatch = Stopwatch.StartNew();

                        CardImage cardImage = cardField.FindCards(cardId).FirstOrDefault();

                        stopWatch.Stop();

                        if (cardImage != null)
                        {
                            Console.WriteLine(string.Format("Found: {0} ({1:0.0}%) in {2} seconds", cardImage.GetName(), cardImage.GetNameSurity(), stopWatch.Elapsed.Seconds));
                            success++;

                            try
                            {
                                // Write Out The Image Files For Debugging
                                CardImage.ImageEvent += CardImage_ImageEvent;

                                using (Mat setIcon = cardImage.FindSetIcons().FirstOrDefault())
                                {
                                    if (setIcon != null)
                                    {
                                        WriteSetIcon(cardId, setIcon);
                                    }
                                }
                            }
                            finally
                            {
                                CardImage.ImageEvent -= CardImage_ImageEvent;
                            }
                        }
                        else
                        {
                            // Failed

                            var savedColor = Console.ForegroundColor;
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine(string.Format("Unable To Process: {0}", fileName));
                            Console.ForegroundColor = savedColor;

                            try
                            {
                                // Do It Again And Write Out The Image Files For Debugging
                                CardImage.ImageEvent += CardImage_ImageEvent;
                                cardField.FindCards(cardId).ToArray();
                            }
                            finally
                            {
                                CardImage.ImageEvent -= CardImage_ImageEvent;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                    }
                }
            }

            Console.WriteLine("{0}/{1} {2}%", success, count, ((double)success / (double)count) * 100.0);
            Console.ReadLine();
        }

        private static void CardImage_ImageEvent(object sender, ImageEventArgs data)
        {
            WriteImage(data);
        }

        private static void WriteSetIcon(Guid cardId, Mat setIconImage)
        {
            string dir = @"c:\temp\cards\icons\";
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            double aspectRatio = (double)setIconImage.Width / (double)setIconImage.Height;

            string fullName = string.Format(@"{0}{1} - {2:0.00}.bmp", dir, cardId, aspectRatio);
            setIconImage.Bitmap.Save(fullName);
        }

        private static void WriteImage(ImageEventArgs data)
        {
            string dir = string.Format(@"c:\temp\cards\{0}\", data.CardId);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            // If There Is a Title Image, Use That As a Sub Directory
            if ((data.CardId != data.ImageId) && (data.ImageType != ImageType.FieldContoured))
            {
                dir = string.Format(@"{0}{1}\", dir, data.ImageId);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
            }

            dir = string.Format(@"{0}{1}\", dir, data.ImageType);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string name = data.Angle.ToString() + " " + data.X.ToString("0.00") + "-" + data.Y.ToString("0.00") + " " + data.Image.Size.Width + "-" + data.Image.Size.Height;

            if (data.ThresholdParamters != null)
            {
                name = name == null ? data.ThresholdParamters.ToString() : name + " " + data.ThresholdParamters.ToString();
            }

            if (data.CannyParameters != null)
            {
                name = name == null ? data.CannyParameters.ToString() : name + " " + data.CannyParameters.ToString();
            }

            if (name == null)
            {
                name = Guid.NewGuid().ToString();
            }

            string fullName = string.Format(@"{0}{1}.bmp", dir, name);
            data.Image.Bitmap.Save(fullName);
        }
    }
}
