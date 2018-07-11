using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CardModel;

namespace CardUtilities
{
    public static class Search
    {
        /// <summary>
        /// MTG Sets
        /// </summary>
        private static MTGAllSets _sets = new MTGAllSets();

        private static Random _random = new Random();

        public static bool FindCard(int multiverseId, out MTGCard result)
        {
            result = default(MTGCard);

            foreach (MTGSet set in _sets.GetSets())
            {
                foreach (MTGCard card in set.Cards)
                {
                    if (card.MultiverseId == multiverseId)
                    {
                        result = card;
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool TryFindCard(Guid cardGuid, out MTGCard result)
        {
            result = default(MTGCard);

            foreach (MTGSet set in _sets.GetSets())
            {
                foreach (MTGCard card in set.Cards)
                {
                    if (Guid.Parse(card.Id.Substring(0, 32)) == cardGuid)
                    {
                        result = card;
                        return true;
                    }
                }
            }

            return false;


            throw new NotImplementedException();
        }

        /// <summary>
        /// Return a Rando Card
        /// </summary>
        public static MTGCard RandomCard()
        {
            var setList = _sets.GetSets().ToArray();

            MTGSet set;

            do
            {
                int setIndex = _random.Next(0, setList.Length);
                set = setList[setIndex];
            }
            while (set.OnlineOnly == true);

            var cardList = set.Cards.ToArray();
            int cardIndex = _random.Next(0, cardList.Length);
            return cardList[cardIndex];
        }
    }
}
