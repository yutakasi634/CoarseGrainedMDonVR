using System.Security.Cryptography;
using UnityEngine;

// Random number generator for Normalized distribution based on Box-Muller's method.
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
    public float Generate(float mean = 0.0f, float standard_deviation = 1.0f)
    {
        if (!has_stock)
        {
            // TODO: avoid x and y equal 1.0 or 0.0 case by non adhock method.
            float x = Random.Range(0.01f, 0.99f);
            float y = Random.Range(0.01f, 0.99f);
            float random = Mathf.Sqrt(-2.0f * Mathf.Log(x)) * Mathf.Cos(2.0f * Mathf.PI * y);
            stock        = Mathf.Sqrt(-2.0f * Mathf.Log(x)) * Mathf.Sin(2.0f * Mathf.PI * y);
            has_stock = true;
            return random * standard_deviation + mean;
        }
        else
        {
            has_stock = false;
            return stock * standard_deviation + mean;
        }
    }
}
