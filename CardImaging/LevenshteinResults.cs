using CardModel;

namespace CardImaging
{
    /// <summary>
    /// Levenshtien Alogrithm Results
    /// </summary>
    internal class LevenshteinResults
    {
        /// <summary>
        /// Levenshtein From Original Text
        /// </summary>
        public double Distance { get; set; }

        /// <summary>
        /// Closest Matching MTG Card
        /// </summary>
        public MTGCard Card { get; set; }

        /// <summary>
        /// Percent of Distance (Distance / Original Lenght)
        /// </summary>
        public double PercentDistance { get; set; }

        /// <summary>
        /// ToString override for Debugging
        /// </summary>
        public override string ToString()
        {
            return string.Format("[\"{0}\" ({1:0.00} - {2:0.00}%)]", Card.Name, Distance, PercentDistance);
        }

        /// <summary>
        /// Equals override for Linq Distnct
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (!(obj is LevenshteinResults))
            {
                return false;
            }

            if (!((LevenshteinResults)obj).Distance.Equals(Distance))
            {
                return false;
            }

            if (!((LevenshteinResults)obj).Card.Equals(Card))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// GetHashCode override for Linq Distinct
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return Distance.GetHashCode() ^ Card.GetHashCode();
        }
    }
}
