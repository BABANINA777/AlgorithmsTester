namespace AlgorithmsTester.Core.Algorithm;

public class VectorAlgorithms : IAlgorithmTemplate
{
    public string Name { get; } = "Vector";
    public Complexity Complexity { get; } = Complexity.O_n;
    double[] arr {get; set;}
    
    public Array PrepareData(int n)
    {
        arr = DataGenerator.GenerateVector(n);
        return arr;
    }

    public void Run()
    {
        double sum = 0;
        double mult = 1;
        foreach (double num1 in arr)
        {
            sum += num1;
        }
    }
}