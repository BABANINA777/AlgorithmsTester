using System.Collections.Generic;
using AlgorithmsTester.Core.Algorithm;

namespace AlgorithmsTester.Core;

public class Programm
{
    public static void Main(string[] args)
    {
        int nstart = 10;
        int nstop = 300;
        //IAlgorithmTemplate algorithm = new VectorAlgorithms();
        //IAlgorithmTemplate algorithm = new ConstAlgorithms();
        //IAlgorithmTemplate algorithm = new MultiplicationAlgorithms();
        //IAlgorithmTemplate algorithm = new NaivePolynomialAlgorithm();
        //IAlgorithmTemplate algorithm = new HornerPolynomialAlgorithm();
        //IAlgorithmTemplate algorithm = new SimplePowAlgorithm();
        //IAlgorithmTemplate algorithm = new RecPowAlgorithm();
        IAlgorithmTemplate algorithm = new KaratsubaAlgorithm();

        BenchmarkEngine bench = new BenchmarkEngine(nstart, nstop, algorithm);
        List<double> realtime = bench.AlgorithmTimer();
        TheoreticalFitter fitter = new TheoreticalFitter(algorithm);
        fitter.TeoreticalAlgorithmTimer(nstart, nstop, realtime);
    }
}