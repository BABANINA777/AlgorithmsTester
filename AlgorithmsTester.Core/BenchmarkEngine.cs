using System.Diagnostics;

namespace AlgorithmsTester.Core;

public class BenchmarkEngine
{
    private int _nStart;
    private int _nStop;
    private IAlgorithmTemplate _algorithm;
    private int repeat = 5;//Нигде не используется - ЗАЧЕМ? - Нахуй нада

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
        
        //холостой запуск
        Array holost = _algorithm.PrepareData(100);
        _algorithm.Run();
        //Он нужен, чтобы первый настоящий запуск не искажался дополнительными накладными расходами, например JIT - компиляцией метода.



        for (int nSize = _nStart; nSize < _nStop; nSize += 50)
        {
            List<double> runTimes = new List<double>();
            for (int runIndex = 0; runIndex < 5; runIndex++)
            {
                
                Array massive = _algorithm.PrepareData(nSize);
                stopwatch.Restart();
                _algorithm.Run();
                stopwatch.Stop();
                runTimes.Add(stopwatch.Elapsed.TotalMilliseconds);//Returns a double — allows you to Returns a double — allows you to Get the fractional part of a millisecond.
            }
            //усреднение пяти прогонов
            double averageTime = runTimes.Average();            
            timeResults.Add(averageTime);
        }
        //часть с записью результатов в файл
        string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Result");//AppDomain.CurrentDomain.BaseDirectory - The path to the folder from which the program is launched.
        Directory.CreateDirectory(folderPath);
        string fileName = $"{_algorithm.Name}_{_algorithm.Complexity}_results.csv";
        string fullPath = Path.Combine(folderPath, fileName);
        using (StreamWriter file = new StreamWriter(fullPath))
        {
            int n = _nStart;
            foreach (double point in timeResults)
            {
                file.WriteLine($"{n};{point.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)}");
                n += 50;
            }
        }
        return timeResults;
    }
}