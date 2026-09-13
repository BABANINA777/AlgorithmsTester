namespace AlgorithmsTester.Core.Algorithm;

public class NaivePolynomialAlgorithm : IAlgorithmTemplate
{
    public string Name { get; } = "PolynomialNaive";
    public Complexity Complexity { get; } = Complexity.O_n2;
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
        double sum = 0;

        for (int k = 0; k < _vector.Length; k++)
        {
            double power = 1.0;
            for (int p = 0; p < k; p++)
            {
                power *= x;
            }

            sum += _vector[k] * power;
        }
    }
}
