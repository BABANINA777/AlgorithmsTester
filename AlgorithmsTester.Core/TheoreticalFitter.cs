namespace AlgorithmsTester.Core;

public class TheoreticalFitter
{
    private readonly IAlgorithmTemplate _template;

    public TheoreticalFitter(IAlgorithmTemplate template)
    {
        _template = template;
    }

    private double GetTheoreticalG(double n)
    {
        return _template.Complexity switch
        {
            Complexity.O_1 => 1.0,
            Complexity.O_logn => n <= 1.0 ? 0.0 : Math.Log2(n),
            Complexity.O_n => n,
            Complexity.O_n_logn => n <= 1.0 ? 0.0 : n * Math.Log2(n),
            Complexity.O_n1_585 => Math.Pow(n, 1.585),
            Complexity.O_n2 => n * n,
            Complexity.O_n3 => n * n * n,
            _ => n
        };
    }

    public double TeoreticalAlgorithmTimer(int nStart, int nStop, List<double> realTimes, int step = 50)
    {
        double numerator = 0;
        double denominator = 0;
        int n = nStart;

        for (int i = 0; i < realTimes.Count; i++)
        {
            double gn = GetTheoreticalG(n);
            numerator += realTimes[i] * gn;
            denominator += gn * gn;
            n += step;
        }

        double C = denominator != 0 ? numerator / denominator : 0;

        // Расчёт MSE и запись в CSV
        string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Result");
        Directory.CreateDirectory(folderPath);
        string fileName = string.Format("{0}_{1}_Teoreticalresults.csv", _template.Name, _template.Complexity);
        string fullPath = Path.Combine(folderPath, fileName);

        double sumSquaredErrors = 0;

        using (StreamWriter file = new StreamWriter(fullPath))
        {
            n = nStart;
            for (int i = 0; i < realTimes.Count; i++)
            {
                double gn = GetTheoreticalG(n);
                double tTeor = C * gn;

                double diff = realTimes[i] - tTeor;
                sumSquaredErrors += diff * diff;

                file.WriteLine(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0};{1:F8}", n, tTeor));
                n += step;
            }
        }

        double mse = realTimes.Count > 0 ? sumSquaredErrors / realTimes.Count : 0;
        return mse;
    }
}