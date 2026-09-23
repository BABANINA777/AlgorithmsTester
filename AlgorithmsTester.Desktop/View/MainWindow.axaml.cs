using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AlgorithmsTester.Core;
using AlgorithmsTester.Core.Algorithm;
using AlgorithmsTester.Desktop.ViewMadels;
using Avalonia.Controls;
using ScottPlot;

namespace AlgorithmsTester.Desktop;

public partial class MainWindow : Window
{
    private ScottPlot.Plottables.Crosshair? _crosshair;
    private readonly List<Action> _activeCleanups = new();
    private readonly Dictionary<long, ScottPlot.Plottables.Scatter> _activeHistoryPlots = new();
    private MainWindowViewModel? _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        var mwvm = new MainWindowViewModel();
        mwvm.OnPlotRequested = ShowGrafic;
        DataContext = mwvm;
        _viewModel = mwvm;

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
        _activeHistoryPlots.Clear();

        if (_viewModel != null)
        {
            for (int i = 0; i < _viewModel.HistoryRuns.Count; i++)
            {
                _viewModel.HistoryRuns[i].IsDisplayed = false;
            }
        }

        BenchmarkPlot.Plot.Clear();
        BenchmarkPlot.Plot.Axes.Rules.Clear();

        // 2. Заново создаем перекрестие
        _crosshair = BenchmarkPlot.Plot.Add.Crosshair(0, 0);
        _crosshair.LineWidth = 1;
        _crosshair.LinePattern = LinePattern.Dashed;
        _crosshair.LineColor = Color.FromHex("#77FFFFFF");

        // Настраиваем нижнюю панель под открытую карточку
        AlgorithmSelector.ItemsSource = Card.Names;
        if (Card.Names.Length > 0)
        {
            AlgorithmSelector.SelectedIndex = 0;
        }
        NStartInput.Text = Card.DefaultStartN.ToString();
        NStopInput.Text = Card.DefaultEndN.ToString();
        RepeatsInput.Text = "5";
        UpdateHistoryButtonState();

        string res = GetResultDir();

        if (Card.Title == "Векторные операции")
        {
            BenchmarkPlot.Plot.Axes.Rules.Add(new ScottPlot.AxisRules.LockedBottom(BenchmarkPlot.Plot.Axes.Left, 0));

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
        else if (Card.Title == "Сортировки")
        {
            BenchmarkPlot.Plot.Axes.Rules.Add(new ScottPlot.AxisRules.LockedBottom(BenchmarkPlot.Plot.Axes.Left, 0));

            var (off0, per0, times0) = LoadCsvSignal(Path.Combine(res, "BubbleSort_O_n2_results.csv"), 2000, 50);
            var (_, _, times0theor) = LoadCsvSignal(Path.Combine(res, "BubbleSort_O_n2_Teoreticalresults.csv"), 2000, 50);

            var (off1, per1, times1) = LoadCsvSignal(Path.Combine(res, "QuickSort_O_n_logn_results.csv"), 2000, 50);
            var (_, _, times1theor) = LoadCsvSignal(Path.Combine(res, "QuickSort_O_n_logn_Teoreticalresults.csv"), 2000, 50);

            var (off2, per2, times2) = LoadCsvSignal(Path.Combine(res, "Timsort_O_n_logn_results.csv"), 2000, 50);
            var (_, _, times2theor) = LoadCsvSignal(Path.Combine(res, "Timsort_O_n_logn_Teoreticalresults.csv"), 2000, 50);

            double[] times0null = new double[times0.Length];
            double[] times0theornull = new double[times0theor.Length];
            double[] times1null = new double[times1.Length];
            double[] times1theornull = new double[times1theor.Length];
            double[] times2null = new double[times2.Length];
            double[] times2theornull = new double[times2theor.Length];

            double minX = Math.Min(off0, Math.Min(off1, off2));
            double maxX = Math.Max(off0 + (times0.Length > 0 ? (times0.Length - 1) * per0 : 0),
                          Math.Max(off1 + (times1.Length > 0 ? (times1.Length - 1) * per1 : 0),
                                   off2 + (times2.Length > 0 ? (times2.Length - 1) * per2 : 0)));
            if (maxX <= minX) maxX = minX + 3000;

            BenchmarkPlot.Plot.Axes.Rules.Add(new ScottPlot.AxisRules.LockedLeft(BenchmarkPlot.Plot.Axes.Bottom, minX));

            // Настройка лимитов камеры ДО анимации
            double maxY = 1.0;
            var allTimes = times0.Concat(times1).Concat(times2)
                                 .Concat(times0theor).Concat(times1theor).Concat(times2theor).ToList();
            if (allTimes.Count > 0)
            {
                maxY = allTimes.Max();
            }
            BenchmarkPlot.Plot.Axes.SetLimits(minX, maxX, 0, Math.Max(0.01, maxY * 1.15));

            if (times0.Length > 0 && Card.SeriesList.Count > 0)
            {
                var signal0 = BenchmarkPlot.Plot.Add.Signal(times0null);
                signal0.Data.Period = per0; signal0.Data.XOffset = off0;
                signal0.Color = Color.FromHex(Card.AlghorithmColor[0]); signal0.LineWidth = 1.5f;

                var signal0theor = BenchmarkPlot.Plot.Add.Signal(times0theornull);
                signal0theor.Data.Period = per0; signal0theor.Data.XOffset = off0;
                signal0theor.Color = Color.FromHex(Card.Theoretical_AlghorithmColor[0]); signal0theor.LineWidth = 1.5f;

                WireSeries(Card.SeriesList[0], signal0, signal0theor);
            }

            if (times1.Length > 0 && Card.SeriesList.Count > 1)
            {
                var signal1 = BenchmarkPlot.Plot.Add.Signal(times1null);
                signal1.Data.Period = per1; signal1.Data.XOffset = off1;
                signal1.Color = Color.FromHex(Card.AlghorithmColor[1]); signal1.LineWidth = 1.5f;

                var signal1theor = BenchmarkPlot.Plot.Add.Signal(times1theornull);
                signal1theor.Data.Period = per1; signal1theor.Data.XOffset = off1;
                signal1theor.Color = Color.FromHex(Card.Theoretical_AlghorithmColor[1]); signal1theor.LineWidth = 1.5f;

                WireSeries(Card.SeriesList[1], signal1, signal1theor);
            }

            if (times2.Length > 0 && Card.SeriesList.Count > 2)
            {
                var signal2 = BenchmarkPlot.Plot.Add.Signal(times2null);
                signal2.Data.Period = per2; signal2.Data.XOffset = off2;
                signal2.Color = Color.FromHex(Card.AlghorithmColor[2]); signal2.LineWidth = 1.5f;

                var signal2theor = BenchmarkPlot.Plot.Add.Signal(times2theornull);
                signal2theor.Data.Period = per2; signal2theor.Data.XOffset = off2;
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

                if (i % 2 == 0)
                {
                    await Task.Delay(20);
                    BenchmarkPlot.Refresh();
                }
            }
            BenchmarkPlot.Refresh();
        }
        else if (Card.Title == "Матричные операции")
        {
            BenchmarkPlot.Plot.Axes.Rules.Add(new ScottPlot.AxisRules.LockedBottom(BenchmarkPlot.Plot.Axes.Left, 0));

            var (off0, per0, times0) = LoadCsvSignal(Path.Combine(res, "MatrixMultiplication_O_n3_results.csv"), 10, 50);
            var (_, _, times0theor) = LoadCsvSignal(Path.Combine(res, "MatrixMultiplication_O_n3_Teoreticalresults.csv"), 10, 50);

            var (off1, per1, times1) = LoadCsvSignal(Path.Combine(res, "Karatsuba_O_n1_585_results.csv"), 10, 50);
            var (_, _, times1theor) = LoadCsvSignal(Path.Combine(res, "Karatsuba_O_n1_585_Teoreticalresults.csv"), 10, 50);

            var (off2, per2, times2) = LoadCsvSignal(Path.Combine(res, "Dijkstra_O_n2_results.csv"), 10, 50);
            var (_, _, times2theor) = LoadCsvSignal(Path.Combine(res, "Dijkstra_O_n2_Teoreticalresults.csv"), 10, 50);

            double[] times0null = new double[times0.Length];
            double[] times0theornull = new double[times0theor.Length];
            double[] times1null = new double[times1.Length];
            double[] times1theornull = new double[times1theor.Length];
            double[] times2null = new double[times2.Length];
            double[] times2theornull = new double[times2theor.Length];

            double minX = Math.Min(off0, Math.Min(off1, off2));
            double maxX = Math.Max(off0 + (times0.Length > 0 ? (times0.Length - 1) * per0 : 0),
                          Math.Max(off1 + (times1.Length > 0 ? (times1.Length - 1) * per1 : 0),
                                   off2 + (times2.Length > 0 ? (times2.Length - 1) * per2 : 0)));
            if (maxX <= minX) maxX = minX + 300;

            BenchmarkPlot.Plot.Axes.Rules.Add(new ScottPlot.AxisRules.LockedLeft(BenchmarkPlot.Plot.Axes.Bottom, minX));

            // Настройка лимитов камеры ДО анимации
            double maxY = 1.0;
            var allTimes = times0.Concat(times1).Concat(times2)
                                 .Concat(times0theor).Concat(times1theor).Concat(times2theor).ToList();
            if (allTimes.Count > 0)
            {
                maxY = allTimes.Max();
            }
            BenchmarkPlot.Plot.Axes.SetLimits(minX, maxX, 0, Math.Max(0.01, maxY * 1.15));

            if (times0.Length > 0 && Card.SeriesList.Count > 0)
            {
                var signal0 = BenchmarkPlot.Plot.Add.Signal(times0null);
                signal0.Data.Period = per0; signal0.Data.XOffset = off0;
                signal0.Color = Color.FromHex(Card.AlghorithmColor[0]); signal0.LineWidth = 1.5f;

                var signal0theor = BenchmarkPlot.Plot.Add.Signal(times0theornull);
                signal0theor.Data.Period = per0; signal0theor.Data.XOffset = off0;
                signal0theor.Color = Color.FromHex(Card.Theoretical_AlghorithmColor[0]); signal0theor.LineWidth = 1.5f;

                WireSeries(Card.SeriesList[0], signal0, signal0theor);
            }

            if (times1.Length > 0 && Card.SeriesList.Count > 1)
            {
                var signal1 = BenchmarkPlot.Plot.Add.Signal(times1null);
                signal1.Data.Period = per1; signal1.Data.XOffset = off1;
                signal1.Color = Color.FromHex(Card.AlghorithmColor[1]); signal1.LineWidth = 1.5f;

                var signal1theor = BenchmarkPlot.Plot.Add.Signal(times1theornull);
                signal1theor.Data.Period = per1; signal1theor.Data.XOffset = off1;
                signal1theor.Color = Color.FromHex(Card.Theoretical_AlghorithmColor[1]); signal1theor.LineWidth = 1.5f;

                WireSeries(Card.SeriesList[1], signal1, signal1theor);
            }

            if (times2.Length > 0 && Card.SeriesList.Count > 2)
            {
                var signal2 = BenchmarkPlot.Plot.Add.Signal(times2null);
                signal2.Data.Period = per2; signal2.Data.XOffset = off2;
                signal2.Color = Color.FromHex(Card.AlghorithmColor[2]); signal2.LineWidth = 1.5f;

                var signal2theor = BenchmarkPlot.Plot.Add.Signal(times2theornull);
                signal2theor.Data.Period = per2; signal2theor.Data.XOffset = off2;
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

    private static (double xOffset, double period, double[] ys) LoadCsvSignal(string path, double defaultOffset = 50, double defaultPeriod = 50)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return (defaultOffset, defaultPeriod, []);

        List<double> xs = new List<double>();
        List<double> ys = new List<double>();

        foreach (string line in File.ReadLines(path))
        {
            string[] parts = line.Split(';');
            if (parts.Length >= 2)
            {
                if (double.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out double x) &&
                    double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out double y))
                {
                    xs.Add(x);
                    ys.Add(y);
                }
            }
        }

        if (ys.Count == 0) return (defaultOffset, defaultPeriod, []);

        double offset = xs[0];
        double period = xs.Count > 1 ? (xs[1] - xs[0]) : defaultPeriod;
        return (offset, period, ys.ToArray());
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
        if (Directory.Exists(dir))
            return dir;

        string coreDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\AlgorithmsTester.Core\bin\Debug\net10.0\Result"));
        if (Directory.Exists(coreDir))
            return coreDir;

        return Path.Combine(Directory.GetCurrentDirectory(), "Result");
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

    // Сопоставление понятного имени из интерфейса с реальным классом алгоритма
    private IAlgorithmTemplate? CreateAlgorithmByName(string name)
    {
        if (name == "Константа") return new ConstAlgorithms();
        if (name == "Сумма") return new VectorAlgorithms();
        if (name == "Произведение") return new MultiplicationAlgorithms();
        if (name == "Прямой (наивный)") return new NaivePolynomialAlgorithm();
        if (name == "Метод Горнера") return new HornerPolynomialAlgorithm();
        if (name == "Простой") return new SimplePowAlgorithm();
        if (name == "Рекурсивный") return new RecursiveLinearPowAlgorithm();
        if (name == "Быстрый бинарный" || name == "Быстрый классический") return new QuickPowAlgorithm();
        if (name == "Пузырьковая") return new BubbleSortAlgorithm();
        if (name == "Быстрая (QuickSort)") return new QuickSortAlgorithm();
        if (name == "Timsort") return new TimsortAlgorithm();
        if (name == "Умножение матриц") return new MatrixMultiplicationAlgorithm();
        if (name == "Алгоритм Карацубы") return new KaratsubaAlgorithm();
        if (name == "Алгоритм Дейкстры") return new DijkstraAlgorithm();
        return null;
    }

    // Обработчик нажатия на кнопку "Сделать прогон"
    private async void RunBenchmarkButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_viewModel == null || _viewModel.SelectedCard == null)
        {
            return;
        }

        // 1. Проверяем выбор алгоритма
        string? selectedAlgoName = AlgorithmSelector.SelectedItem as string;
        if (string.IsNullOrEmpty(selectedAlgoName))
        {
            return;
        }

        // 2. Проверяем числовые поля
        int nStart;
        if (!int.TryParse(NStartInput.Text, out nStart) || nStart <= 0)
        {
            return;
        }

        int nStop;
        if (!int.TryParse(NStopInput.Text, out nStop) || nStop < nStart)
        {
            return;
        }

        int step = 50;

        int repeats;
        if (!int.TryParse(RepeatsInput.Text, out repeats) || repeats <= 0)
        {
            repeats = 5;
        }

        // 3. Создаем объект алгоритма
        IAlgorithmTemplate? algo = CreateAlgorithmByName(selectedAlgoName);
        if (algo == null)
        {
            return;
        }

        // Блокируем кнопку, чтобы не запускали несколько раз одновременно
        RunBenchmarkButton.IsEnabled = false;

        try
        {
            // 4. Запускаем бенчмарк (в фоновом потоке, чтобы окно не зависало)
            BenchmarkEngine engine = new BenchmarkEngine(nStart, nStop, step, repeats, algo);
            List<double> results = await Task.Run(engine.AlgorithmTimer);

            // 4.1 Считаем теоретическую аппроксимацию (МНК) и сохраняем файл
            TheoreticalFitter fitter = new TheoreticalFitter(algo);
            fitter.TeoreticalAlgorithmTimer(nStart, nStop, results, step);

            // 5. Сохраняем в базу данных SQLite
            long expId = DatabaseManager.SaveExperimentRun(
                _viewModel.SelectedCard.Title,
                algo,
                nStart,
                nStop,
                step,
                repeats,
                results);

            // 6. Добавляем в историю в MVVM (сразу появится в выпадающем списке)
            RunHistoryItem newItem = new RunHistoryItem();
            newItem.Id = expId;
            newItem.AlgorithmName = algo.Name;
            newItem.GroupName = _viewModel.SelectedCard.Title;
            newItem.ExperimentDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            newItem.NStart = nStart;
            newItem.NStop = nStop;
            newItem.Step = step;
            newItem.RunsCount = repeats;

            _viewModel.HistoryRuns.Insert(0, newItem);
            _viewModel.SelectedHistoryRun = newItem;
            UpdateHistoryButtonState();
        }
        finally
        {
            RunBenchmarkButton.IsEnabled = true;
        }
    }

    // Обновление состояния кнопки "Показать / Скрыть" в зависимости от выбранного элемента
    private void UpdateHistoryButtonState()
    {
        RunHistoryItem? item = HistoryRunsSelector.SelectedItem as RunHistoryItem;
        if (item == null)
        {
            LoadHistoryButton.IsEnabled = false;
            LoadHistoryButtonText.Text = "Показать";
            LoadHistoryButton.Background = Avalonia.Media.Brush.Parse("#2563EB");
            return;
        }

        LoadHistoryButton.IsEnabled = true;
        if (item.IsDisplayed)
        {
            LoadHistoryButtonText.Text = "Скрыть";
            LoadHistoryButton.Background = Avalonia.Media.Brush.Parse("#DC2626"); // Красный
        }
        else
        {
            LoadHistoryButtonText.Text = "Показать";
            LoadHistoryButton.Background = Avalonia.Media.Brush.Parse("#2563EB"); // Синий
        }
    }

    // Событие при смене выбранного прогона в выпадающем списке
    private void HistoryRunsSelector_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        UpdateHistoryButtonState();
    }

    // Обработчик нажатия на кнопку "Показать / Скрыть"
    private void LoadHistoryButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        RunHistoryItem? item = HistoryRunsSelector.SelectedItem as RunHistoryItem;
        if (item == null)
        {
            return;
        }

        if (!item.IsDisplayed)
        {
            // 1. Читаем точки из базы по ID прогона
            var plotData = DatabaseManager.GetRunPlotData(item.Id);
            if (plotData.xs.Length == 0)
            {
                return;
            }

            // 2. Добавляем Scatter на график
            var scatter = BenchmarkPlot.Plot.Add.Scatter(plotData.xs, plotData.ys);
            scatter.LegendText = item.AlgorithmName + " (" + item.ExperimentDate + ")";
            scatter.LineWidth = 2.0f;
            scatter.MarkerSize = 5;
            scatter.LinePattern = LinePattern.DenselyDashed;

            // 3. Сохраняем ссылку в словарь и ставим флаг
            _activeHistoryPlots[item.Id] = scatter;
            item.IsDisplayed = true;

            // 4. Автомасштаб и перерисовка
            BenchmarkPlot.Plot.Axes.AutoScale();
            BenchmarkPlot.Refresh();

            // 5. Меняем состояние кнопки
            UpdateHistoryButtonState();
        }
        else
        {
            // 1. Ищем линию в активных и удаляем с графика
            if (_activeHistoryPlots.TryGetValue(item.Id, out var scatter))
            {
                BenchmarkPlot.Plot.Remove(scatter);
                _activeHistoryPlots.Remove(item.Id);
            }

            item.IsDisplayed = false;

            // 2. Перерисовываем холст
            BenchmarkPlot.Refresh();

            // 3. Меняем состояние кнопки
            UpdateHistoryButtonState();
        }
    }
}