namespace AlgorithmsTester.Core.Algorithm;

public class VectorAlgorithms : IAlgorithmTemplate
{
    public string Name { get; } = "Vector";
    public Complexity Complexity { get; } = Complexity.O_n;
    double[] vector {get; set;}
    
    public Array PrepareData(int n)
    {
        vector = DataGenerator.GenerateVector(n);
        return vector;
    }

    public void Run()
    {
        double sum = 0;

        foreach (double number in vector)
        {
            sum += number;
        }
    }
}