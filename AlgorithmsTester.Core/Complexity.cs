namespace AlgorithmsTester.Core;

public enum Complexity
{
    O_1,        // O(1) - константа
    O_logn,     // O(log n) - степени RecPow, QuickPow
    O_n,        // O(n) - сумма, произведение, Горнер, простой pow
    O_n_logn,   // O(n log n) - QuickSort, Timsort
    O_n1_585,   // O(n^1.585) - алгоритм Карацубы!
    O_n2,       // O(n^2) - пузырек, Дейкстра, наивный полином
    O_n3        // O(n^3) - умножение матриц
}