using System;
using System.Collections.Generic;
using System.Text;

namespace AlgorithmsTester.Core.Algorithm
{
    public class BubbleSortAlgorithm : IAlgorithmTemplate
    {
        public string Name { get; } = "BubbleSort";
        public Complexity Complexity { get; } = Complexity.O_n2;

        private double[] _vector = [];

        public Array PrepareData(int n)
        {
            _vector = DataGenerator.GenerateVector(n);
            return _vector;
        }

        public void Run()
        {
            for (int i = 0; i < _vector.Length - 1; i++)
            {
                bool swapped = false;

                for (int j = 0; j < _vector.Length - 1 - i; j++)
                {
                    if (_vector[j] > _vector[j + 1])
                    {
                        double temp = _vector[j];
                        _vector[j] = _vector[j + 1];
                        _vector[j + 1] = temp;

                        swapped = true;
                    }
                }

                // Если за проход обменов не было,
                // массив уже отсортирован.
                if (!swapped)
                    break;
            }
        }
    }
}
