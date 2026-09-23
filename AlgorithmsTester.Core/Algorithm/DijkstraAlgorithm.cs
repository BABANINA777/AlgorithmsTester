using System;

namespace AlgorithmsTester.Core.Algorithm;

public class DijkstraAlgorithm : IAlgorithmTemplate
{
    public string Name { get; } = "Dijkstra";
    public Complexity Complexity { get; } = Complexity.O_n2;

    private double[,]? _graph;
    private int _verticesCount;

    public Array PrepareData(int n)
    {
        _verticesCount = n;
        _graph = GenerateGraph(n);

        return _graph;
    }

    public void Run()
    {
        if (_verticesCount == 0 || _graph is null)
            return;

        Dijkstra(_graph, 0);
    }

    private static double[,] GenerateGraph(int n)
    {
        Random random = new Random(42);

        double[,] graph = new double[n, n];

        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                if (i == j)
                {
                    graph[i, j] = 0;
                }
                else
                {
                    // Не каждую вершину соединяем с каждой.
                    // Примерно 30% возможных рёбер.
                    if (random.NextDouble() < 0.3)
                        graph[i, j] = random.Next(1, 100);
                    else
                        graph[i, j] = double.PositiveInfinity;
                }
            }
        }

        return graph;
    }

    private static double[] Dijkstra(double[,] graph, int start)
    {
        int n = graph.GetLength(0);

        double[] distances = new double[n];
        bool[] visited = new bool[n];

        for (int i = 0; i < n; i++)
        {
            distances[i] = double.PositiveInfinity;
        }

        distances[start] = 0;

        for (int count = 0; count < n; count++)
        {
            int current = -1;
            double minDistance = double.PositiveInfinity;

            // Ищем ещё не посещённую вершину
            // с минимальным расстоянием.
            for (int i = 0; i < n; i++)
            {
                if (!visited[i] && distances[i] < minDistance)
                {
                    minDistance = distances[i];
                    current = i;
                }
            }

            if (current == -1)
                break;

            visited[current] = true;

            // Обновляем расстояния до соседей.
            for (int neighbour = 0; neighbour < n; neighbour++)
            {
                if (visited[neighbour])
                    continue;

                if (double.IsPositiveInfinity(graph[current, neighbour]))
                    continue;

                double newDistance =
                    distances[current] + graph[current, neighbour];

                if (newDistance < distances[neighbour])
                {
                    distances[neighbour] = newDistance;
                }
            }
        }

        return distances;
    }
}   