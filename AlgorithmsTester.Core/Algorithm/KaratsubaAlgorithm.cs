using System;

namespace AlgorithmsTester.Core.Algorithm;

public class KaratsubaAlgorithm : IAlgorithmTemplate
{
    public string Name { get; } = "Karatsuba";
    public Complexity Complexity { get; } = Complexity.O_n1_585;

    private int[] _a = [];
    private int[] _b = [];

    public Array PrepareData(int n)
    {
        Random rnd = new Random(42);
        _a = new int[n];
        _b = new int[n];
        for (int i = 0; i < n; i++)
        {
            _a[i] = rnd.Next(1, 10);
            _b[i] = rnd.Next(1, 10);
        }
        return _a;
    }

    public void Run()
    {
        Multiply(_a, _b);
    }

    public static int[] Multiply(int[] x, int[] y)
    {
        int len = Math.Max(x.Length, y.Length);
        if (len <= 4)
        {
            int[] res = new int[x.Length + y.Length];
            for (int i = 0; i < x.Length; i++)
            {
                for (int j = 0; j < y.Length; j++)
                {
                    res[i + j] += x[i] * y[j];
                }
            }
            return res;
        }

        int m = len / 2;

        int[] x0 = x[..Math.Min(m, x.Length)];
        int[] x1 = m < x.Length ? x[m..] : [];

        int[] y0 = y[..Math.Min(m, y.Length)];
        int[] y1 = m < y.Length ? y[m..] : [];

        int[] xSum = Add(x0, x1);
        int[] ySum = Add(y0, y1);

        int[] z0 = Multiply(x0, y0);
        int[] z2 = Multiply(x1, y1);
        int[] zMid = Multiply(xSum, ySum);

        int[] z1 = Subtract(Subtract(zMid, z2), z0);

        int[] result = new int[x.Length + y.Length];
        for (int i = 0; i < z0.Length; i++) result[i] += z0[i];
        for (int i = 0; i < z1.Length; i++) result[i + m] += z1[i];
        for (int i = 0; i < z2.Length; i++) result[i + 2 * m] += z2[i];

        return result;
    }

    private static int[] Add(int[] a, int[] b)
    {
        int maxLen = Math.Max(a.Length, b.Length);
        int[] res = new int[maxLen];
        for (int i = 0; i < maxLen; i++)
        {
            int valA = i < a.Length ? a[i] : 0;
            int valB = i < b.Length ? b[i] : 0;
            res[i] = valA + valB;
        }
        return res;
    }

    private static int[] Subtract(int[] a, int[] b)
    {
        int maxLen = Math.Max(a.Length, b.Length);
        int[] res = new int[maxLen];
        for (int i = 0; i < maxLen; i++)
        {
            int valA = i < a.Length ? a[i] : 0;
            int valB = i < b.Length ? b[i] : 0;
            res[i] = valA - valB;
        }
        return res;
    }
}
