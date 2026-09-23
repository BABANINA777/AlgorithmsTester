namespace AlgorithmsTester.Core.Algorithm;

public class QuickPowAlgorithm : IAlgorithmTemplate
{
    public string Name { get; } = "QuickPow";
    public Complexity Complexity { get; } = Complexity.O_logn;

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
        BinaryPow(_x, _n, ref steps);
        StepCount = steps;
    }

    private static double BinaryPow(double x, int n, ref long steps)
    {
        if (n <= 0) return 1.0;

        steps++; // шаг деления / вызова
        double half = BinaryPow(x, n / 2, ref steps);

        if (n % 2 == 0)
        {
            steps++; // умножение half * half
            return half * half;
        }
        else
        {
            steps += 2; // умножения half * half и * x
            return half * half * x;
        }
    }
}