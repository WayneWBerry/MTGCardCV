using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using CardImaging;
using CardModel;
using CardUtilities;
using Emgu.CV;
using Emgu.CV.CvEnum;
//using Accord.MachineLearning;
//using Accord.MachineLearning.VectorMachines;

namespace IconClassifier
{
    class Program
    {
        static void Main(string[] args)
        {
            string dir = @"c:\temp\icons\";
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            DownloadAndProcess(dir);

            //Guid cardGuid = Guid.Parse("e8f8d632-1642-9a1f-0a3b-1ec95d4ebc56");
            //Process(cardGuid, dir);

            //var keyValuePair = ReadAndProcess(dir);

            //Console.WriteLine("{0}/{1} {2}%", keyValuePair.Key, keyValuePair.Value, ((double)keyValuePair.Key / (double)keyValuePair.Value) * 100.0);
            //Console.ReadLine();
        }

        private static void Train()
        {
            CascadeClassifier cascadeClassifer = new CascadeClassifier();
        }

        private static KeyValuePair<int, int> ReadAndProcess(string dir)
        {
            int count = 0;
            int success = 0;

            foreach (string fileName in Directory.GetFiles(@"C:\Temp\cards", "*.bmp"))
            {
                count++;
                Console.WriteLine(string.Format("Reading: {0}", fileName));

                FileInfo info = new FileInfo(fileName);
                var guidName = info.Name.Substring(0, info.Name.Length - 4);

                Guid cardGuid;
                if (Guid.TryParse(guidName, out cardGuid))
                {
                    if (Process(cardGuid, dir))
                    {
                        success++;
                    }
                }
            }

            return new KeyValuePair<int, int>(success, count);
        }

        private static void DownloadAndProcess(string dir)
        {
            for (int i = 0; i < 1000; i++)
            {
                MTGCard card = Search.RandomCard();
                try
                {
                    using (var bitmap = Downloader.DownloadCardImage(card))
                    {
                        Guid cardId = Guid.Parse(card.Id.Substring(0, 32));
                        string fileName = string.Format(@"c:\temp\cards\{0}.bmp", cardId);
                        bitmap.Save(fileName);

                        Process(card, dir);
                    }
                }
                catch (Exception)
                {

                }
            }
        }

        private static bool Process(Guid cardGuid, string dir)
        {
            MTGCard card;

            if (Search.TryFindCard(cardGuid, out card))
            {
                if (card.Layout != "normal")
                {
                    return false;
                }

                try
                {
                    // Write Out The Image Files For Debugging
                    CardImage.ImageEvent += CardImage_ImageEvent;

                    return Process(card, dir);
                }
                finally
                {
                    CardImage.ImageEvent -= CardImage_ImageEvent;
                }
            }

            return false;
        }

        private static bool Process(MTGCard card, string dir)
        {
            bool hashName = false;

            Guid cardId = Guid.Parse(card.Id.Substring(0, 32));
            string fileName = string.Format(@"c:\temp\cards\{0}.bmp", cardId);

            using (Mat image = new Mat(fileName, LoadImageType.Color))
            {
                Console.WriteLine(string.Format("Processing: {0}", fileName));

                CardImage cardImage = new CardImage(cardId, image, angle: 0, cardFrame: card.Set.CardFrame);

                using (Mat setIcon = cardImage.FindSetIcons().FirstOrDefault())
                {
                    if (setIcon != null)
                    {
                        string fullName;

                        if (hashName == true)
                        {
                            string hash;
                            using (SHA1CryptoServiceProvider sha1 = new SHA1CryptoServiceProvider())
                            {
                                ImageConverter converter = new ImageConverter();
                                var byteArray = (byte[])converter.ConvertTo(setIcon.Bitmap, typeof(byte[]));
                                hash = Convert.ToBase64String(sha1.ComputeHash(byteArray));
                                hash = hash.Replace("=", "").Replace("-", "").Replace("+", "").Replace("\\", "").Replace("/", "").ToUpper().Substring(0, 8);
                            }

                            fullName = string.Format(@"{0}{1}-{2}.bmp", dir, card.Set.Code, hash);
                        }
                        else
                        {
                            fullName = string.Format(@"{0}{1}-{2}.bmp", dir, cardId, card.Set.CardFrame);
                        }

                        setIcon.Bitmap.Save(fullName);

                        return true;
                    }
                    else
                    {
                        var savedColor = Console.ForegroundColor;
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(string.Format("Unable To Process: {0}", fileName));
                        Console.ForegroundColor = savedColor;
                    }
                }
            }

            return false;
        }

        private static void CardImage_ImageEvent(object sender, ImageEventArgs data)
        {
            WriteImage(data);
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
