using System.Security.Cryptography;
using UnityEngine;

namespace Coral_iMD
{
    // Random number generator for Normalized distribution based on Marsaglia polar method.
    public class NormalizedRandom
    {
        private bool  has_stock;
        private float stock;

        public NormalizedRandom()
        {
            has_stock = false;
        }

        // Genarate random number from normalized distribution
        // which specified by mean and standard deviation.
        public float Generate()
        {
            if (!has_stock)
            {
                float u, v, s;
                do
                {
                    u = Random.value * 2 - 1;
                    v = Random.value * 2 - 1;
                    s = u * u + v * v;
                } while ( s >= 1 || s == 0);
                s = Mathf.Sqrt(-2.0f * Mathf.Log(s) / s);
                stock = v * s;
                has_stock = true;
                return u * s;
            }
            else
            {
                has_stock = false;
                return stock;
            }
        }
    }

} // Coral_iMD
