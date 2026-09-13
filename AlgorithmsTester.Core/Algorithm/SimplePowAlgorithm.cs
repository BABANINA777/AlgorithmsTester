namespace AlgorithmsTester.Core.Algorithm;

public class SimplePowAlgorithm : IAlgorithmTemplate
{
    public string Name { get; } = "SimplePow";
    public Complexity Complexity { get; } = Complexity.O_n;

    private int _n;
    private double _x = 1.5;

    public Array PrepareData(int n)
    {
        _n = n;
        return new double[] { _x };
    }

    public void Run()
    {
        double result = 1.0;
        for (int i = 0; i < _n; i++)
        {
            result *= _x;
        }
    }
}
