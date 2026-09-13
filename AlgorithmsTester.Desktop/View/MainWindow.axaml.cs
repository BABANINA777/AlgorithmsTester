using System;
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
    public MainWindow()
    {
        InitializeComponent();
        var mwvm = new MainWindowViewModel();
        mwvm.OnPlotRequested = ShowGrafic;
        DataContext = mwvm;

        //перикрестие в центре
        var crosshair = BenchmarkPlot.Plot.Add.Crosshair(0, 0);
        crosshair.LineWidth = 1;
        crosshair.LinePattern = LinePattern.Dashed;
        // Подписываемся на событие движения мыши в Avalonia
        BenchmarkPlot.PointerMoved += (sender, e) =>
        {
            // 1. Получаем физические координаты курсора внутри контрола
            var pixelPosition = e.GetPosition(BenchmarkPlot);
            Pixel mousePixel = new((float)pixelPosition.X, (float)pixelPosition.Y);
            // 2. Конвертируем пиксели окна в реальные координаты данных (N и Время)
            Coordinates coords = BenchmarkPlot.Plot.GetCoordinates(mousePixel);
            // 3. Обновляем позицию перекрестия
            crosshair.Position = coords;
            // 4. Перерисовываем холст
            BenchmarkPlot.Refresh();
        };

    }

    /*ГРАФИК*/
    public async void ShowGrafic(AlgorithmCardViewModel Card)
    {
        BenchmarkPlot.Plot.Clear();
        
        // заново создаем курсор
        var crosshair = BenchmarkPlot.Plot.Add.Crosshair(0, 0);
        crosshair.LineWidth = 1;
        crosshair.LinePattern = LinePattern.Dashed;

        // 2. Запрещает смещать график левее и ниже диапазона
        BenchmarkPlot.Plot.Axes.Rules.Add(
        new ScottPlot.AxisRules.LockedBottom(BenchmarkPlot.Plot.Axes.Left, 0));
        BenchmarkPlot.Plot.Axes.Rules.Add(
            new ScottPlot.AxisRules.LockedLeft(BenchmarkPlot.Plot.Axes.Bottom, 80000));
        
        if (Card.Title == "Векторные операции")
        {
            BenchmarkPlot.Plot.Axes.AutoScale();
            BenchmarkPlot.Plot.Axes.SetLimits(
                left: 80000,
                bottom: 0
            );
            /*Алгоритм 0*/


            /*Алгоритм 1*/
            var path1 = Path.Join(Directory.GetCurrentDirectory(), "Result", "Vector_O_n_results.csv");
            var path2 = Path.Join(Directory.GetCurrentDirectory(), "Result", "Vector_O_n_Teoreticalresults.csv");

            //читаем массив из файла
            double[] times1 = File.ReadLines(path1)
                .Select(line => line.Split(';'))
                .Select(parts => double.Parse(parts[1], CultureInfo.InvariantCulture))
                .ToArray();
            double[] times1null = new double[times1.Length];
            double[] times1theor = File.ReadLines(path2)
                .Select(line => line.Split(';'))
                .Select(parts => double.Parse(parts[1], CultureInfo.InvariantCulture))
                .ToArray();
            double[] times1teornull = new double[times1theor.Length];


            //создаем график на пустом массиве
            var signal1 = BenchmarkPlot.Plot.Add.Signal(times1null);
            signal1.Data.Period = 50;
            signal1.Data.XOffset = 80000;
            signal1.Color = Color.FromHex(Card.AlghorithmColor[1]);
            signal1.LineWidth = 1.5f;
            var signal1theor = BenchmarkPlot.Plot.Add.Signal(times1teornull);
            signal1theor.Data.Period = 50;
            signal1theor.Data.XOffset = 80000;
            signal1theor.Color = Color.FromHex(Card.Theoretical_AlghorithmColor[1]);
            signal1theor.LineWidth = 1.5f;

            //анимированная отрисовка графика
            for (int i = 0; i < times1.Length; i++)
            {
                times1null[i] = times1[i];
                times1teornull[i] = times1theor[i];

                if (i % 10 == 0)
                {
                    await Task.Delay(20);
                    BenchmarkPlot.Refresh();
                }
            }

            //при изменении IsEnabled(зависимой от тоглбатон) вызовется событие PropertyChanged которое изменит визибл графика
            var series1 = Card.SeriesList[1];
            series1.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(series1.IsEnabled))
                {
                    signal1.IsVisible = series1.IsEnabled; // скрываем или показываем линию
                    BenchmarkPlot.Refresh();             // обновляем экран
                }
            };
            var series1theor = Card.SeriesList[1];
            series1theor.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(series1theor.IsTheoreticalEnabled))
                {
                    signal1theor.IsVisible = series1theor.IsTheoreticalEnabled;
                    BenchmarkPlot.Refresh();
                }
            };

            /*Алгоритм 2*/




            BenchmarkPlot.Refresh();
        }
    }
}