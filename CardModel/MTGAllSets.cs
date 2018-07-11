using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace CardModel
{
    public class MTGAllSets
    {
        /// <summary>
        /// Cached Loaded Sets
        /// </summary>
        Dictionary<string, MTGSet> _sets;

        /// <summary>
        /// Get All the Sets
        /// </summary>
        /// <returns></returns>
        public MTGSet[] GetSets()
        {
            if (_sets == null)
            {
                using (Stream allSetsStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(@"CardModel.data.AllSets-x.json"))
                {
                    using (StreamReader streamReader = new StreamReader(allSetsStream))
                    {
                        string result = streamReader.ReadToEnd();
                        _sets = Deserialize<Dictionary<string, MTGSet>>(result);
                    }
                }

                foreach(MTGSet set in _sets.Values)
                {
                    foreach(MTGCard card in set.Cards)
                    {
                        card.Set = set;
                    }
                }
            }

            return _sets.Values.ToArray();
        }

        /// <summary>
        /// Find Set By Card Name
        /// </summary>
        /// <param name="name">Card Name</param>
        /// <returns>List Of Sets</returns>
        public IEnumerable<MTGSet> FindSetByCardName(string text)
        {
            if (text == null)
            {
                yield break;
            };

            List<KeyValuePair<string, MTGSet>> list = new List<KeyValuePair<string, MTGSet>>();

            foreach (MTGSet set in GetSets())
            {
                foreach (MTGCard card in set.Cards)
                {
                    // Use Starts With To "Trim" Off Mana
                    if (text.Equals(card.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        yield return set;
                    }
                }
            }
        }

        public static T Deserialize<T>(string json)
        {
            DataContractJsonSerializerSettings settings = new DataContractJsonSerializerSettings()
            {
                UseSimpleDictionaryFormat = true
            };

            DataContractJsonSerializer deserializer = new DataContractJsonSerializer(typeof(T), settings);
            using (MemoryStream stream = new MemoryStream(Encoding.Unicode.GetBytes(json)))
            {
                T result = (T)deserializer.ReadObject(stream);
                return result;
            }
        }
    }
}
