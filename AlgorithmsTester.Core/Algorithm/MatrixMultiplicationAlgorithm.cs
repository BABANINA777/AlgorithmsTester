namespace AlgorithmsTester.Core.Algorithm;

public class MatrixMultiplicationAlgorithm : IAlgorithmTemplate
{
    public string Name { get; } = "MatrixMultiplication";
    public Complexity Complexity { get; } = Complexity.O_n3;

    private double[,]? _matrixA;
    private double[,]? _matrixB;

    public Array PrepareData(int n)
    {
        _matrixA = DataGenerator.GenerateMatrix(n);
        _matrixB = DataGenerator.GenerateMatrix(n);

        return _matrixA;
    }

    public void Run()
    {
        if (_matrixA is null || _matrixB is null)
            return;

        Multiply(_matrixA, _matrixB);
    }

    private static double[,] Multiply(double[,] a, double[,] b)
    {
        int n = a.GetLength(0);
        double[,] result = new double[n, n];

        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                double sum = 0;

                for (int k = 0; k < n; k++)
                {
                    sum += a[i, k] * b[k, j];
                }

                result[i, j] = sum;
            }
        }

        return result;
    }
}