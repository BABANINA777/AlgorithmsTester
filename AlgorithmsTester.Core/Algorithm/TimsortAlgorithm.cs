namespace AlgorithmsTester.Core.Algorithm;

public class TimsortAlgorithm : IAlgorithmTemplate
{
    public string Name { get; } = "Timsort";
    public Complexity Complexity { get; } = Complexity.O_n_logn;

    private double[] _vector = [];

    public Array PrepareData(int n)
    {
        _vector = DataGenerator.GenerateVector(n);
        return _vector;
    }

    public void Run()
    {
        Array.Sort(_vector);
    }
}