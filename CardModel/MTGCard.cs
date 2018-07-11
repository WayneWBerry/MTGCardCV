using System;
using System.Runtime.Serialization;

namespace CardModel
{
    [DataContract]
    public class MTGCard
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "id")]
        public string Id { get; set; }

        [DataMember(Name = "cmc")]
        public string CMC { get; set; }

        [DataMember(Name = "multiverseid")]
        public int MultiverseId { get; set; }

        [DataMember(Name = "number")]
        public string Number { get; set; }

        [DataMember(Name = "mciNumber")]
        public string MCINumber { get; set; }

        [DataMember(Name = "printings")]
        public string[] Printings { get; set; }

        [DataMember(Name = "layout")]
        public string Layout { get; set; }

        /// <summary>
        /// MTG Set That the Card Belongs
        /// </summary>
        public MTGSet Set { get; set; }

        /// <summary>
        /// Image Uri On Wizard's Website
        /// </summary>
        public Uri WizardsImageUri
        {
            get
            {
                return new Uri(string.Format("http://gatherer.wizards.com/Handlers/Image.ashx?type=card&multiverseid={0}", MultiverseId));
            }
        }

        /// <summary>
        /// Image Uri on Magic Cards 
        /// </summary>
        public Uri MagicCardsImageUri
        {
            get
            {
                return new Uri(string.Format("https://magiccards.info/scans/en/{0}/{1}.jpg", Set.MagicCardsInfoCode, MCINumber == null ? Number : MCINumber));
            }
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (!(obj is MTGCard))
            {
                return false;
            }

            if (!((MTGCard)obj).Name.Equals(Name))
            {
                return false;
            }

            if (!((MTGCard)obj).Id.Equals(Id))
            {
                return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}
