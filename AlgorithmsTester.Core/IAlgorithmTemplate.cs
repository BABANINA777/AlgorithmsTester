namespace AlgorithmsTester.Core;

public interface IAlgorithmTemplate
{
    string Name { get; }
    Complexity Complexity { get; }
    Array PrepareData(int n);//генерирует массивы(одномерные, матрицы)
    void Run();
}