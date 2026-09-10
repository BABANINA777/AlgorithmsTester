namespace AlgorithmsTester.Core;

public class TheoreticalFitter
{
    IAlgorithmTemplate _template;
    public TheoreticalFitter(IAlgorithmTemplate template)
    {
        _template = template;
    }

    private double GetTheoreticalG(double n)// метод для помощи с вычислением C
    {
        return _template.Complexity switch
        {
            Complexity.O_1 => 1.0,
            Complexity.O_logn => Math.Log2(n),
            Complexity.O_n => n,
            Complexity.O_n_logn => n * Math.Log2(n),
            Complexity.O_n1_585 => Math.Pow(n, 1.585),
            Complexity.O_n2 => n * n,
            Complexity.O_n3 => n * n * n,
            _ => n
        };
    }
    
    public void TeoreticalAlgorithmTimer(int nstart, int nstop, List<double> realTimes)
    {
        // вычисление C
        double numerator = 0;
        double denominator = 0;
        int n = nstart;
        for (int i = 0; i < realTimes.Count; i++)
        {
            numerator += realTimes[i] * GetTheoreticalG(n);
            denominator += GetTheoreticalG(n)*GetTheoreticalG(n);
            n += 50;
        }
        double C = numerator / denominator;
        
        //часть с записью результатов в файл и созданием теоретических точек
        string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Result");
        Directory.CreateDirectory(folderPath);
        string fileName = $"{_template.Name}_{_template.Complexity}_Teoreticalresults.csv";
        string fullPath = Path.Combine(folderPath, fileName);
        using (StreamWriter file = new StreamWriter(fullPath))
        {
            n = nstart;
            for (int i = 0; i < realTimes.Count; i++)
            {
                double gn = GetTheoreticalG(n);
                double tTeor = C * gn; // теоретическое время для точки
                file.WriteLine($"{n};{tTeor.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)}");
                n += 50;
            }
        }
    }
}