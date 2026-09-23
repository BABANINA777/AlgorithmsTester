using System.Collections.Generic;
using AlgorithmsTester.Core.Algorithm;

namespace AlgorithmsTester.Core;

public class Programm
{
    public static void Main(string[] args)
    {
        int nstart = 10;
        int nstop = 300;
        //var algorithm = new VectorAlgorithms();
        //var algorithm = new ConstAlgorithms();
        //var algorithm = new MultiplicationAlgorithms();
        //var algorithm = new NaivePolynomialAlgorithm();
        //var algorithm = new HornerPolynomialAlgorithm();
        //var algorithm = new SimplePowAlgorithm();
        //var algorithm = new RecPowAlgorithm();
        //var algorithm = new DijkstraAlgorithm();
        var algorithm = new KaratsubaAlgorithm();

        var benchmark = new BenchmarkEngine(nstart, nstop, algorithm);
        List<double> realtime = benchmark.AlgorithmTimer();
        TheoreticalFitter fitter = new TheoreticalFitter(algorithm);
        fitter.TeoreticalAlgorithmTimer(nstart, nstop, realtime);
    }
}