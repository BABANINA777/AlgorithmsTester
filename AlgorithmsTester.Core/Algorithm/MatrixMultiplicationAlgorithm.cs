namespace AlgorithmsTester.Core.Algorithm;

public class MatrixMultiplicationAlgorithm : IAlgorithmTemplate
{
    public string Name { get; } = "MatrixMultiplication";
    public Complexity Complexity { get; } = Complexity.O_n3;

    private double[,]? _matrixA;
    private double[,]? _matrixB;

    public Array PrepareData(int n)
    {
        return PrepareData(n, n, n);
    }

    public Array PrepareData(int n, int m, int k)
    {
        _matrixA = DataGenerator.GenerateMatrix(n, k);
        _matrixB = DataGenerator.GenerateMatrix(k, m);

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
        int rowsA = a.GetLength(0);
        int colsA = a.GetLength(1);
        int colsB = b.GetLength(1);

        double[,] result = new double[rowsA, colsB];

        for (int i = 0; i < rowsA; i++)
        {
            for (int j = 0; j < colsB; j++)
            {
                double sum = 0;

                for (int k = 0; k < colsA; k++)
                {
                    sum += a[i, k] * b[k, j];
                }

                result[i, j] = sum;
            }
        }

        return result;
    }
}