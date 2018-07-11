using System;

namespace CardImaging
{
    /// <summary>
    /// Contains approximate string matching
    /// </summary>
    static class LevenshteinDistance
    {
        /// <summary>
        /// Compute the distance between two strings.
        /// </summary>
        public static double Compute(string s, string t)
        {
            int n = s.Length;
            int m = t.Length;
            double[,] d = new double[n + 1, m + 1];

            // Step 1
            if (n == 0)
            {
                return m;
            }

            if (m == 0)
            {
                return n;
            }

            // Step 2
            for (int i = 0; i <= n; d[i, 0] = i++)
            {
            }

            for (int j = 0; j <= m; d[0, j] = j++)
            {
            }

            // Step 3
            for (int i = 1; i <= n; i++)
            {
                //Step 4
                for (int j = 1; j <= m; j++)
                {
                    // Step 5
                    double cost = CalculateCost(t[j - 1], s[i - 1]);

                    // Step 6
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }

            // Step 7
            return d[n, m];
        }

        /// <summary>
        /// Calculate the cost of the change. with OCR some characters are so close, that the cost is less then a "full" 1
        /// </summary>

        public static double CalculateCost(char x, char y)
        {
            if (x == y)
            {
                return 0.0;
            }
            else if ((x == 'l' && y == 'I') || (x == 'I' && y == 'l'))
            {
                return .2;
            }
            else if ((x == 'I' && y == 't') || (x == 't' && y == 'I'))
            {
                return .25;
            }
            else if ((x == 'i' && y == 't') || (x == 't' && y == 'i'))
            {
                return .3;
            }
            else if ((x == 't' && y == 'r') || (x == 'r' && y == 't'))
            {
                return .45;
            }
            else
            {
                return 1.0;
            }
        }
    }
}
