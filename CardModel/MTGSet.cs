using System;
using System.Runtime.Serialization;

namespace CardModel
{
    [DataContract]
    public class MTGSet
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "code")]
        public string Code { get; set; }

        [DataMember(Name = "magicCardsInfoCode")]
        public string MagicCardsInfoCode { get; set; }

        [DataMember(Name = "cards")]
        public MTGCard[] Cards { get; set; }

        [DataMember(Name = "releaseDate")]
        public string ReleaseDate { get; set; }

        [DataMember(Name = "onlineOnly")]
        public bool OnlineOnly { get; set; }

        /// <summary>
        /// Card Frame
        /// </summary>
        public MTGCardFrame CardFrame
        {
            get
            {
                DateTime releaseDate = DateTime.Parse(ReleaseDate);

                if (releaseDate <= DateTime.Parse("July 29, 2003"))
                {
                    return MTGCardFrame.Original;
                }
                else if (releaseDate <= DateTime.Parse("July 18, 2014"))
                {
                    return MTGCardFrame.M15;
                }
                else
                {
                    return MTGCardFrame.Modern;
                }
            }
        }
    }
}
