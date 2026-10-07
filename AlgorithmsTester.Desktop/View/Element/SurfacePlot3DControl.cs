using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace AlgorithmsTester.Desktop.Element;

/// <summary>
/// Информация о выбранной пользователем точке замеров для отображения подсказки с координатами.
/// </summary>
public class SelectedPointInfo
{
    public string SeriesTitle { get; set; } = "";
    public int N { get; set; }
    public int M { get; set; }
    public double TimeMs { get; set; }
    public double TimeSec => TimeMs / 1000.0;
    public Color Color { get; set; }
    public bool IsTheoretical { get; set; }
}

/// <summary>
/// Описание отдельного 3D-графика (поверхности) в списке контрола.
/// Позволяет добавлять произвольное количество прогонов, сравнивать их и переключать видимость.
/// </summary>
public class Surface3DSeries
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = "";
    public int[] NValues { get; set; } = [];
    public int[] MValues { get; set; } = [];
    public double[,] Data { get; set; } = new double[0, 0];
    public bool IsVisible { get; set; } = true;
    public bool IsTheoretical { get; set; } = false;
    public Color? BaseColor { get; set; }
    public bool UseViridis { get; set; } = true;
    public double LineWidth { get; set; } = 1.8;
}

/// <summary>
/// Кастомный Avalonia-компонент для интерактивного 3D-отображения поверхностей.
/// Поддерживает:
/// - Список 3D-серий (текущий прогон, теоретический, история и др.)
/// - Вращение, масштабирование и панорамирование мышью
/// - Нажатие на точки с отображением всплывающей карточки координат (N, M, Время в секундах)
/// - Деления и числовые значения на всех осях (время переведено в секунды)
/// </summary>
public class SurfacePlot3DControl : Control
{
    // ==========================================
    // 1. СПИСОК ГРАФИКОВ (СЕРИЙ)
    // ==========================================
    
    public List<Surface3DSeries> Series { get; } = new();

    private bool _showEmpirical = true;
    private bool _showTheoretical = true;

    public bool ShowEmpirical
    {
        get => Series.FirstOrDefault(s => s.Id == "empirical")?.IsVisible ?? _showEmpirical;
        set
        {
            _showEmpirical = value;
            var s = Series.FirstOrDefault(x => x.Id == "empirical");
            if (s != null) s.IsVisible = value;
            InvalidateVisual();
        }
    }

    public bool ShowTheoretical
    {
        get => Series.FirstOrDefault(s => s.Id == "theoretical")?.IsVisible ?? _showTheoretical;
        set
        {
            _showTheoretical = value;
            var s = Series.FirstOrDefault(x => x.Id == "theoretical");
            if (s != null) s.IsVisible = value;
            InvalidateVisual();
        }
    }

    // ==========================================
    // 2. ДИАПАЗОНЫ ОСЕЙ
    // ==========================================
    
    private int _minN = 50;
    private int _maxN = 300;
    private int _minM = 50;
    private int _maxM = 300;
    private double _minTime = 0.0;
    private double _maxTime = 1.0;

    // Выбранная точка при клике
    private SelectedPointInfo? _selectedPoint;

    // ==========================================
    // 3. КАМЕРА (ВРАЩЕНИЕ, ПАНОРАМИРОВАНИЕ, ЗУМ)
    // ==========================================
    
    private double _azimuth = -Math.PI / 4.0; 
    private double _elevation = Math.PI / 7.0; 
    private double _zoom = 1.0;
    private double _panX = 0;
    private double _panY = 15;

    private bool _isDragging = false;
    private bool _isPanning = false;
    private Point _lastMousePosition;
    private Point _pointerDownPosition;

    // ==========================================
    // 4. УПРАВЛЕНИЕ СЕРИЯМИ
    // ==========================================

    public void AddSeries(Surface3DSeries series)
    {
        Series.RemoveAll(s => s.Id == series.Id);
        Series.Add(series);
        RecalculateBounds();
        InvalidateVisual();
    }

    public bool RemoveSeries(string id)
    {
        int count = Series.RemoveAll(s => s.Id == id);
        if (count > 0)
        {
            RecalculateBounds();
            InvalidateVisual();
            return true;
        }
        return false;
    }

    public void ClearSeries()
    {
        Series.Clear();
        _selectedPoint = null;
        RecalculateBounds();
        InvalidateVisual();
    }

    public void SetData(int[] nValues, int[] mValues, double[,] empirical, double[,]? theoretical = null)
    {
        Series.RemoveAll(s => s.Id == "empirical" || s.Id == "theoretical");

        Series.Add(new Surface3DSeries
        {
            Id = "empirical",
            Title = "Умножение матриц",
            NValues = nValues,
            MValues = mValues,
            Data = empirical,
            UseViridis = true,
            IsTheoretical = false,
            IsVisible = _showEmpirical,
            LineWidth = 2.0
        });

        if (theoretical != null)
        {
            Series.Add(new Surface3DSeries
            {
                Id = "theoretical",
                Title = "Теоретическая модель",
                NValues = nValues,
                MValues = mValues,
                Data = theoretical,
                UseViridis = false,
                BaseColor = Color.Parse("#F97316"),
                IsTheoretical = true,
                IsVisible = _showTheoretical,
                LineWidth = 1.8
            });
        }

        RecalculateBounds();
        InvalidateVisual();
    }

    public void RecalculateBounds()
    {
        bool hasData = false;
        int minN = int.MaxValue;
        int maxN = int.MinValue;
        int minM = int.MaxValue;
        int maxM = int.MinValue;
        double minT = double.MaxValue;
        double maxT = double.MinValue;

        foreach (var s in Series)
        {
            if (s.NValues.Length == 0 || s.MValues.Length == 0 || s.Data == null) continue;

            hasData = true;
            if (s.NValues[0] < minN) minN = s.NValues[0];
            if (s.NValues[^1] > maxN) maxN = s.NValues[^1];

            if (s.MValues[0] < minM) minM = s.MValues[0];
            if (s.MValues[^1] > maxM) maxM = s.MValues[^1];

            int rows = s.Data.GetLength(0);
            int cols = s.Data.GetLength(1);
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    double val = s.Data[i, j];
                    if (val < minT) minT = val;
                    if (val > maxT) maxT = val;
                }
            }
        }

        if (hasData)
        {
            _minN = minN;
            _maxN = maxN > minN ? maxN : minN + 1;
            _minM = minM;
            _maxM = maxM > minM ? maxM : minM + 1;
            _minTime = minT;
            _maxTime = maxT > minT ? maxT : minT + 1.0;
        }
        else
        {
            _minN = 50; _maxN = 300;
            _minM = 50; _maxM = 300;
            _minTime = 0; _maxTime = 1;
        }
    }

    // ==========================================
    // 5. 3D ПРОЕКЦИЯ
    // ==========================================
    
    private Point ProjectPoint(double x, double y, double z, double centerX, double centerY, double scale)
    {
        double cosA = Math.Cos(_azimuth);
        double sinA = Math.Sin(_azimuth);
        double xRot = x * cosA - y * sinA;
        double yRot = x * sinA + y * cosA;

        double cosE = Math.Cos(_elevation);
        double sinE = Math.Sin(_elevation);
        double yFinal = yRot * cosE - z * sinE;
        double zFinal = yRot * sinE + z * cosE;

        double screenX = centerX + _panX + xRot * scale;
        double screenY = centerY + _panY - zFinal * scale;

        return new Point(screenX, screenY);
    }

    // ==========================================
    // 6. ОТРИСОВКА (RENDER)
    // ==========================================
    
    public override void Render(DrawingContext context)
    {
        base.Render(context);

        double width = Bounds.Width;
        double height = Bounds.Height;
        if (width <= 0 || height <= 0) return;

        // Фон
        context.FillRectangle(new SolidColorBrush(Color.Parse("#1A1A1E")), new Rect(0, 0, width, height));

        var visibleSeries = Series.Where(s => s.IsVisible && s.NValues.Length > 0 && s.MValues.Length > 0).ToList();
        if (visibleSeries.Count == 0)
        {
            DrawEmptyMessage(context, width, height);
            return;
        }

        double centerX = width / 2.0;
        double centerY = height / 2.0;
        double scale = Math.Min(width, height) * 0.38 * _zoom;

        // Координатный бокс с делениями и секундами
        DrawBoundingBox(context, centerX, centerY, scale);

        // Рисуем все активные поверхности из списка
        foreach (var s in visibleSeries.OrderBy(x => x.IsTheoretical ? 0 : 1))
        {
            DrawSurface(context, s, centerX, centerY, scale);
        }

        // Если точка выбрана кликом мыши, рисуем красивую плашку координат
        if (_selectedPoint != null)
        {
            DrawSelectedPointTooltip(context, centerX, centerY, scale);
        }
    }

    private void DrawSurface(DrawingContext context, Surface3DSeries series, double centerX, double centerY, double scale)
    {
        int nPoints = series.NValues.Length;
        int mPoints = series.MValues.Length;
        if (nPoints < 2 || mPoints < 2) return;

        Point[,] screenPoints = new Point[nPoints, mPoints];

        for (int i = 0; i < nPoints; i++)
        {
            int n = series.NValues[i];
            double xNorm = -1.0 + 2.0 * ((double)(n - _minN) / (_maxN - _minN));

            for (int j = 0; j < mPoints; j++)
            {
                int m = series.MValues[j];
                double yNorm = -1.0 + 2.0 * ((double)(m - _minM) / (_maxM - _minM));
                double time = series.Data[i, j];
                double zNorm = -1.0 + 2.0 * ((time - _minTime) / (_maxTime - _minTime));

                screenPoints[i, j] = ProjectPoint(xNorm, yNorm, zNorm, centerX, centerY, scale);
            }
        }

        // 1. Полупрозрачные грани полигонов (поверхность полотна)
        for (int i = 0; i < nPoints - 1; i++)
        {
            for (int j = 0; j < mPoints - 1; j++)
            {
                Point p1 = screenPoints[i, j];
                Point p2 = screenPoints[i, j + 1];
                Point p3 = screenPoints[i + 1, j + 1];
                Point p4 = screenPoints[i + 1, j];

                IBrush facetBrush;
                if (series.IsTheoretical)
                {
                    Color baseCol = series.BaseColor ?? Color.Parse("#F97316");
                    facetBrush = new SolidColorBrush(Color.FromArgb(28, baseCol.R, baseCol.G, baseCol.B));
                }
                else if (!series.UseViridis && series.BaseColor.HasValue)
                {
                    Color baseCol = series.BaseColor.Value;
                    facetBrush = new SolidColorBrush(Color.FromArgb(35, baseCol.R, baseCol.G, baseCol.B));
                }
                else
                {
                    double avgTime = (series.Data[i, j] + series.Data[i, j + 1] + series.Data[i + 1, j + 1] + series.Data[i + 1, j]) / 4.0;
                    Color c = GetViridisColor((avgTime - _minTime) / (_maxTime - _minTime));
                    facetBrush = new SolidColorBrush(Color.FromArgb(42, c.R, c.G, c.B));
                }

                StreamGeometry geometry = new StreamGeometry();
                using (var gc = geometry.Open())
                {
                    gc.BeginFigure(p1, isFilled: true);
                    gc.LineTo(p2);
                    gc.LineTo(p3);
                    gc.LineTo(p4);
                    gc.EndFigure(isClosed: true);
                }

                context.DrawGeometry(facetBrush, null, geometry);
            }
        }

        // 2. Линии сетки
        Pen defaultPen = series.IsTheoretical
            ? new Pen(new SolidColorBrush(series.BaseColor ?? Color.Parse("#F97316")), series.LineWidth, lineCap: PenLineCap.Round)
            : new Pen(new SolidColorBrush(series.BaseColor ?? Color.Parse("#38BDF8")), series.LineWidth);

        // Линии вдоль N
        for (int i = 0; i < nPoints; i++)
        {
            for (int j = 0; j < mPoints - 1; j++)
            {
                Point p1 = screenPoints[i, j];
                Point p2 = screenPoints[i, j + 1];

                if (series.UseViridis && !series.IsTheoretical)
                {
                    double avgTime = (series.Data[i, j] + series.Data[i, j + 1]) / 2.0;
                    Color color = GetViridisColor((avgTime - _minTime) / (_maxTime - _minTime));
                    Pen empPen = new Pen(new SolidColorBrush(color), series.LineWidth);
                    context.DrawLine(empPen, p1, p2);
                }
                else
                {
                    context.DrawLine(defaultPen, p1, p2);
                }
            }
        }

        // Линии вдоль M
        for (int j = 0; j < mPoints; j++)
        {
            for (int i = 0; i < nPoints - 1; i++)
            {
                Point p1 = screenPoints[i, j];
                Point p2 = screenPoints[i + 1, j];

                if (series.UseViridis && !series.IsTheoretical)
                {
                    double avgTime = (series.Data[i, j] + series.Data[i + 1, j]) / 2.0;
                    Color color = GetViridisColor((avgTime - _minTime) / (_maxTime - _minTime));
                    Pen empPen = new Pen(new SolidColorBrush(color), series.LineWidth);
                    context.DrawLine(empPen, p1, p2);
                }
                else
                {
                    context.DrawLine(defaultPen, p1, p2);
                }
            }
        }

        // 3. Светящиеся узлы
        if (series.IsTheoretical)
        {
            var theorDotBrush = new SolidColorBrush(series.BaseColor ?? Color.Parse("#F97316"));
            for (int i = 0; i < nPoints; i++)
            {
                for (int j = 0; j < mPoints; j++)
                {
                    Point pt = screenPoints[i, j];
                    context.DrawEllipse(theorDotBrush, null, pt, 2.5, 2.5);
                }
            }
        }
        else
        {
            for (int i = 0; i < nPoints; i++)
            {
                for (int j = 0; j < mPoints; j++)
                {
                    Point pt = screenPoints[i, j];
                    Color ptColor = series.UseViridis
                        ? GetViridisColor((series.Data[i, j] - _minTime) / (_maxTime - _minTime))
                        : (series.BaseColor ?? Color.Parse("#38BDF8"));
                    context.DrawEllipse(new SolidColorBrush(ptColor), null, pt, 3.5, 3.5);
                }
            }
        }
    }

    // ==========================================
    // 7. КООРДИНАТНЫЙ БОКС С ДЕЛЕНИЯМИ (В СЕКУНДАХ)
    // ==========================================
    
    private void DrawBoundingBox(DrawingContext context, double centerX, double centerY, double scale)
    {
        Pen boxPen = new Pen(new SolidColorBrush(Color.FromArgb(70, 255, 255, 255)), 1.0, lineCap: PenLineCap.Round);
        Pen axisPen = new Pen(new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)), 2.0);
        Pen tickPen = new Pen(new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)), 1.4);
        Pen gridPen = new Pen(new SolidColorBrush(Color.FromArgb(28, 255, 255, 255)), 0.8);

        Point p000 = ProjectPoint(-1, -1, -1, centerX, centerY, scale); // Начало (min N, min M, min T)
        Point p100 = ProjectPoint(1, -1, -1, centerX, centerY, scale);  // max N
        Point p110 = ProjectPoint(1, 1, -1, centerX, centerY, scale);   // max N, max M
        Point p010 = ProjectPoint(-1, 1, -1, centerX, centerY, scale);  // max M

        Point p001 = ProjectPoint(-1, -1, 1, centerX, centerY, scale);  // max Time
        Point p101 = ProjectPoint(1, -1, 1, centerX, centerY, scale);
        Point p111 = ProjectPoint(1, 1, 1, centerX, centerY, scale);
        Point p011 = ProjectPoint(-1, 1, 1, centerX, centerY, scale);

        // Ребра основания (пол z = -1)
        context.DrawLine(axisPen, p000, p100);
        context.DrawLine(boxPen, p100, p110);
        context.DrawLine(boxPen, p110, p010);
        context.DrawLine(axisPen, p010, p000);

        // Вертикальные ребра куба (ось времени вверх)
        context.DrawLine(axisPen, p000, p001);
        context.DrawLine(boxPen, p100, p101);
        context.DrawLine(boxPen, p110, p111);
        context.DrawLine(boxPen, p010, p011);

        // Верхняя грань
        context.DrawLine(boxPen, p001, p101);
        context.DrawLine(boxPen, p101, p111);
        context.DrawLine(boxPen, p111, p011);
        context.DrawLine(boxPen, p011, p001);

        Typeface font = new Typeface("Inter, Segoe UI, sans-serif");
        Typeface fontBold = new Typeface("Inter, Segoe UI, sans-serif", FontStyle.Normal, FontWeight.SemiBold);
        IBrush labelBrush = new SolidColorBrush(Color.Parse("#E0E0E0"));
        IBrush tickTextBrush = new SolidColorBrush(Color.Parse("#A0A0A5"));

        // ----------------------------------------------------
        // ДЕЛЕНИЯ НА ОСИ N (строки)
        // ----------------------------------------------------
        int nTicks = 5;
        for (int i = 0; i <= nTicks; i++)
        {
            double ratio = (double)i / nTicks;
            double xNorm = -1.0 + 2.0 * ratio;
            int nVal = (int)Math.Round(_minN + (_maxN - _minN) * ratio);

            Point ptAxis = ProjectPoint(xNorm, -1.0, -1.0, centerX, centerY, scale);
            Point ptTick = ProjectPoint(xNorm, -1.05, -1.0, centerX, centerY, scale);
            context.DrawLine(tickPen, ptAxis, ptTick);

            // Сетка пола
            Point ptFloorEnd = ProjectPoint(xNorm, 1.0, -1.0, centerX, centerY, scale);
            context.DrawLine(gridPen, ptAxis, ptFloorEnd);

            // Числовое значение
            FormattedText txtVal = new FormattedText(nVal.ToString(), CultureInfo.InvariantCulture, FlowDirection.LeftToRight, font, 10, tickTextBrush);
            context.DrawText(txtVal, new Point(ptTick.X - txtVal.Width / 2.0, ptTick.Y + 2));
        }

        // Подпись оси N
        Point midN = new Point((p000.X + p100.X) / 2.0 + 10, (p000.Y + p100.Y) / 2.0 + 18);
        FormattedText txtN = new FormattedText("Размерность N (строки)", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, fontBold, 12, labelBrush);
        context.DrawText(txtN, midN);

        // ----------------------------------------------------
        // ДЕЛЕНИЯ НА ОСИ M (столбцы)
        // ----------------------------------------------------
        int mTicks = 5;
        for (int j = 0; j <= mTicks; j++)
        {
            double ratio = (double)j / mTicks;
            double yNorm = -1.0 + 2.0 * ratio;
            int mVal = (int)Math.Round(_minM + (_maxM - _minM) * ratio);

            Point ptAxis = ProjectPoint(-1.0, yNorm, -1.0, centerX, centerY, scale);
            Point ptTick = ProjectPoint(-1.05, yNorm, -1.0, centerX, centerY, scale);
            context.DrawLine(tickPen, ptAxis, ptTick);

            // Сетка пола
            Point ptFloorEnd = ProjectPoint(1.0, yNorm, -1.0, centerX, centerY, scale);
            context.DrawLine(gridPen, ptAxis, ptFloorEnd);

            // Числовое значение
            FormattedText txtVal = new FormattedText(mVal.ToString(), CultureInfo.InvariantCulture, FlowDirection.LeftToRight, font, 10, tickTextBrush);
            context.DrawText(txtVal, new Point(ptTick.X - txtVal.Width - 4, ptTick.Y - txtVal.Height / 2.0));
        }

        // Подпись оси M
        Point midM = new Point((p000.X + p010.X) / 2.0 - 75, (p000.Y + p010.Y) / 2.0 + 18);
        FormattedText txtM = new FormattedText("Размерность M (столбцы)", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, fontBold, 12, labelBrush);
        context.DrawText(txtM, midM);

        // ----------------------------------------------------
        // ДЕЛЕНИЯ НА ОСИ Z (ВРЕМЯ В СЕКУНДАХ!)
        // ----------------------------------------------------
        int zTicks = 5;
        for (int k = 0; k <= zTicks; k++)
        {
            double ratio = (double)k / zTicks;
            double zNorm = -1.0 + 2.0 * ratio;
            double timeMs = _minTime + (_maxTime - _minTime) * ratio;
            double timeSec = timeMs / 1000.0; // ПЕРЕВОД В СЕКУНДЫ

            Point ptAxis = ProjectPoint(-1.0, -1.0, zNorm, centerX, centerY, scale);
            Point ptTick = ProjectPoint(-1.06, -1.06, zNorm, centerX, centerY, scale);
            context.DrawLine(tickPen, ptAxis, ptTick);

            // Сетка задней стенки
            Point ptWallEnd = ProjectPoint(-1.0, 1.0, zNorm, centerX, centerY, scale);
            context.DrawLine(gridPen, ptAxis, ptWallEnd);

            // Форматирование секунд: если значения меньше 0.1 с, 3-4 знака после запятой
            string secLabel = $"{timeSec:F3} с";
            FormattedText txtVal = new FormattedText(secLabel, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, font, 10, tickTextBrush);
            context.DrawText(txtVal, new Point(ptTick.X - txtVal.Width - 6, ptTick.Y - txtVal.Height / 2.0));
        }

        // Подпись вертикальной оси Z (Время в секундах)
        Point midT = new Point(p001.X - 95, (p000.Y + p001.Y) / 2.0);
        FormattedText txtT = new FormattedText("Время T (секунды)", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, fontBold, 12, labelBrush);
        context.DrawText(txtT, midT);
    }

    // ==========================================
    // 8. КАРТОЧКА КООРДИНАТ ПРИ КЛИКЕ НА ТОЧКУ
    // ==========================================
    
    private void DrawSelectedPointTooltip(DrawingContext context, double centerX, double centerY, double scale)
    {
        if (_selectedPoint == null) return;

        double xNorm = -1.0 + 2.0 * ((double)(_selectedPoint.N - _minN) / (_maxN - _minN));
        double yNorm = -1.0 + 2.0 * ((double)(_selectedPoint.M - _minM) / (_maxM - _minM));
        double zNorm = -1.0 + 2.0 * ((_selectedPoint.TimeMs - _minTime) / (_maxTime - _minTime));
        Point ptScreen = ProjectPoint(xNorm, yNorm, zNorm, centerX, centerY, scale);

        // Светящееся кольцо вокруг выбранной точки
        Pen haloPen = new Pen(new SolidColorBrush(Color.FromArgb(140, _selectedPoint.Color.R, _selectedPoint.Color.G, _selectedPoint.Color.B)), 3.5);
        context.DrawEllipse(null, haloPen, ptScreen, 7.5, 7.5);

        Pen corePen = new Pen(new SolidColorBrush(Colors.White), 2.0);
        context.DrawEllipse(new SolidColorBrush(_selectedPoint.Color), corePen, ptScreen, 4.5, 4.5);

        // Позиция всплывающей плашки
        double boxW = 210;
        double boxH = 75;
        double boxX = ptScreen.X + 18;
        double boxY = ptScreen.Y - boxH - 12;

        if (boxX + boxW > Bounds.Width - 10) boxX = ptScreen.X - boxW - 18;
        if (boxY < 10) boxY = ptScreen.Y + 18;

        Rect tooltipRect = new Rect(boxX, boxY, boxW, boxH);

        // Линия-указатель от точки к плашке
        Pen pointerPen = new Pen(new SolidColorBrush(Color.FromArgb(180, _selectedPoint.Color.R, _selectedPoint.Color.G, _selectedPoint.Color.B)), 1.4, lineCap: PenLineCap.Round);
        Point anchor = new Point(boxX < ptScreen.X ? boxX + boxW : boxX, boxY + boxH / 2.0);
        context.DrawLine(pointerPen, ptScreen, anchor);

        // Фон плашки с закругленными краями
        IBrush cardBg = new SolidColorBrush(Color.Parse("#202026"));
        Pen cardBorder = new Pen(new SolidColorBrush(Color.FromArgb(220, _selectedPoint.Color.R, _selectedPoint.Color.G, _selectedPoint.Color.B)), 1.5);
        context.DrawRectangle(cardBg, cardBorder, tooltipRect, 8.0, 8.0);

        Typeface fontBold = new Typeface("Inter, Segoe UI, sans-serif", FontStyle.Normal, FontWeight.Bold);
        Typeface fontRegular = new Typeface("Inter, Segoe UI, sans-serif");

        // 1. Название графика
        string title = string.IsNullOrEmpty(_selectedPoint.SeriesTitle)
            ? (_selectedPoint.IsTheoretical ? "Теоретическая модель" : "Замер времени")
            : _selectedPoint.SeriesTitle;
        FormattedText txtTitle = new FormattedText(title, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, fontBold, 11, new SolidColorBrush(_selectedPoint.Color));
        context.DrawText(txtTitle, new Point(boxX + 12, boxY + 8));

        // 2. Координаты N и M
        FormattedText txtNM = new FormattedText($"Размерность: N = {_selectedPoint.N}, M = {_selectedPoint.M}", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, fontRegular, 11, new SolidColorBrush(Color.Parse("#E0E0E0")));
        context.DrawText(txtNM, new Point(boxX + 12, boxY + 28));

        // 3. Время в секундах и миллисекундах
        string timeStr = $"Время: {_selectedPoint.TimeSec:F4} с ({_selectedPoint.TimeMs:F1} мс)";
        FormattedText txtTime = new FormattedText(timeStr, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, fontBold, 11, new SolidColorBrush(Color.Parse("#FACC15")));
        context.DrawText(txtTime, new Point(boxX + 12, boxY + 48));
    }

    // ==========================================
    // 9. ЦВЕТОВАЯ КАРТА VIRIDIS
    // ==========================================
    
    private static Color GetViridisColor(double t)
    {
        t = Math.Clamp(t, 0.0, 1.0);

        byte r, g, b;
        if (t < 0.33)
        {
            double localT = t / 0.33;
            r = (byte)(0x44 + (0x21 - 0x44) * localT);
            g = (byte)(0x01 + (0x90 - 0x01) * localT);
            b = (byte)(0x54 + (0x8D - 0x54) * localT);
        }
        else if (t < 0.66)
        {
            double localT = (t - 0.33) / 0.33;
            r = (byte)(0x21 + (0x5D - 0x21) * localT);
            g = (byte)(0x90 + (0xC8 - 0x90) * localT);
            b = (byte)(0x8D + (0x63 - 0x8D) * localT);
        }
        else
        {
            double localT = (t - 0.66) / 0.34;
            r = (byte)(0x5D + (0xFD - 0x5D) * localT);
            g = (byte)(0xC8 + (0xE7 - 0xC8) * localT);
            b = (byte)(0x63 + (0x25 - 0x63) * localT);
        }

        return Color.FromRgb(r, g, b);
    }

    private void DrawEmptyMessage(DrawingContext context, double width, double height)
    {
        var text = new FormattedText(
            "Нажмите «Сделать прогон», чтобы рассчитать 3D-поверхность",
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Inter, Segoe UI, sans-serif"),
            14,
            new SolidColorBrush(Color.Parse("#888888")));

        Point pos = new Point(width / 2.0 - text.Width / 2.0, height / 2.0 - text.Height / 2.0);
        context.DrawText(text, pos);
    }

    // ==========================================
    // 10. КЛИКИ МЫШИ (ПОИСК БЛИЖАЙШЕЙ ТОЧКИ)
    // ==========================================
    
    private static double Distance(Point a, Point b)
    {
        double dx = a.X - b.X;
        double dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private void HandlePointClick(Point clickPos)
    {
        double centerX = Bounds.Width / 2.0;
        double centerY = Bounds.Height / 2.0;
        double scale = Math.Min(Bounds.Width, Bounds.Height) * 0.38 * _zoom;

        SelectedPointInfo? bestMatch = null;
        double minDistance = 18.0; // Радиус захвата в пикселях

        foreach (var s in Series.Where(x => x.IsVisible && x.NValues.Length > 0 && x.MValues.Length > 0))
        {
            int nPoints = s.NValues.Length;
            int mPoints = s.MValues.Length;

            for (int i = 0; i < nPoints; i++)
            {
                int n = s.NValues[i];
                double xNorm = -1.0 + 2.0 * ((double)(n - _minN) / (_maxN - _minN));

                for (int j = 0; j < mPoints; j++)
                {
                    int m = s.MValues[j];
                    double yNorm = -1.0 + 2.0 * ((double)(m - _minM) / (_maxM - _minM));
                    double time = s.Data[i, j];
                    double zNorm = -1.0 + 2.0 * ((time - _minTime) / (_maxTime - _minTime));

                    Point ptScreen = ProjectPoint(xNorm, yNorm, zNorm, centerX, centerY, scale);
                    double dist = Distance(ptScreen, clickPos);

                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        Color ptColor = s.IsTheoretical
                            ? (s.BaseColor ?? Color.Parse("#F97316"))
                            : (s.UseViridis
                                ? GetViridisColor((time - _minTime) / (_maxTime - _minTime))
                                : (s.BaseColor ?? Color.Parse("#38BDF8")));

                        bestMatch = new SelectedPointInfo
                        {
                            SeriesTitle = s.Title,
                            N = n,
                            M = m,
                            TimeMs = time,
                            Color = ptColor,
                            IsTheoretical = s.IsTheoretical
                        };
                    }
                }
            }
        }

        _selectedPoint = bestMatch;
        InvalidateVisual();
    }

    // ==========================================
    // 11. СОБЫТИЯ МЫШИ (ВРАЩЕНИЕ, ПАНОРАМИРОВАНИЕ, ЗУМ, КЛИК)
    // ==========================================
    
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        var point = e.GetCurrentPoint(this);
        _pointerDownPosition = point.Position;

        if (point.Properties.IsLeftButtonPressed)
        {
            _isDragging = true;
            _lastMousePosition = point.Position;
            e.Pointer.Capture(this);
        }
        else if (point.Properties.IsRightButtonPressed)
        {
            _isPanning = true;
            _lastMousePosition = point.Position;
            e.Pointer.Capture(this);
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        var currentPos = e.GetPosition(this);
        var delta = currentPos - _lastMousePosition;
        _lastMousePosition = currentPos;

        if (_isDragging)
        {
            _azimuth += delta.X * 0.01;
            _elevation += delta.Y * 0.01;
            _elevation = Math.Clamp(_elevation, -Math.PI / 2.5, Math.PI / 2.5);
            InvalidateVisual();
        }
        else if (_isPanning)
        {
            _panX += delta.X;
            _panY += delta.Y;
            InvalidateVisual();
        }
        else
        {
            // Подсветка курсора (Hand), если курсор рядом с точкой
            UpdateCursorNearPoint(currentPos);
        }
    }

    private void UpdateCursorNearPoint(Point mousePos)
    {
        double centerX = Bounds.Width / 2.0;
        double centerY = Bounds.Height / 2.0;
        double scale = Math.Min(Bounds.Width, Bounds.Height) * 0.38 * _zoom;
        bool isNear = false;

        foreach (var s in Series.Where(x => x.IsVisible && x.NValues.Length > 0 && x.MValues.Length > 0))
        {
            int nPoints = s.NValues.Length;
            int mPoints = s.MValues.Length;

            for (int i = 0; i < nPoints && !isNear; i++)
            {
                int n = s.NValues[i];
                double xNorm = -1.0 + 2.0 * ((double)(n - _minN) / (_maxN - _minN));

                for (int j = 0; j < mPoints; j++)
                {
                    int m = s.MValues[j];
                    double yNorm = -1.0 + 2.0 * ((double)(m - _minM) / (_maxM - _minM));
                    double time = s.Data[i, j];
                    double zNorm = -1.0 + 2.0 * ((time - _minTime) / (_maxTime - _minTime));

                    Point ptScreen = ProjectPoint(xNorm, yNorm, zNorm, centerX, centerY, scale);
                    if (Distance(ptScreen, mousePos) < 16.0)
                    {
                        isNear = true;
                        break;
                    }
                }
            }
            if (isNear) break;
        }

        Cursor = isNear ? new Cursor(StandardCursorType.Hand) : new Cursor(StandardCursorType.Arrow);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        var currentPos = e.GetPosition(this);
        double dragDist = Distance(currentPos, _pointerDownPosition);

        // Если смещение мыши меньше 5 пикселей — считаем это кликом для просмотра координат точки
        if (dragDist < 5.0)
        {
            HandlePointClick(currentPos);
        }

        _isDragging = false;
        _isPanning = false;
        e.Pointer.Capture(null);
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);

        double zoomFactor = e.Delta.Y > 0 ? 1.1 : 0.9;
        _zoom *= zoomFactor;
        _zoom = Math.Clamp(_zoom, 0.2, 5.0);
        InvalidateVisual();
    }
}