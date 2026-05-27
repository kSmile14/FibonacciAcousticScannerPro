using UnityEngine;

public static class FibonacciUtils
{
    /// <summary>
    /// Generates an array of evenly distributed unit directions over a sphere
    /// using the Fibonacci sphere algorithm.
    /// </summary>
    public static Vector3[] GenerateDirections(int count)
    {
        if (count <= 0) return new Vector3[0];

        var   directions     = new Vector3[count];
        float goldenRatio    = (1f + Mathf.Sqrt(5f)) / 2f;
        float angleIncrement = Mathf.PI * 2f * goldenRatio;

        for (int i = 0; i < count; i++)
        {
            float t           = (float)i / count;
            float inclination = Mathf.Acos(1f - 2f * t);
            float azimuth     = angleIncrement * i;

            directions[i] = new Vector3(
                Mathf.Sin(inclination) * Mathf.Cos(azimuth),
                Mathf.Cos(inclination),
                Mathf.Sin(inclination) * Mathf.Sin(azimuth)
            ).normalized;
        }

        return directions;
    }
}
