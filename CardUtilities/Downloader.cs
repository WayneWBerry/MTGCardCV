using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CardModel;

namespace CardUtilities
{
    public static class Downloader
    {
        /// <summary>
        /// Download Card Image As Bitmap
        /// </summary>
        /// <param name="card"></param>
        /// <returns></returns>
        public static Bitmap DownloadCardImage(MTGCard card)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(card.MagicCardsImageUri);
            request.AutomaticDecompression = DecompressionMethods.GZip;

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                using (Stream stream = response.GetResponseStream())
                {
                    return new Bitmap(stream);
                }
            }
        }
    }
}
