namespace AlgorithmsTester.Core.Algorithm;

public class HornerPolynomialAlgorithm : IAlgorithmTemplate
{
    public string Name { get; } = "PolynomialHorner";
    public Complexity Complexity { get; } = Complexity.O_n;
    private double[] _vector = [];

    public Array PrepareData(int n)
    {
        _vector = DataGenerator.GenerateVector(n);
        return _vector;
    }

    public void Run()
    {
        if (_vector.Length == 0) return;

        double x = 1.5;
        double result = _vector[^1];

        for (int i = _vector.Length - 2; i >= 0; i--)
        {
            result = result * x + _vector[i];
        }
    }
}
