namespace AlgorithmsTester.Core;

public class DataGenerator
{
    private static Random _random = new Random();
    public static double[] GenerateVector(int n)
    {
        double[] vector = new double[n];
        for (int i = 0; i < n; i++)
        {
            vector[i] = _random.Next(1000, 10000);
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
                matrix[i, j] = _random.NextDouble() * (10000 - 1000) + 1000;
            }
        }
        return matrix;
    }
}