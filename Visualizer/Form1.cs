using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using CardImaging;
using CardModel;
using CardUtilities;
using Emgu.CV;
using Emgu.CV.CvEnum;

namespace Visualizer
{
    public partial class Form1 : Form
    {
        CameraController cameraController = new CameraController();
        public Form1()
        {
            InitializeComponent();
        }

        private void ReImage(Guid cardId, Mat image)
        {
            Cursor.Current = Cursors.WaitCursor;

            try
            {
                CardImage.ImageEvent += CardImage_ImageEvent;

                /*
                var image = cameraController.CaptureAsync(focus: 110).Result;
                pictureBox1.Image = image.Bitmap;

                image.Bitmap.Save(@"c:\temp\test.bmp");
                */

                CardField cardField = new CardField(image);

                if (cardField != null)
                {
                    Mat contourImage = cardField.GetContouredImage(angle: 0, cannyParameter: new CannyParam(250, 125, true, 3));
                    if (contourImage != null)
                    {
                        pictureBox7.Image = contourImage.Bitmap;
                        tabPage7.Text = "Field Contoured";
                    }

                    CardImage cardImage = cardField.FindCards(cardId).FirstOrDefault();

                    if (cardImage != null)
                    {
                        Mat setIconImage = cardImage.FindSetIcons().FirstOrDefault();
                        if (setIconImage != null)
                        {
                            pictureBox8.Image = setIconImage.Bitmap;
                            tabPage8.Text = "Set Icon";
                        }

                        pictureBox1.Image = cardImage.GetRotatedImage().Bitmap;
                        tabPage1.Text = "Card Image";

                        Mat cardGreyImage = cardImage.GetGreyImage();
                        if (cardGreyImage != null)
                        {
                            pictureBox6.Image = cardGreyImage.Bitmap;
                            tabPage6.Text = "Grey Image";
                        }

                        Mat cardContourImage = cardImage.GetContouredImage(angle: 0, cannyParameter: new CannyParam(250, 125, true, 3));
                        if (cardContourImage != null)
                        {
                            pictureBox2.Image = cardContourImage.Bitmap;
                            tabPage2.Text = "Card Contour";
                        }

                        CardTitleImage titleImage = cardImage.GetCardTitleImage();

                        if (titleImage != null)
                        {
                            pictureBox3.Image = titleImage.GetImage().Bitmap;
                            tabPage3.Text = "Title Image";
                        }
                    }
                }
            }
            finally
            {
                CardImage.ImageEvent -= CardImage_ImageEvent;
                Cursor.Current = Cursors.Default;
            }

        }

        private static void WriteImage(ImageEventArgs data)
        {
            string dir = string.Format(@"c:\temp\cards\{0}\", data.CardId);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            dir = string.Format(@"c:\temp\cards\{0}\{1}\", data.CardId, data.ImageType.ToString());
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            // If There Is a Title Image, Use That As a Sub Directory
            if ((data.CardId != data.ImageId) && (data.ImageType != ImageType.FieldContoured))
            {
                dir = Path.Combine(dir, string.Format(@"\{0}\", data.ImageId));
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
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

        private void CardImage_ImageEvent(object sender, ImageEventArgs data)
        {
            WriteImage(data);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;
            button4.Enabled = false;

            try
            {
                var image = cameraController.CaptureAsync(focus: 102).Result;
                pictureBox1.Image = (Bitmap)image.Bitmap.Clone();
                string name = Guid.NewGuid().ToString().ToUpper();
                string fullName = string.Format(@"c:\temp\cards\{0}.bmp", name);
                image.Bitmap.Save(fullName);
            }
            finally
            {
                Cursor.Current = Cursors.Default;
                button4.Enabled = true;
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            Guid cardId = Guid.Parse("00e4c03e-0517-d885-6445-568550da404c");
            string fileName = string.Format(@"C:\Temp\cards\{0}.bmp", cardId);
            Mat image = new Mat(fileName, LoadImageType.Color);
            ReImage(cardId, image);
        }

        private void trackBar2_Scroll(object sender, EventArgs e)
        {
            Redo();
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            Redo();
        }

        private void Redo()
        {
            Guid cardId = Guid.Parse("014F3226-9CB7-4A7F-9C98-6821BADB6033");
            string fileName = string.Format(@"C:\Temp\cards\{0}.bmp", cardId);

            using (Mat titleImage = new Mat(fileName, LoadImageType.Color))
            {
                var image = new CardImaging.Image(titleImage);

                CannyParam cannyParam = new CannyParam(trackBar1.Value, trackBar2.Value, false, 5);

                using (var contouredImage = image.GetContouredImage(angle: 0, cannyParameter: cannyParam))
                {
                    if (pictureBox9.Image != null)
                    {
                        pictureBox9.Image.Dispose();
                    }

                    pictureBox9.Image = (Bitmap)contouredImage.Bitmap.Clone();
                    pictureBox9.Update();
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            MTGCard card = null;

            Cursor.Current = Cursors.WaitCursor;

            try
            {
                if (Search.FindCard(368970, out card))
                {
                    using (var bitmap = Downloader.DownloadCardImage(card))
                    {
                        Guid guid = Guid.Parse(card.Id.Substring(0, 32));
                        string fileName = string.Format(@"c:\temp\cards\{0}.bmp", guid);
                        bitmap.Save(fileName);

                        using (Mat image = new Mat(fileName, LoadImageType.AnyColor))
                        {
                            CardImage.ImageEvent += CardImage_ImageEvent;
                            CardField cardField = new CardField(image);
                            var cards = cardField.FindCards(guid).ToArray();
                            CardImage.ImageEvent -= CardImage_ImageEvent;
                        }
                    }
                }
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < 100; i++)
            {
                MTGCard card = Search.RandomCard();
                try
                {
                    using (var bitmap = Downloader.DownloadCardImage(card))
                    {
                        Guid guid = Guid.Parse(card.Id.Substring(0, 32));
                        string fileName = string.Format(@"c:\temp\cards\{0}.bmp", guid);
                        bitmap.Save(fileName);
                    }
                }
                catch (Exception)
                {

                }
            }
        }
    }
}
