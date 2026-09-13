namespace AlgorithmsTester.Core.Algorithm;

public class MultiplicationAlgorithms : IAlgorithmTemplate
{
    public string Name { get; } = "Multiplication";
    public Complexity Complexity { get; } = Complexity.O_1;
    double[] vector {get; set;}

    public Array PrepareData(int n)
    {
        vector = DataGenerator.GenerateVector(n);
        return vector;
    }
    public void Run()
    {
        double mult = 1;
        foreach (var i in vector)
        {
            mult *= i;
        }
    }
}