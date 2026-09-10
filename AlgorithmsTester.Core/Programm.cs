using AlgorithmsTester.Core.Algorithm;

namespace AlgorithmsTester.Core;

public class Programm
{
    public static  void Main(string[] args)
    {
        int nstart = 80000;
        int nstop = 100000;
        IAlgorithmTemplate algorithm = new VectorAlgorithms();
        BenchmarkEngine bench = new BenchmarkEngine(nstart, nstop, algorithm);
        List<double> realtime = bench.AlgorithmTimer();
        TheoreticalFitter fitter = new TheoreticalFitter(algorithm);
        fitter.TeoreticalAlgorithmTimer(nstart, nstop, realtime);
    }
}