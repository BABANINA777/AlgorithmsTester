using System.Diagnostics;

namespace AlgorithmsTester.Core;

public class BenchmarkEngine
{
    private readonly int _nStart;
    private readonly int _nStop;
    private readonly IAlgorithmTemplate _algorithm;

    public BenchmarkEngine(int nstart, int nstop, IAlgorithmTemplate algorithm)
    {
        _nStart = nstart;
        _nStop = nstop;
        _algorithm = algorithm;
    }

    public List<double> AlgorithmTimer()
    {
        List<double> timeResults = new List<double>();
        Stopwatch stopwatch = new Stopwatch();

        // Холостой запуск для прогрева JIT
        _algorithm.PrepareData(_nStart);
        _algorithm.Run();

        for (int nSize = _nStart; nSize < _nStop; nSize += 50)
        {
            List<double> runTimes = new List<double>();

            for (int runIndex = 0; runIndex < 5; runIndex++)
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

                n += 50;
            }
        }

        return timeResults;
    }
}