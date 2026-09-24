using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using AlgorithmsTester.Core.Algorithm;

namespace AlgorithmsTester.Core;

public class BenchmarkEngine
{
    private readonly int _nStart;
    private readonly int _nStop;
    private readonly int _steps;
    private readonly int _repeats;
    private readonly IAlgorithmTemplate _algorithm;

    public BenchmarkEngine(int nstart, int nstop, int steps, int repeats, IAlgorithmTemplate algorithm)
    {
        _nStart = nstart;
        _nStop = nstop;
        _algorithm = algorithm;
        _steps = steps;
        _repeats = repeats;
    }

    public List<double> AlgorithmTimer()
    {
        List<double> timeResults = new List<double>();
        Stopwatch stopwatch = new Stopwatch();

        // Холостой запуск для прогрева JIT
        _algorithm.PrepareData(_nStart);
        _algorithm.Run();

        for (int nSize = _nStart; nSize <= _nStop; nSize += _steps)
        {
            List<double> runTimes = new List<double>();

            for (int runIndex = 0; runIndex < _repeats; runIndex++)
            {
                // Подготавливаем новые данные перед каждым запуском
                _algorithm.PrepareData(nSize);

                stopwatch.Restart();
                _algorithm.Run();
                stopwatch.Stop();

                runTimes.Add(stopwatch.Elapsed.TotalMilliseconds);
            }

            // Среднее время пяти запусков
            double averageTime = runTimes.Average();
            timeResults.Add(averageTime);
        }

        // Запись результатов в CSV
        string folderPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Result");

        Directory.CreateDirectory(folderPath);

        string fileName =
            $"{_algorithm.Name}_{_algorithm.Complexity}_results.csv";

        string fullPath = Path.Combine(folderPath, fileName);

        using (StreamWriter file = new StreamWriter(fullPath))
        {
            int n = _nStart;

            foreach (double point in timeResults)
            {
                file.WriteLine(
                    $"{n};{point.ToString(
                        "F6",
                        System.Globalization.CultureInfo.InvariantCulture)}");

                n += _steps;
            }
        }

        return timeResults;
    }

    // Подсчёт элементарных шагов (операций) для алгоритмов возведения в степень
    public List<double> AlgorithmCount()
    {
        List<double> stepResults = new List<double>();

        for (int nSize = _nStart; nSize <= _nStop; nSize += _steps)
        {
            _algorithm.PrepareData(nSize);
            _algorithm.Run();

            long steps = 0;
            if (_algorithm is SimplePowAlgorithm simple)
            {
                steps = simple.StepCount;
            }
            else if (_algorithm is RecursiveLinearPowAlgorithm rec)
            {
                steps = rec.StepCount;
            }
            else if (_algorithm is QuickPowAlgorithm quick)
            {
                steps = quick.StepCount;
            }

            stepResults.Add(steps);
        }

        // Запись результатов шагов в CSV
        string folderPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Result");

        Directory.CreateDirectory(folderPath);

        string fileName =
            $"{_algorithm.Name}_{_algorithm.Complexity}_results.csv";

        string fullPath = Path.Combine(folderPath, fileName);

        using (StreamWriter file = new StreamWriter(fullPath))
        {
            int n = _nStart;

            foreach (double step in stepResults)
            {
                file.WriteLine(
                    $"{n};{step.ToString(
                        "F6",
                        System.Globalization.CultureInfo.InvariantCulture)}");

                n += _steps;
            }
        }

        return stepResults;
    }
}