using System.Diagnostics;

namespace AlgorithmsTester.Core;

public class BenchmarkEngine
{
    public int nstart;
    public int nstop;
    public IAlgorithmTemplate algorithm;
    private int repeat = 5;

    public BenchmarkEngine(int nstart, int nstop, IAlgorithmTemplate algorithm)
    {
        this.nstart = nstart;
        this.nstop = nstop;
        this.algorithm = algorithm;
    }
    public List<double> AlgorithmTimer()
    {
        List<double> timeResults = new List<double>();
        Stopwatch stopwatch = new Stopwatch();
        
        //холостой запуск
        Array holost = algorithm.PrepareData(100);
        algorithm.Run();
        
        
        for (int i = nstart; i < nstop; i += 50)
        {
            List<double> minitime = new List<double>();
            for (int j = 0; j < 5; j++)
            {
                
                Array massive = algorithm.PrepareData(i);
                stopwatch.Reset();
                stopwatch.Start();
                algorithm.Run();
                stopwatch.Stop();
                minitime.Add(stopwatch.Elapsed.TotalMilliseconds);
            }
            //усреднение 5прогонов
            double sum = 0;
            foreach (double k in minitime)
            {
                sum += k;
            }
            sum /= 5;
            
            timeResults.Add(sum);
        }
        //часть с записью результатов в файл
        string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Result");
        Directory.CreateDirectory(folderPath);
        string fileName = $"{algorithm.Name}_{algorithm.Complexity}_results.csv";
        string fullPath = Path.Combine(folderPath, fileName);
        using (StreamWriter file = new StreamWriter(fullPath))
        {
            int n = nstart;
            foreach (double point in timeResults)
            {
                file.WriteLine($"{n};{point.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)}");
                n += 50;
            }
        }
        return timeResults;
    }
}