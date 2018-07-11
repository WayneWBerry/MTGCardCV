using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardModel
{
    /// <summary>
    /// Card Frame
    /// </summary>
    /// <remarks>
    /// The card frame or card face is printed onto the front of a Magic card
    /// and gives a structural property to the card. The card face includes the illustration,
    /// the card frame is literally everything around the illustration.
    /// </remarks>
    public enum MTGCardFrame
    {
        /// <summary>
        /// Since its inception, the game had a card frame separated into two halves. The top half was dominated by
        /// the artwork of the card while the lower half was dominated by the text box. Other features such as name, cost type, rarity and power/toughness 
        /// for creatures was printed directly onto the frame, which at times, especially in earlier editions, made it hard to read. 
        /// </summary>
        Original,

        /// <summary>
        /// With 8th Edition a new card frame was introduced in which the name and cost, types and expansion symbol as
        /// well as the power/toughness were given their own boxes to elevate them from the card frame and enhance readability. 
        /// </summary>
        Modern,

        /// <summary>
        /// With Magic 2015, another update was made to the card frame. This concerned the introduction of a special Magic font (Beleren), 
        /// a holofoil stamp, revamped collector info and a decreased border size.
        /// </summary>
        M15
    }
}
