namespace AlgorithmsTester.Core.Algorithm;

public class RecPowAlgorithm : IAlgorithmTemplate
{
    public string Name { get; } = "RecPow";
    public Complexity Complexity { get; } = Complexity.O_logn;

    private int _n;
    private double _x = 1.5;

    public Array PrepareData(int n)
    {
        _n = n;
        return new double[] { _x };
    }

    public void Run()
    {
        RecPow(_x, _n);
    }

    private static double RecPow(double x, int n)
    {
        if (n <= 0) return 1.0;
        double half = RecPow(x, n / 2);
        if (n % 2 == 0)
        {
            return half * half;
        }
        else
        {
            return half * half * x;
        }
    }
}
