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
    //массив времени выполнения алгоритма 
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

                runTimes.Add(stopwatch.Elapsed.TotalSeconds);
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

        string fileName = string.Format("{0}_{1}_results.csv", _algorithm.Name, _algorithm.Complexity);

        string fullPath = Path.Combine(folderPath, fileName);

        using (StreamWriter file = new StreamWriter(fullPath))
        {
            int n = _nStart;

            foreach (double point in timeResults)
            {
                file.WriteLine(
                    string.Format(
                        System.Globalization.CultureInfo.InvariantCulture,
                        "{0};{1:F8}",
                        n,
                        point));

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

    public double[,] AlgorithmMatrixTimer(int? customMStart = null, int? customMStop = null, int? customMStep = null, int? customK = null)
    {
        int mStart = customMStart ?? _nStart;
        int mStop = customMStop ?? _nStop;
        int mStep = customMStep ?? _steps;
        int k = customK ?? Math.Max(20, (_nStart + _nStop) / 2);

        // Количество точек сетки по N и M
        int nPoints = Math.Max(1, ((_nStop - _nStart) / _steps) + 1);
        int mPoints = Math.Max(1, ((mStop - mStart) / mStep) + 1);

        double[,] timeResults = new double[nPoints, mPoints];
        Stopwatch stopwatch = new Stopwatch();

        // Холостой запуск для прогрева JIT
        if (_algorithm is MatrixMultiplicationAlgorithm matrixAlgo)
        {
            matrixAlgo.PrepareData(_nStart, mStart, k);
            matrixAlgo.Run();
        }
        else
        {
            _algorithm.PrepareData(_nStart);
            _algorithm.Run();
        }

        int i = 0;
        for (int nSize = _nStart; nSize <= _nStop; nSize += _steps, i++)
        {
            int j = 0;
            for (int mSize = mStart; mSize <= mStop; mSize += mStep, j++)
            {
                List<double> runTimes = new List<double>();

                for (int runIndex = 0; runIndex < _repeats; runIndex++)
                {
                    if (_algorithm is MatrixMultiplicationAlgorithm mAlgo)
                    {
                        mAlgo.PrepareData(nSize, mSize, k);
                    }
                    else
                    {
                        _algorithm.PrepareData(nSize);
                    }

                    stopwatch.Restart();
                    _algorithm.Run();
                    stopwatch.Stop();

                    runTimes.Add(stopwatch.Elapsed.TotalMilliseconds);
                }

                timeResults[i, j] = runTimes.Average();
            }
        }

        // Сохранение результатов в CSV
        string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Result");
        Directory.CreateDirectory(folderPath);

        string fileName = $"{_algorithm.Name}_3D_results.csv";
        string fullPath = Path.Combine(folderPath, fileName);

        using (StreamWriter file = new StreamWriter(fullPath))
        {
            file.WriteLine("N;M;K;TimeMs");

            int nIdx = 0;
            for (int n = _nStart; n <= _nStop; n += _steps, nIdx++)
            {
                int mIdx = 0;
                for (int m = mStart; m <= mStop; m += mStep, mIdx++)
                {
                    file.WriteLine($"{n};{m};{k};{timeResults[nIdx, mIdx].ToString("F6", System.Globalization.CultureInfo.InvariantCulture)}");
                }
            }
        }

        // Теоретическая аппроксимация (3D МНК): T(n, m) = C * (n * m * k)
        double sumNumerator = 0;
        double sumDenominator = 0;

        int row = 0;
        for (int n = _nStart; n <= _nStop; n += _steps, row++)
        {
            int col = 0;
            for (int m = mStart; m <= mStop; m += mStep, col++)
            {
                double ops = (double)n * m * k;
                double t = timeResults[row, col];
                sumNumerator += t * ops;
                sumDenominator += ops * ops;
            }
        }

        double cCoeff = sumDenominator > 0 ? sumNumerator / sumDenominator : 0;
        string theorFileName = $"{_algorithm.Name}_3D_Teoreticalresults.csv";
        string theorFullPath = Path.Combine(folderPath, theorFileName);

        using (StreamWriter theorFile = new StreamWriter(theorFullPath))
        {
            theorFile.WriteLine("N;M;K;TimeMs");

            for (int n = _nStart; n <= _nStop; n += _steps)
            {
                for (int m = mStart; m <= mStop; m += mStep)
                {
                    double theorTime = cCoeff * n * m * k;
                    theorFile.WriteLine($"{n};{m};{k};{theorTime.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)}");
                }
            }
        }

        return timeResults;
    }
}