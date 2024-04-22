using UnityEngine;

public class HeightMapGenerator : MonoBehaviour
{
    public int seed;
    public bool randomizeSeed;

    public int numOctaves = 7;
    public float persistence = .5f;
    public float lacunarity = 2;
    public float initialScale = 2;

    public bool useComputeShader = true;
    public ComputeShader heightMapComputeShader;

    public float[] GenerateHeightMap(int mapSize)
    {
        return GenerateHeightMapCPU(mapSize);
    }

    float[] GenerateHeightMapCPU(int mapSize)
    {
        var map = new float[mapSize * mapSize];
        seed = (randomizeSeed) ? Random.Range(-10000, 10000) : seed;
        var prng = new System.Random(seed);

        Vector2[] offsets = new Vector2[numOctaves];
        for (int i = 0; i < numOctaves; i++)
        {
            offsets[i] = new Vector2(prng.Next(-1000, 1000), prng.Next(-1000, 1000));
        }

        float minValue = float.MaxValue;
        float maxValue = float.MinValue;

        for (int y = 0; y < mapSize; y++)
        {
            for (int x = 0; x < mapSize; x++)
            {
                float noiseValue = 0;
                float scale = initialScale;
                float weight = 1;
                for (int i = 0; i < numOctaves; i++)
                {
                    Vector2 p = offsets[i] + new Vector2(x / (float)mapSize, y / (float)mapSize) * scale;
                    noiseValue += Mathf.PerlinNoise(p.x, p.y) * weight;
                    weight *= persistence;
                    scale *= lacunarity;
                }
                map[y * mapSize + x] = noiseValue;
                minValue = Mathf.Min(noiseValue, minValue);
                maxValue = Mathf.Max(noiseValue, maxValue);
            }
        }

        // Normalize
        if (maxValue != minValue)
        {
            for (int i = 0; i < map.Length; i++)
            {
                map[i] = (map[i] - minValue) / (maxValue - minValue);
            }
        }

        return map;
    }

    public float[] GenerateMountainBaseShape(int mapSize, AnimationCurve curve, float scale)
    {
        float[] mountainBase = new float[mapSize * mapSize];

        Vector2 center = new Vector2(mapSize / 2, mapSize / 2);

        for (int y = 0; y < mapSize; y++)
        {
            for (int x = 0; x < mapSize; x++)
            {
                // Calculate the distance from the current position to the center
                Vector2 position = new Vector2(x, y);
                float distanceToCenter = Vector2.Distance(center, position);

                // Normalize the distance to a value between 0 and 1
                float normalizedDistance = distanceToCenter / (mapSize / 2);

                // Evaluate the AnimationCurve using the normalized distance
                float curveValue = curve.Evaluate(1 - normalizedDistance) * scale; // Invert the value to make it closer to the center equal to 1

                // Store the curve value in the mountainBase array
                mountainBase[y * mapSize + x] = curveValue;
            }
        }

        return mountainBase;
    }

    public float[] GenerateMountainSide(int mapSize, AnimationCurve curve, float scale)
    {
        float[] mountainBase = new float[mapSize * mapSize];

        for (int y = 0; y < mapSize; y++)
        {
            // Evaluate the AnimationCurve using the normalized Y coordinate
            float normalizedY = (float)y / (mapSize - 1);  // Using mapHeight - 1 to get a range from 0 to 1
            float curveValue = curve.Evaluate(normalizedY) * scale;

            for (int x = 0; x < mapSize; x++)
            {
                // Store the curve value in the mountainBase array for each point along the Y dimension
                mountainBase[y * mapSize + x] = curveValue;
            }
        }

        return mountainBase;
    }
}