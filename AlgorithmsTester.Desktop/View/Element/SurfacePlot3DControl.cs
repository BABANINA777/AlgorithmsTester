using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace AlgorithmsTester.Desktop.Element;

/// <summary>
/// НАЗНАЧЕНИЕ И АРХИТЕКТУРА ОБЪЕКТА SurfacePlot3DControl:
/// 
/// 1. ЧТО ЭТО ТАКОЕ И ГДЕ ОН СОЗДАЕТСЯ?
///    Это полноценный кастомный визуальный элемент (виджет) интерфейса Avalonia, унаследованный от Avalonia.Controls.Control.
///    Он объявляется в XAML-разметке окна MainWindow.axaml тегом:
///        <element:SurfacePlot3DControl x:Name="Matrix3DPlot" />
///    При запуске приложения движок Avalonia автоматически создает этот объект (точно так же, как создает Button, TextBox или AvaPlot).
/// 
/// 2. КАК СЮДА ПОПАДАЮТ ГРАФИКИ (В ЧЕМ РАЗНИЦА СО SCOTTPLOT)?
///    В 2D-библиотеке ScottPlot вы вызывали BenchmarkPlot.Plot.Add.Signal(times), то есть создавали отдельный объект-серию
///    и регистрировали его во внутреннем списке ScottPlot.
///    Здесь сторонние библиотеки не нужны! График — это не отдельный сложный объект, а просто чистые сырые матрицы чисел.
///    Вы передаете данные одним вызовом метода:
///        Matrix3DPlot.SetData(nValues, mValues, empiricalTimes, theorTimes);
/// 
/// 3. КТО И КАК РИСУЕТ ГРАФИКИ?
///    Контрол рисует всё САМ напрямую через графический движок Avalonia (Skia) в методе Render(DrawingContext context):
///    - Берет ваши массивы N, M и сетку времен T.
///    - На лету поворачивает каждую 3D-точку матрицей поворота камеры (зависит от мыши).
///    - Проецирует трехмерные координаты в плоские пиксели экрана.
///    - Рисует цветную проволочную сетку (палитра Viridis от синего к желтому), светящиеся узлы замеров и координатные оси.
///    - При движении мыши вызывается InvalidateVisual(), и контрол мгновенно перерисовывает кадр с частотой 60+ кадров/сек.
/// </summary>
public class SurfacePlot3DControl : Control
{
    // ==========================================
    // 1. ДАННЫЕ ДЛЯ ОТОБРАЖЕНИЯ
    // ==========================================
    
    // Сетка эмпирических (реальных) замеров времени в миллисекундах [nPoints, mPoints]
    private double[,]? _empiricalData;
    
    // Сетка теоретических значений времени (МНК) [nPoints, mPoints]
    private double[,]? _theoreticalData;
    
    // Массивы значений по осям (например, n = [50, 100, 150...], m = [50, 100, 150...])
    private int[]? _nValues;
    private int[]? _mValues;
    
    // Минимальное и максимальное время для масштабирования оси Z и цветовой шкалы
    private double _minTime = 0;
    private double _maxTime = 1;

    // Флаги включения/выключения поверхностей
    public bool ShowEmpirical { get; set; } = true;
    public bool ShowTheoretical { get; set; } = true;

    // ==========================================
    // 2. СОСТОЯНИЕ 3D КАМЕРЫ (ВРАЩЕНИЕ И ЗУМ)
    // ==========================================
    
    // Угол азимута (вращение влево-вправо вокруг вертикальной оси в радианах)
    // -45 градусов для красивого начального изометрического ракурса
    private double _azimuth = -Math.PI / 4.0; 
    
    // Угол подъема камеры (тангаж, наклон сверху-вниз)
    // +25 градусов (чуть сверху)
    private double _elevation = Math.PI / 7.0; 
    
    // Масштаб отображения (зум колесиком мыши)
    private double _zoom = 1.0;
    
    // Смещение центра графика по экрану (панорамирование правой кнопкой мыши)
    private double _panX = 0;
    private double _panY = 20;

    // Переменные для отслеживания перетаскивания мыши
    private bool _isDragging = false;
    private bool _isPanning = false;
    private Point _lastMousePosition;

    // ==========================================
    // 3. МЕТОД ЗАГРУЗКИ ДАННЫХ
    // ==========================================
    
    /// <summary>
    /// Передать новые замеры в 3D-график и перерисовать его.
    /// </summary>
    public void SetData(int[] nValues, int[] mValues, double[,] empirical, double[,]? theoretical = null)
    {
        _nValues = nValues;
        _mValues = mValues;
        _empiricalData = empirical;
        _theoreticalData = theoretical;

        // Находим минимальное и максимальное время для нормализации оси высоты (Z)
        _minTime = double.MaxValue;
        _maxTime = double.MinValue;

        int rows = empirical.GetLength(0);
        int cols = empirical.GetLength(1);

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                double val = empirical[i, j];
                if (val < _minTime) _minTime = val;
                if (val > _maxTime) _maxTime = val;

                if (theoretical != null)
                {
                    double thVal = theoretical[i, j];
                    if (thVal < _minTime) _minTime = thVal;
                    if (thVal > _maxTime) _maxTime = thVal;
                }
            }
        }

        // Защита от нулевого диапазона
        if (_maxTime <= _minTime)
        {
            _maxTime = _minTime + 1.0;
        }

        // Сообщаем Avalonia, что компонент нужно перерисовать
        InvalidateVisual();
    }

    // ==========================================
    // 4. МАТЕМАТИКА 3D ПРОЕКЦИИ (3D -> 2D ПИКСЕЛИ)
    // ==========================================
    
    /// <summary>
    /// Преобразует нормализованную 3D-точку (x, y, z от -1 до 1)
    /// в плоскую экранную точку (ScreenX, ScreenY в пикселях) с учетом поворота и зума камеры.
    /// </summary>
    private Point ProjectPoint(double x, double y, double z, double centerX, double centerY, double scale)
    {
        // ШАГ А: Поворот вокруг вертикальной оси (угол _azimuth)
        // Формула стандартного вращения плоскости (X, Y):
        double cosA = Math.Cos(_azimuth);
        double sinA = Math.Sin(_azimuth);
        double xRot = x * cosA - y * sinA;
        double yRot = x * sinA + y * cosA;

        // ШАГ Б: Поворот вокруг горизонтальной оси (угол _elevation, наклон сверху-вниз)
        // Формула вращения плоскости (Y, Z):
        double cosE = Math.Cos(_elevation);
        double sinE = Math.Sin(_elevation);
        double yFinal = yRot * cosE - z * sinE;
        double zFinal = yRot * sinE + z * cosE;

        // ШАГ В: Проекция на 2D-экран (ортографическая / изометрическая):
        // X на экране идет вправо.
        // Y на экране идет сверху вниз (поэтому отнимаем zFinal, чтобы "высота" шла вверх экрана).
        double screenX = centerX + _panX + xRot * scale;
        double screenY = centerY + _panY - zFinal * scale;

        return new Point(screenX, screenY);
    }

    // ==========================================
    // 5. ГЛАВНЫЙ МЕТОД ОТРИСОВКИ (RENDER)
    // ==========================================
    
    public override void Render(DrawingContext context)
    {
        base.Render(context);

        double width = Bounds.Width;
        double height = Bounds.Height;

        if (width <= 0 || height <= 0) return;

        // 1. Рисуем темный фон карточки графика
        context.FillRectangle(new SolidColorBrush(Color.Parse("#1A1A1E")), new Rect(0, 0, width, height));

        // Если данных еще нет, рисуем вежливую подсказку
        if (_empiricalData == null || _nValues == null || _mValues == null)
        {
            DrawEmptyMessage(context, width, height);
            return;
        }

        // Центр области рисования и базовый масштаб
        double centerX = width / 2.0;
        double centerY = height / 2.0;
        double scale = Math.Min(width, height) * 0.38 * _zoom;

        // 2. Рисуем координатную коробку (основание, оси N, M, Time)
        DrawBoundingBox(context, centerX, centerY, scale);

        int nPoints = _empiricalData.GetLength(0);
        int mPoints = _empiricalData.GetLength(1);

        // 3. Вычисляем координаты всех точек 3D-сетки на экране
        Point[,] screenEmpirical = new Point[nPoints, mPoints];
        Point[,]? screenTheor = _theoreticalData != null ? new Point[nPoints, mPoints] : null;

        for (int i = 0; i < nPoints; i++)
        {
            // Нормализуем индекс N в диапазон от -1.0 до +1.0
            double xNorm = nPoints > 1 ? -1.0 + 2.0 * i / (nPoints - 1) : 0;

            for (int j = 0; j < mPoints; j++)
            {
                // Нормализуем индекс M в диапазон от -1.0 до +1.0
                double yNorm = mPoints > 1 ? -1.0 + 2.0 * j / (mPoints - 1) : 0;

                // Нормализуем время Z в диапазон от -1.0 (минимум) до +1.0 (максимум)
                double empTime = _empiricalData[i, j];
                double zEmpNorm = -1.0 + 2.0 * ((empTime - _minTime) / (_maxTime - _minTime));
                screenEmpirical[i, j] = ProjectPoint(xNorm, yNorm, zEmpNorm, centerX, centerY, scale);

                if (_theoreticalData != null && screenTheor != null)
                {
                    double thTime = _theoreticalData[i, j];
                    double zThNorm = -1.0 + 2.0 * ((thTime - _minTime) / (_maxTime - _minTime));
                    screenTheor[i, j] = ProjectPoint(xNorm, yNorm, zThNorm, centerX, centerY, scale);
                }
            }
        }

        // 4. Отрисовка теоретической поверхности (плавная оранжевая сетка МНК)
        if (ShowTheoretical && screenTheor != null && _theoreticalData != null)
        {
            DrawSurfaceGrid(context, screenTheor, _theoreticalData, nPoints, mPoints, isTheoretical: true);
        }

        // 5. Отрисовка эмпирической поверхности (яркая цветная сетка замеров)
        if (ShowEmpirical)
        {
            DrawSurfaceGrid(context, screenEmpirical, _empiricalData, nPoints, mPoints, isTheoretical: false);
        }

        // 6. Отрисовка подсказок управления внизу экрана
        DrawLegend(context, width, height);
    }

    // ==========================================
    // 6. ОТРИСОВКА СЕТКИ ПОВЕРХНОСТИ И ЦВЕТОВ
    // ==========================================

    private void DrawSurfaceGrid(
        DrawingContext context, 
        Point[,] screenPoints, 
        double[,] data, 
        int nPoints, 
        int mPoints, 
        bool isTheoretical)
    {
        // 1. Отрисовка полупрозрачных граней (полигонов), чтобы поверхность выглядела как реальное трехмерное полотно
        for (int i = 0; i < nPoints - 1; i++)
        {
            for (int j = 0; j < mPoints - 1; j++)
            {
                Point p1 = screenPoints[i, j];
                Point p2 = screenPoints[i, j + 1];
                Point p3 = screenPoints[i + 1, j + 1];
                Point p4 = screenPoints[i + 1, j];

                IBrush facetBrush;
                if (isTheoretical)
                {
                    facetBrush = new SolidColorBrush(Color.FromArgb(28, 249, 115, 22));
                }
                else
                {
                    double avgTime = (data[i, j] + data[i, j + 1] + data[i + 1, j + 1] + data[i + 1, j]) / 4.0;
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

        // 2. Линии каркаса (проволочная сетка)
        Pen theorPen = new Pen(new SolidColorBrush(Color.FromArgb(220, 249, 115, 22)), 1.8, lineCap: PenLineCap.Round);

        // Линии вдоль оси N (строки)
        for (int i = 0; i < nPoints; i++)
        {
            for (int j = 0; j < mPoints - 1; j++)
            {
                Point p1 = screenPoints[i, j];
                Point p2 = screenPoints[i, j + 1];

                if (isTheoretical)
                {
                    context.DrawLine(theorPen, p1, p2);
                }
                else
                {
                    double avgTime = (data[i, j] + data[i, j + 1]) / 2.0;
                    Color color = GetViridisColor((avgTime - _minTime) / (_maxTime - _minTime));
                    Pen empPen = new Pen(new SolidColorBrush(color), 2.0);
                    context.DrawLine(empPen, p1, p2);
                }
            }
        }

        // Линии вдоль оси M (столбцы)
        for (int j = 0; j < mPoints; j++)
        {
            for (int i = 0; i < nPoints - 1; i++)
            {
                Point p1 = screenPoints[i, j];
                Point p2 = screenPoints[i + 1, j];

                if (isTheoretical)
                {
                    context.DrawLine(theorPen, p1, p2);
                }
                else
                {
                    double avgTime = (data[i, j] + data[i + 1, j]) / 2.0;
                    Color color = GetViridisColor((avgTime - _minTime) / (_maxTime - _minTime));
                    Pen empPen = new Pen(new SolidColorBrush(color), 2.0);
                    context.DrawLine(empPen, p1, p2);
                }
            }
        }

        // 3. Светящиеся узлы сетки
        if (isTheoretical)
        {
            var theorDotBrush = new SolidColorBrush(Color.FromArgb(220, 249, 115, 22));
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
                    Color ptColor = GetViridisColor((data[i, j] - _minTime) / (_maxTime - _minTime));
                    context.DrawEllipse(new SolidColorBrush(ptColor), null, pt, 3.5, 3.5);
                }
            }
        }
    }

    // ==========================================
    // 7. КООРДИНАТНАЯ КОРОБКА (ОСИ N, M, T)
    // ==========================================

    private void DrawBoundingBox(DrawingContext context, double centerX, double centerY, double scale)
    {
        Pen boxPen = new Pen(new SolidColorBrush(Color.FromArgb(70, 255, 255, 255)), 1.0, lineCap: PenLineCap.Round);
        Pen axisPen = new Pen(new SolidColorBrush(Color.FromArgb(160, 255, 255, 255)), 1.8);

        // 8 углов трехмерного куба от -1 до +1
        Point p000 = ProjectPoint(-1, -1, -1, centerX, centerY, scale); // начало координат (min N, min M, min T)
        Point p100 = ProjectPoint(1, -1, -1, centerX, centerY, scale);  // max N
        Point p110 = ProjectPoint(1, 1, -1, centerX, centerY, scale);   // max N, max M
        Point p010 = ProjectPoint(-1, 1, -1, centerX, centerY, scale);  // max M

        Point p001 = ProjectPoint(-1, -1, 1, centerX, centerY, scale);  // max T (вверх)
        Point p101 = ProjectPoint(1, -1, 1, centerX, centerY, scale);
        Point p111 = ProjectPoint(1, 1, 1, centerX, centerY, scale);
        Point p011 = ProjectPoint(-1, 1, 1, centerX, centerY, scale);

        // Рисуем плоскость основания (сетка пола z = -1)
        context.DrawLine(axisPen, p000, p100);
        context.DrawLine(boxPen, p100, p110);
        context.DrawLine(boxPen, p110, p010);
        context.DrawLine(axisPen, p010, p000);

        // Вертикальные ребра куба (ось времени вверх)
        context.DrawLine(axisPen, p000, p001);
        context.DrawLine(boxPen, p100, p101);
        context.DrawLine(boxPen, p110, p111);
        context.DrawLine(boxPen, p010, p011);

        // Верхняя грань коробки
        context.DrawLine(boxPen, p001, p101);
        context.DrawLine(boxPen, p101, p111);
        context.DrawLine(boxPen, p111, p011);
        context.DrawLine(boxPen, p011, p001);

        // Подписи осей текста
        IBrush labelBrush = new SolidColorBrush(Color.Parse("#E0E0E0"));
        Typeface font = new Typeface("Inter, Segoe UI, sans-serif");

        // Подпись оси N
        Point midN = new Point((p000.X + p100.X) / 2.0 + 10, (p000.Y + p100.Y) / 2.0 + 12);
        FormattedText txtN = new FormattedText("Размерность N (строки)", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, font, 12, labelBrush);
        context.DrawText(txtN, midN);

        // Подпись оси M
        Point midM = new Point((p000.X + p010.X) / 2.0 - 60, (p000.Y + p010.Y) / 2.0 + 12);
        FormattedText txtM = new FormattedText("Размерность M (столбцы)", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, font, 12, labelBrush);
        context.DrawText(txtM, midM);

        // Подпись оси Z (Время T)
        Point midT = new Point(p001.X - 85, (p000.Y + p001.Y) / 2.0);
        FormattedText txtT = new FormattedText($"Время T ({_maxTime:F1} мс)", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, font, 12, labelBrush);
        context.DrawText(txtT, midT);
    }

    // ==========================================
    // 8. ЦВЕТОВАЯ КАРТА VIRIDIS (ОТ СИНЕГО К ЖЕЛТОМУ)
    // ==========================================

    /// <summary>
    /// Возвращает цвет по нормализованной высоте t от 0.0 до 1.0.
    /// Градиент: Сине-фиолетовый -> Бирюзовый -> Зеленый -> Ярко-желтый.
    /// </summary>
    private static Color GetViridisColor(double t)
    {
        t = Math.Clamp(t, 0.0, 1.0);

        byte r, g, b;
        if (t < 0.33)
        {
            // От фиолетового (#440154) к сине-бирюзовому (#21908D)
            double localT = t / 0.33;
            r = (byte)(0x44 + (0x21 - 0x44) * localT);
            g = (byte)(0x01 + (0x90 - 0x01) * localT);
            b = (byte)(0x54 + (0x8D - 0x54) * localT);
        }
        else if (t < 0.66)
        {
            // От сине-бирюзового (#21908D) к салатовому (#5DC863)
            double localT = (t - 0.33) / 0.33;
            r = (byte)(0x21 + (0x5D - 0x21) * localT);
            g = (byte)(0x90 + (0xC8 - 0x90) * localT);
            b = (byte)(0x8D + (0x63 - 0x8D) * localT);
        }
        else
        {
            // От салатового (#5DC863) к ярко-желтому (#FDE725)
            double localT = (t - 0.66) / 0.34;
            r = (byte)(0x5D + (0xFD - 0x5D) * localT);
            g = (byte)(0xC8 + (0xE7 - 0xC8) * localT);
            b = (byte)(0x63 + (0x25 - 0x63) * localT);
        }

        return Color.FromRgb(r, g, b);
    }

    // ==========================================
    // 9. ВСПОМОГАТЕЛЬНЫЕ ПОДПИСИ И ЛЕГЕНДА
    // ==========================================

    private void DrawEmptyMessage(DrawingContext context, double width, double height)
    {
        var text = new FormattedText(
            "Нажмите «Сделать прогон», чтобы рассчитать 3D-поверхность умножения матриц",
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Inter, Segoe UI, sans-serif"),
            14,
            new SolidColorBrush(Color.Parse("#888888")));

        Point pos = new Point(width / 2.0 - text.Width / 2.0, height / 2.0 - text.Height / 2.0);
        context.DrawText(text, pos);
    }

    private void DrawLegend(DrawingContext context, double width, double height)
    {
        var hint = new FormattedText(
            "Вращение: Левая кнопка мыши | Масштаб: Колёсико | Смещение: Правая кнопка",
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Inter, Segoe UI, sans-serif"),
            11,
            new SolidColorBrush(Color.Parse("#66FFFFFF")));

        context.DrawText(hint, new Point(20, height - 25));
    }

    // ==========================================
    // 10. ОБРАБОТКА МЫШИ (ВРАЩЕНИЕ И ЗУМ)
    // ==========================================

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        var point = e.GetCurrentPoint(this);
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
        double deltaX = currentPos.X - _lastMousePosition.X;
        double deltaY = currentPos.Y - _lastMousePosition.Y;

        if (_isDragging)
        {
            // Вращаем азимут (влево-вправо)
            _azimuth += deltaX * 0.01;

            // Наклоняем угол камеры (вверх-вниз)
            _elevation += deltaY * 0.01;

            // Ограничиваем угол наклона, чтобы мир не переворачивался вверх ногами
            _elevation = Math.Clamp(_elevation, -Math.PI / 2.1, Math.PI / 2.1);

            _lastMousePosition = currentPos;
            InvalidateVisual(); // Перерисовываем кадр
        }
        else if (_isPanning)
        {
            // Смещаем центр графика
            _panX += deltaX;
            _panY += deltaY;

            _lastMousePosition = currentPos;
            InvalidateVisual();
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _isDragging = false;
        _isPanning = false;
        e.Pointer.Capture(null);
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);

        // Зум колесиком мыши (с ограничением от 0.3x до 4.0x)
        double factor = e.Delta.Y > 0 ? 1.12 : 0.89;
        _zoom = Math.Clamp(_zoom * factor, 0.3, 4.0);

        InvalidateVisual();
    }
}