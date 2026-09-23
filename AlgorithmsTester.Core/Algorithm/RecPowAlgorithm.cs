namespace AlgorithmsTester.Core.Algorithm;

public class RecursiveLinearPowAlgorithm : IAlgorithmTemplate
{
    public string Name { get; } = "RecursiveLinearPow";
    public Complexity Complexity { get; } = Complexity.O_n;

    private int _n;
    private double _x = 1.5;

    public long StepCount { get; private set; }

    public Array PrepareData(int n)
    {
        _n = n;
        StepCount = 0;
        return new double[] { _x };
    }

    public void Run()
    {
        long steps = 0;
        PowRecursive(_x, _n, ref steps);
        StepCount = steps;
    }

    private static double PowRecursive(double x, int n, ref long steps)
    {
        if (n <= 0) return 1.0;
        steps++; // 1 умножение / шаг рекурсии
        return x * PowRecursive(x, n - 1, ref steps);
    }
}