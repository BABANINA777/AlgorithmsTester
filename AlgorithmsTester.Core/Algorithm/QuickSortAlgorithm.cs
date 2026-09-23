namespace AlgorithmsTester.Core.Algorithm;

public class QuickSortAlgorithm : IAlgorithmTemplate
{
    public string Name { get; } = "QuickSort";
    public Complexity Complexity { get; } = Complexity.O_n_logn;

    private double[] _vector = [];

    public Array PrepareData(int n)
    {
        _vector = DataGenerator.GenerateVector(n);
        return _vector;
    }

    public void Run()
    {
        if (_vector.Length <= 1)
            return;

        QuickSort(_vector, 0, _vector.Length - 1);
    }

    private void QuickSort(double[] array, int left, int right)
    {
        int i = left;
        int j = right;

        double pivot = array[left + (right - left) / 2];

        while (i <= j)
        {
            while (array[i] < pivot)
                i++;

            while (array[j] > pivot)
                j--;

            if (i <= j)
            {
                double temp = array[i];
                array[i] = array[j];
                array[j] = temp;

                i++;
                j--;
            }
        }

        if (left < j)
            QuickSort(array, left, j);

        if (i < right)
            QuickSort(array, i, right);
    }
}