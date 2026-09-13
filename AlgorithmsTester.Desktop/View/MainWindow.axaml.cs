using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AlgorithmsTester.Desktop.ViewMadels;
using Avalonia.Controls;
using ScottPlot;

namespace AlgorithmsTester.Desktop;

public partial class MainWindow : Window
{
    private ScottPlot.Plottables.Crosshair? _crosshair;
    private readonly List<Action> _activeCleanups = new();

    public MainWindow()
    {
        InitializeComponent();
        var mwvm = new MainWindowViewModel();
        mwvm.OnPlotRequested = ShowGrafic;
        DataContext = mwvm;

        // Подписываемся на событие движения мыши в Avalonia
        BenchmarkPlot.PointerMoved += (sender, e) =>
        {
            if (_crosshair == null) return;
            // 1. Получаем физические координаты курсора внутри контрола
            var pixelPosition = e.GetPosition(BenchmarkPlot);
            Pixel mousePixel = new((float)pixelPosition.X, (float)pixelPosition.Y);
            // 2. Конвертируем пиксели окна в реальные координаты данных (N и Время)
            Coordinates coords = BenchmarkPlot.Plot.GetCoordinates(mousePixel);
            // 3. Обновляем позицию перекрестия
            _crosshair.Position = coords;
            // 4. Перерисовываем холст
            BenchmarkPlot.Refresh();
        };
    }

    /*ГРАФИК*/
    public async void ShowGrafic(AlgorithmCardViewModel Card)
    {
        // 1. Очищаем холст, правила осей и предыдущие подписки
        foreach (var cleanup in _activeCleanups) cleanup();
        _activeCleanups.Clear();

        BenchmarkPlot.Plot.Clear();
        BenchmarkPlot.Plot.Axes.Rules.Clear();

        // 2. Заново создаем перекрестие
        _crosshair = BenchmarkPlot.Plot.Add.Crosshair(0, 0);
        _crosshair.LineWidth = 1;
        _crosshair.LinePattern = LinePattern.Dashed;
        _crosshair.LineColor = Color.FromHex("#77FFFFFF");

        string res = GetResultDir();

        if (Card.Title == "Векторные операции")
        {
            BenchmarkPlot.Plot.Axes.Rules.Add(new ScottPlot.AxisRules.LockedBottom(BenchmarkPlot.Plot.Axes.Left, 0));
            BenchmarkPlot.Plot.Axes.Rules.Add(new ScottPlot.AxisRules.LockedLeft(BenchmarkPlot.Plot.Axes.Bottom, 80000));

            double[] times0 = LoadCsv(Path.Combine(res, "Const_O_1_results.csv"));
            double[] times0theor = LoadCsv(Path.Combine(res, "Const_O_1_Teoreticalresults.csv"));
            double[] times1 = LoadCsv(Path.Combine(res, "Vector_O_n_results.csv"));
            double[] times1theor = LoadCsv(Path.Combine(res, "Vector_O_n_Teoreticalresults.csv"));
            double[] times2 = LoadCsv(Path.Combine(res, "Multiplication_O_1_results.csv"));
            double[] times2theor = LoadCsv(Path.Combine(res, "Multiplication_O_1_Teoreticalresults.csv"));

            double[] times0null = new double[times0.Length];
            double[] times0theornull = new double[times0theor.Length];
            double[] times1null = new double[times1.Length];
            double[] times1theornull = new double[times1theor.Length];
            double[] times2null = new double[times2.Length];
            double[] times2theornull = new double[times2theor.Length];

            // Настройка лимитов камеры ДО анимации
            double maxY = 0.1;
            var allTimes = times0.Concat(times1).Concat(times2).Concat(times0theor).Concat(times1theor).Concat(times2theor).ToList();
            if (allTimes.Count > 0)
            {
                maxY = allTimes.Max();
            }
            BenchmarkPlot.Plot.Axes.SetLimits(80000, 100000, 0, Math.Max(0.01, maxY * 1.15));

            // Сигналы для 3 алгоритмов (Константа, Сумма, Произведение)
            var signal0 = BenchmarkPlot.Plot.Add.Signal(times0null);
            signal0.Data.Period = 50; signal0.Data.XOffset = 80000;
            signal0.Color = Color.FromHex(Card.AlghorithmColor[0]); signal0.LineWidth = 1.5f;

            var signal0theor = BenchmarkPlot.Plot.Add.Signal(times0theornull);
            signal0theor.Data.Period = 50; signal0theor.Data.XOffset = 80000;
            signal0theor.Color = Color.FromHex(Card.Theoretical_AlghorithmColor[0]); signal0theor.LineWidth = 1.5f;

            var signal1 = BenchmarkPlot.Plot.Add.Signal(times1null);
            signal1.Data.Period = 50; signal1.Data.XOffset = 80000;
            signal1.Color = Color.FromHex(Card.AlghorithmColor[1]); signal1.LineWidth = 1.5f;

            var signal1theor = BenchmarkPlot.Plot.Add.Signal(times1theornull);
            signal1theor.Data.Period = 50; signal1theor.Data.XOffset = 80000;
            signal1theor.Color = Color.FromHex(Card.Theoretical_AlghorithmColor[1]); signal1theor.LineWidth = 1.5f;

            var signal2 = BenchmarkPlot.Plot.Add.Signal(times2null);
            signal2.Data.Period = 50; signal2.Data.XOffset = 80000;
            signal2.Color = Color.FromHex(Card.AlghorithmColor[2]); signal2.LineWidth = 1.5f;

            var signal2theor = BenchmarkPlot.Plot.Add.Signal(times2theornull);
            signal2theor.Data.Period = 50; signal2theor.Data.XOffset = 80000;
            signal2theor.Color = Color.FromHex(Card.Theoretical_AlghorithmColor[2]); signal2theor.LineWidth = 1.5f;

            // Связка с кнопками (серия 0, 1, 2)
            WireSeries(Card.SeriesList[0], signal0, signal0theor);
            WireSeries(Card.SeriesList[1], signal1, signal1theor);
            WireSeries(Card.SeriesList[2], signal2, signal2theor);

            // Одновременная плавная анимация всех линий
            int maxLen = Math.Max(times0.Length, Math.Max(times1.Length, times2.Length));
            for (int i = 0; i < maxLen; i++)
            {
                if (i < times0.Length) times0null[i] = times0[i];
                if (i < times0theor.Length) times0theornull[i] = times0theor[i];
                if (i < times1.Length) times1null[i] = times1[i];
                if (i < times1theor.Length) times1theornull[i] = times1theor[i];
                if (i < times2.Length) times2null[i] = times2[i];
                if (i < times2theor.Length) times2theornull[i] = times2theor[i];

                if (i % 10 == 0)
                {
                    await Task.Delay(20);
                    BenchmarkPlot.Refresh();
                }
            }
            BenchmarkPlot.Refresh();
        }
        else if (Card.Title == "Полиномы")
        {
            BenchmarkPlot.Plot.Axes.Rules.Add(new ScottPlot.AxisRules.LockedBottom(BenchmarkPlot.Plot.Axes.Left, 0));
            BenchmarkPlot.Plot.Axes.Rules.Add(new ScottPlot.AxisRules.LockedLeft(BenchmarkPlot.Plot.Axes.Bottom, 50));

            double[] times0 = LoadCsv(Path.Combine(res, "PolynomialNaive_O_n2_results.csv"));
            double[] times0theor = LoadCsv(Path.Combine(res, "PolynomialNaive_O_n2_Teoreticalresults.csv"));
            double[] times1 = LoadCsv(Path.Combine(res, "PolynomialHorner_O_n_results.csv"));
            double[] times1theor = LoadCsv(Path.Combine(res, "PolynomialHorner_O_n_Teoreticalresults.csv"));

            double[] times0null = new double[times0.Length];
            double[] times0theornull = new double[times0theor.Length];
            double[] times1null = new double[times1.Length];
            double[] times1theornull = new double[times1theor.Length];

            // Настройка лимитов камеры ДО анимации
            double maxY = 1.0;
            var allTimes = times0.Concat(times1).Concat(times0theor).Concat(times1theor).ToList();
            if (allTimes.Count > 0)
            {
                maxY = allTimes.Max();
            }
            BenchmarkPlot.Plot.Axes.SetLimits(50, 2000, 0, Math.Max(0.01, maxY * 1.15));

            // Сигналы для 2 алгоритмов полинома (Прямой/Наивный и Метод Горнера)
            var signal0 = BenchmarkPlot.Plot.Add.Signal(times0null);
            signal0.Data.Period = 50; signal0.Data.XOffset = 50;
            signal0.Color = Color.FromHex(Card.AlghorithmColor[0]); signal0.LineWidth = 1.5f;

            var signal0theor = BenchmarkPlot.Plot.Add.Signal(times0theornull);
            signal0theor.Data.Period = 50; signal0theor.Data.XOffset = 50;
            signal0theor.Color = Color.FromHex(Card.Theoretical_AlghorithmColor[0]); signal0theor.LineWidth = 1.5f;

            var signal1 = BenchmarkPlot.Plot.Add.Signal(times1null);
            signal1.Data.Period = 50; signal1.Data.XOffset = 50;
            signal1.Color = Color.FromHex(Card.AlghorithmColor[1]); signal1.LineWidth = 1.5f;

            var signal1theor = BenchmarkPlot.Plot.Add.Signal(times1theornull);
            signal1theor.Data.Period = 50; signal1theor.Data.XOffset = 50;
            signal1theor.Color = Color.FromHex(Card.Theoretical_AlghorithmColor[1]); signal1theor.LineWidth = 1.5f;

            // Связка с кнопками (серия 0 и 1)
            WireSeries(Card.SeriesList[0], signal0, signal0theor);
            WireSeries(Card.SeriesList[1], signal1, signal1theor);

            // Одновременная плавная анимация (в полиномах ~40 точек, обновляем каждые 2 точки)
            int maxLen = Math.Max(times0.Length, times1.Length);
            for (int i = 0; i < maxLen; i++)
            {
                if (i < times0.Length) times0null[i] = times0[i];
                if (i < times0theor.Length) times0theornull[i] = times0theor[i];
                if (i < times1.Length) times1null[i] = times1[i];
                if (i < times1theor.Length) times1theornull[i] = times1theor[i];

                if (i % 2 == 0)
                {
                    await Task.Delay(25);
                    BenchmarkPlot.Refresh();
                }
            }
            BenchmarkPlot.Refresh();
        }
        else if (Card.Title == "Возведение в степень")
        {
            BenchmarkPlot.Plot.Axes.Rules.Add(new ScottPlot.AxisRules.LockedBottom(BenchmarkPlot.Plot.Axes.Left, 0));
            BenchmarkPlot.Plot.Axes.Rules.Add(new ScottPlot.AxisRules.LockedLeft(BenchmarkPlot.Plot.Axes.Bottom, 1000));

            double[] times0 = LoadCsv(Path.Combine(res, "SimplePow_O_n_results.csv"));
            double[] times0theor = LoadCsv(Path.Combine(res, "SimplePow_O_n_Teoreticalresults.csv"));
            double[] times1 = LoadCsv(Path.Combine(res, "RecPow_O_logn_results.csv"));
            double[] times1theor = LoadCsv(Path.Combine(res, "RecPow_O_logn_Teoreticalresults.csv"));
            double[] times2 = LoadCsv(Path.Combine(res, "QuickPow_O_logn_results.csv"));
            double[] times2theor = LoadCsv(Path.Combine(res, "QuickPow_O_logn_Teoreticalresults.csv"));
            double[] times3 = LoadCsv(Path.Combine(res, "QuickPow1_O_logn_results.csv"));
            double[] times3theor = LoadCsv(Path.Combine(res, "QuickPow1_O_logn_Teoreticalresults.csv"));

            double[] times0null = new double[times0.Length];
            double[] times0theornull = new double[times0theor.Length];
            double[] times1null = new double[times1.Length];
            double[] times1theornull = new double[times1theor.Length];
            double[] times2null = new double[times2.Length];
            double[] times2theornull = new double[times2theor.Length];
            double[] times3null = new double[times3.Length];
            double[] times3theornull = new double[times3theor.Length];

            // Настройка лимитов камеры ДО анимации
            double maxY = 0.1;
            var allTimes = times0.Concat(times1).Concat(times2).Concat(times3)
                                 .Concat(times0theor).Concat(times1theor).Concat(times2theor).Concat(times3theor).ToList();
            if (allTimes.Count > 0)
            {
                maxY = allTimes.Max();
            }
            BenchmarkPlot.Plot.Axes.SetLimits(1000, 100000, 0, Math.Max(0.01, maxY * 1.15));

            // Сигналы для алгоритмов возведения в степень
            if (times0.Length > 0 && Card.SeriesList.Count > 0)
            {
                var signal0 = BenchmarkPlot.Plot.Add.Signal(times0null);
                signal0.Data.Period = 50; signal0.Data.XOffset = 1000;
                signal0.Color = Color.FromHex(Card.AlghorithmColor[0]); signal0.LineWidth = 1.5f;

                var signal0theor = BenchmarkPlot.Plot.Add.Signal(times0theornull);
                signal0theor.Data.Period = 50; signal0theor.Data.XOffset = 1000;
                signal0theor.Color = Color.FromHex(Card.Theoretical_AlghorithmColor[0]); signal0theor.LineWidth = 1.5f;

                WireSeries(Card.SeriesList[0], signal0, signal0theor);
            }

            if (times1.Length > 0 && Card.SeriesList.Count > 1)
            {
                var signal1 = BenchmarkPlot.Plot.Add.Signal(times1null);
                signal1.Data.Period = 50; signal1.Data.XOffset = 1000;
                signal1.Color = Color.FromHex(Card.AlghorithmColor[1]); signal1.LineWidth = 1.5f;

                var signal1theor = BenchmarkPlot.Plot.Add.Signal(times1theornull);
                signal1theor.Data.Period = 50; signal1theor.Data.XOffset = 1000;
                signal1theor.Color = Color.FromHex(Card.Theoretical_AlghorithmColor[1]); signal1theor.LineWidth = 1.5f;

                WireSeries(Card.SeriesList[1], signal1, signal1theor);
            }

            if (times2.Length > 0 && Card.SeriesList.Count > 2)
            {
                var signal2 = BenchmarkPlot.Plot.Add.Signal(times2null);
                signal2.Data.Period = 50; signal2.Data.XOffset = 1000;
                signal2.Color = Color.FromHex(Card.AlghorithmColor[2]); signal2.LineWidth = 1.5f;

                var signal2theor = BenchmarkPlot.Plot.Add.Signal(times2theornull);
                signal2theor.Data.Period = 50; signal2theor.Data.XOffset = 1000;
                signal2theor.Color = Color.FromHex(Card.Theoretical_AlghorithmColor[2]); signal2theor.LineWidth = 1.5f;

                WireSeries(Card.SeriesList[2], signal2, signal2theor);
            }

            if (times3.Length > 0 && Card.SeriesList.Count > 3)
            {
                var signal3 = BenchmarkPlot.Plot.Add.Signal(times3null);
                signal3.Data.Period = 50; signal3.Data.XOffset = 1000;
                signal3.Color = Color.FromHex(Card.AlghorithmColor[3]); signal3.LineWidth = 1.5f;

                var signal3theor = BenchmarkPlot.Plot.Add.Signal(times3theornull);
                signal3theor.Data.Period = 50; signal3theor.Data.XOffset = 1000;
                signal3theor.Color = Color.FromHex(Card.Theoretical_AlghorithmColor[3]); signal3theor.LineWidth = 1.5f;

                WireSeries(Card.SeriesList[3], signal3, signal3theor);
            }

            // Одновременная плавная анимация линий (1980 точек, обновляем каждые 25 точек)
            int maxLen = Math.Max(times0.Length, Math.Max(times1.Length, Math.Max(times2.Length, times3.Length)));
            for (int i = 0; i < maxLen; i++)
            {
                if (i < times0.Length) times0null[i] = times0[i];
                if (i < times0theor.Length) times0theornull[i] = times0theor[i];
                if (i < times1.Length) times1null[i] = times1[i];
                if (i < times1theor.Length) times1theornull[i] = times1theor[i];
                if (i < times2.Length) times2null[i] = times2[i];
                if (i < times2theor.Length) times2theornull[i] = times2theor[i];
                if (i < times3.Length) times3null[i] = times3[i];
                if (i < times3theor.Length) times3theornull[i] = times3theor[i];

                if (i % 25 == 0)
                {
                    await Task.Delay(15);
                    BenchmarkPlot.Refresh();
                }
            }
            BenchmarkPlot.Refresh();
        }
        else if (Card.Title == "Матричные операции")
        {
            BenchmarkPlot.Plot.Axes.Rules.Add(new ScottPlot.AxisRules.LockedBottom(BenchmarkPlot.Plot.Axes.Left, 0));
            BenchmarkPlot.Plot.Axes.Rules.Add(new ScottPlot.AxisRules.LockedLeft(BenchmarkPlot.Plot.Axes.Bottom, 10));

            double[] times0 = LoadCsv(Path.Combine(res, "MatrixMultiplication_O_n3_results.csv"));
            double[] times0theor = LoadCsv(Path.Combine(res, "MatrixMultiplication_O_n3_Teoreticalresults.csv"));
            double[] times1 = LoadCsv(Path.Combine(res, "Karatsuba_O_n1_585_results.csv"));
            double[] times1theor = LoadCsv(Path.Combine(res, "Karatsuba_O_n1_585_Teoreticalresults.csv"));
            double[] times2 = LoadCsv(Path.Combine(res, "Levenshtein_O_n2_results.csv"));
            double[] times2theor = LoadCsv(Path.Combine(res, "Levenshtein_O_n2_Teoreticalresults.csv"));

            double[] times0null = new double[times0.Length];
            double[] times0theornull = new double[times0theor.Length];
            double[] times1null = new double[times1.Length];
            double[] times1theornull = new double[times1theor.Length];
            double[] times2null = new double[times2.Length];
            double[] times2theornull = new double[times2theor.Length];

            // Настройка лимитов камеры ДО анимации
            double maxY = 1.0;
            var allTimes = times0.Concat(times1).Concat(times2)
                                 .Concat(times0theor).Concat(times1theor).Concat(times2theor).ToList();
            if (allTimes.Count > 0)
            {
                maxY = allTimes.Max();
            }
            BenchmarkPlot.Plot.Axes.SetLimits(10, 300, 0, Math.Max(0.01, maxY * 1.15));

            if (times0.Length > 0 && Card.SeriesList.Count > 0)
            {
                var signal0 = BenchmarkPlot.Plot.Add.Signal(times0null);
                signal0.Data.Period = 50; signal0.Data.XOffset = 10;
                signal0.Color = Color.FromHex(Card.AlghorithmColor[0]); signal0.LineWidth = 1.5f;

                var signal0theor = BenchmarkPlot.Plot.Add.Signal(times0theornull);
                signal0theor.Data.Period = 50; signal0theor.Data.XOffset = 10;
                signal0theor.Color = Color.FromHex(Card.Theoretical_AlghorithmColor[0]); signal0theor.LineWidth = 1.5f;

                WireSeries(Card.SeriesList[0], signal0, signal0theor);
            }

            if (times1.Length > 0 && Card.SeriesList.Count > 1)
            {
                var signal1 = BenchmarkPlot.Plot.Add.Signal(times1null);
                signal1.Data.Period = 50; signal1.Data.XOffset = 10;
                signal1.Color = Color.FromHex(Card.AlghorithmColor[1]); signal1.LineWidth = 1.5f;

                var signal1theor = BenchmarkPlot.Plot.Add.Signal(times1theornull);
                signal1theor.Data.Period = 50; signal1theor.Data.XOffset = 10;
                signal1theor.Color = Color.FromHex(Card.Theoretical_AlghorithmColor[1]); signal1theor.LineWidth = 1.5f;

                WireSeries(Card.SeriesList[1], signal1, signal1theor);
            }

            if (times2.Length > 0 && Card.SeriesList.Count > 2)
            {
                var signal2 = BenchmarkPlot.Plot.Add.Signal(times2null);
                signal2.Data.Period = 50; signal2.Data.XOffset = 10;
                signal2.Color = Color.FromHex(Card.AlghorithmColor[2]); signal2.LineWidth = 1.5f;

                var signal2theor = BenchmarkPlot.Plot.Add.Signal(times2theornull);
                signal2theor.Data.Period = 50; signal2theor.Data.XOffset = 10;
                signal2theor.Color = Color.FromHex(Card.Theoretical_AlghorithmColor[2]); signal2theor.LineWidth = 1.5f;

                WireSeries(Card.SeriesList[2], signal2, signal2theor);
            }

            int maxLen = Math.Max(times0.Length, Math.Max(times1.Length, times2.Length));
            for (int i = 0; i < maxLen; i++)
            {
                if (i < times0.Length) times0null[i] = times0[i];
                if (i < times0theor.Length) times0theornull[i] = times0theor[i];
                if (i < times1.Length) times1null[i] = times1[i];
                if (i < times1theor.Length) times1theornull[i] = times1theor[i];
                if (i < times2.Length) times2null[i] = times2[i];
                if (i < times2theor.Length) times2theornull[i] = times2theor[i];

                await Task.Delay(40);
                BenchmarkPlot.Refresh();
            }
            BenchmarkPlot.Refresh();
        }
        else
        {
            BenchmarkPlot.Plot.Axes.AutoScale();
            BenchmarkPlot.Refresh();
        }
    }

    private static double[] LoadCsv(string path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return [];
        return File.ReadLines(path)
            .Select(line => line.Split(';'))
            .Where(parts => parts.Length >= 2)
            .Select(parts => double.Parse(parts[1], CultureInfo.InvariantCulture))
            .ToArray();
    }

    private static string GetResultDir()
    {
        string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Result");
        if (!Directory.Exists(dir))
            dir = Path.Combine(Directory.GetCurrentDirectory(), "Result");
        return dir;
    }

    private void WireSeries(AlgorithmSeriesItem series, ScottPlot.Plottables.Signal signal, ScottPlot.Plottables.Signal signalTheor)
    {
        signal.IsVisible = series.IsEnabled;
        signalTheor.IsVisible = series.IsTheoreticalEnabled;

        PropertyChangedEventHandler handler = (sender, e) =>
        {
            if (e.PropertyName == nameof(series.IsEnabled))
            {
                signal.IsVisible = series.IsEnabled;
                BenchmarkPlot.Refresh();
            }
            else if (e.PropertyName == nameof(series.IsTheoreticalEnabled))
            {
                signalTheor.IsVisible = series.IsTheoreticalEnabled;
                BenchmarkPlot.Refresh();
            }
        };

        series.PropertyChanged += handler;
        _activeCleanups.Add(() => series.PropertyChanged -= handler);
    }
}