namespace AlgorithmsTester.Core.Algorithm;

public class ConstAlgorithms : IAlgorithmTemplate
{
    public string Name { get; } = "Const";
    public Complexity Complexity { get; } = Complexity.O_1;
    double[] vector {get; set;}

    public Array PrepareData(int n)
    {
        vector = DataGenerator.GenerateVector(n);
        return vector;
    }
    public void Run()
    {
        double i = Math.Pow(10, 10) * Math.Pow(10, 10) * Math.Pow(10, 10) * Math.Pow(10, 10);
    }
    
}