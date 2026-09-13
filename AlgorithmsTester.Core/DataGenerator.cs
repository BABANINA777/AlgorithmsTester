namespace AlgorithmsTester.Core;

public class DataGenerator
{
    private static Random _random = new Random();

    private static double RandomDouble (double min, double max)
    {
        return _random.NextDouble() * (max - min) + min;
    }

    public static double[] GenerateVector(int n)
    {
        double[] vector = new double[n];
        for (int i = 0; i < n; i++)
        {
            vector[i] = RandomDouble(1000, 10_000);
        }
        return vector;
    }

    public static double[,] GenerateMatrix(int n)
    {
        double[,] matrix = new double[n, n];
        for (int i=0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                matrix[i, j] = RandomDouble(1000, 10_000);
            }
        }
        return matrix;
    }
}