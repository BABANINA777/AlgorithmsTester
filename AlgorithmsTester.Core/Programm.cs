using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using AlgorithmsTester.Core.Algorithm;

namespace AlgorithmsTester.Core;

public class Programm
{
    public static void Main(string[] args)
    {
        Console.WriteLine("===============================================================");
        Console.WriteLine("       ЗАПУСК ПОЛНОГО ПАКЕТА БЕНЧМАРКОВ ВСЕХ АЛГОРИТМОВ       ");
        Console.WriteLine("===============================================================");

        Stopwatch totalSw = Stopwatch.StartNew();

        // 1. Векторные операции (N: 0..100000, шаг 1000, 5 повторов)
        Console.WriteLine("\n[1/6] Векторные операции (O(1), O(n), O(n))...");
        RunBenchmark("Векторные операции", new ConstAlgorithms(), 0, 100000, 1000, 5);
        RunBenchmark("Векторные операции", new VectorAlgorithms(), 0, 100000, 1000, 5);
        RunBenchmark("Векторные операции", new MultiplicationAlgorithms(), 0, 100000, 1000, 5);

        // 2. Полиномы (N: 0..2000, шаг 50, 5 повторов)
        Console.WriteLine("\n[2/6] Полиномы (O(n^2), O(n), O(n^1.585))...");
        RunBenchmark("Полиномы", new NaivePolynomialAlgorithm(), 0, 2000, 50, 5);
        RunBenchmark("Полиномы", new HornerPolynomialAlgorithm(), 0, 2000, 50, 5);
        RunBenchmark("Полиномы", new KaratsubaAlgorithm(), 0, 2000, 50, 5);

        // 3. Возведение в степень (подсчёт элементарных шагов/операций) (N: 0..1000, шаг 20)
        Console.WriteLine("\n[3/6] Возведение в степень (подсчёт шагов: O(n), O(n), O(log n))...");
        RunBenchmark("Возведение в степень", new SimplePowAlgorithm(), 0, 1000, 20, 1, isStepCount: true);
        RunBenchmark("Возведение в степень", new RecursiveLinearPowAlgorithm(), 0, 1000, 20, 1, isStepCount: true);
        RunBenchmark("Возведение в степень", new QuickPowAlgorithm(), 0, 1000, 20, 1, isStepCount: true);

        // 4. Сортировки (N: 0..5000, шаг 100, 5 повторов)
        Console.WriteLine("\n[4/6] Сортировки (O(n^2), O(n log n), O(n log n))...");
        RunBenchmark("Сортировки", new BubbleSortAlgorithm(), 0, 5000, 100, 5);
        RunBenchmark("Сортировки", new QuickSortAlgorithm(), 0, 5000, 100, 5);
        RunBenchmark("Сортировки", new TimsortAlgorithm(), 0, 5000, 100, 5);

        // 5. Графы - Дейкстра (V: 0..300, шаг 10, 5 повторов)
        Console.WriteLine("\n[5/6] Графы (Алгоритм Дейкстры, O(V^2))...");
        RunBenchmark("Графы", new DijkstraAlgorithm(), 0, 300, 10, 5);

        // 6. Матричные операции
        Console.WriteLine("\n[6/6] Матричные операции (3D и 2D сетки)...");
        var matrixAlgo = new MatrixMultiplicationAlgorithm();
        RunMatrix3DBenchmark("Матричные операции", matrixAlgo, 50, 300, 50, 300, 50, 3);
        RunBenchmark("Матричные операции", matrixAlgo, 0, 300, 20, 3);

        // Синхронизация CSV файлов в папки Desktop и Result
        Console.WriteLine("\nСинхронизация результатов CSV в папку Desktop/Result...");
        SyncResultFiles();

        totalSw.Stop();
        Console.WriteLine("\n===============================================================");
        Console.WriteLine(string.Format("ВСЕ 14 АЛГОРИТМОВ УСПЕШНО ПРОТЕСТИРОВАНЫ ЗА {0:F1} сек!", totalSw.Elapsed.TotalSeconds));
        Console.WriteLine("Результаты сохранены в БД benchmark.db и файлы Result/*.csv");
        Console.WriteLine("===============================================================");
    }

    private static void RunBenchmark(
        string groupName,
        IAlgorithmTemplate algorithm,
        int nStart,
        int nStop,
        int step,
        int repeats,
        bool isStepCount = false)
    {
        string mode = isStepCount ? "подсчёт шагов" : "замер времени";
        Console.WriteLine(string.Format("  -> {0} ({1}): N от {2} до {3}, шаг {4} [{5}]...",
            algorithm.Name, algorithm.Complexity, nStart, nStop, step, mode));

        Stopwatch sw = Stopwatch.StartNew();
        var engine = new BenchmarkEngine(nStart, nStop, step, repeats, algorithm);
        List<double> results;

        if (isStepCount)
        {
            results = engine.AlgorithmCount();
        }
        else
        {
            results = engine.AlgorithmTimer();
        }

        TheoreticalFitter fitter = new TheoreticalFitter(algorithm);
        fitter.TeoreticalAlgorithmTimer(nStart, nStop, results, step);

        long expId = DatabaseManager.SaveExperimentRun(groupName, algorithm, nStart, nStop, step, repeats, results);
        sw.Stop();

        Console.WriteLine(string.Format("     Готово за {0:F2} с (Эксперимент ID: {1}, точек: {2})",
            sw.Elapsed.TotalSeconds, expId, results.Count));
    }

    private static void RunMatrix3DBenchmark(
        string groupName,
        MatrixMultiplicationAlgorithm algorithm,
        int nStart,
        int nStop,
        int mStart,
        int mStop,
        int step,
        int repeats)
    {
        Console.WriteLine(string.Format("  -> {0} 3D: N от {1} до {2}, M от {3} до {4}, шаг {5}...",
            algorithm.Name, nStart, nStop, mStart, mStop, step));

        Stopwatch sw = Stopwatch.StartNew();
        var engine = new BenchmarkEngine(nStart, nStop, step, repeats, algorithm);
        double[,] results3D = engine.AlgorithmMatrixTimer(mStart, mStop, step);

        long expId = DatabaseManager.SaveExperimentRun(
            groupName,
            algorithm,
            nStart,
            nStop,
            mStart,
            mStop,
            step,
            repeats,
            results3D);

        sw.Stop();
        Console.WriteLine(string.Format("     Готово за {0:F2} с (Эксперимент ID: {1})",
            sw.Elapsed.TotalSeconds, expId));
    }

    private static void SyncResultFiles()
    {
        string sourceDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Result");
        if (!Directory.Exists(sourceDir))
            return;

        string[] candidateDirs = new[]
        {
            Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\AlgorithmsTester.Desktop\Result")),
            Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\AlgorithmsTester.Desktop\bin\Debug\net10.0\Result")),
            Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\Result")),
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "AlgorithmsTester.Desktop", "Result")),
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "AlgorithmsTester.Desktop", "bin", "Debug", "net10.0", "Result"))
        };

        var files = Directory.GetFiles(sourceDir, "*.csv");
        foreach (string targetDir in candidateDirs)
        {
            try
            {
                if (Path.GetFullPath(targetDir).Equals(Path.GetFullPath(sourceDir), StringComparison.OrdinalIgnoreCase))
                    continue;

                Directory.CreateDirectory(targetDir);
                foreach (string file in files)
                {
                    string dest = Path.Combine(targetDir, Path.GetFileName(file));
                    File.Copy(file, dest, true);
                }
            }
            catch
            {
                // Игнорируем недоступные директории
            }
        }
    }
}