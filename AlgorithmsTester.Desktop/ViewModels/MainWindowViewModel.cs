using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;


namespace AlgorithmsTester.Desktop.ViewMadels;

public partial class MainWindowViewModel : ObservableObject
{
    
    public ObservableCollection<AlgorithmCardViewModel> Cards {get; init; } = new ();
    // 1. Показываем карточки (по умолчанию включены)
    [ObservableProperty]
    private bool _isCardsVisible = true;
    // 2. Показываем таблицу/график (по умолчанию выключены)
    [ObservableProperty]
    private bool _isTableVisible = false;
    // 3. Запоминаем, на какую карточку нажали
    [ObservableProperty]
    private AlgorithmCardViewModel? _selectedCard;

    public Action<AlgorithmCardViewModel>? OnPlotRequested;

    public MainWindowViewModel()
    {
        // 1. Векторные операции (Арсений)
        Cards.Add(new AlgorithmCardViewModel(
            "Векторные операции",
            ["Константа", "Сумма", "Произведение"],
            ["O(1)", "O(n)", "O(n)"],
            "#5B8FB9",
            80000, 100000,
            ["#38BDF8", "#2563EB", "#1E3A8A", "#FDBA74", "#F97316", "#C2410C"],
            OpenCard));

        // 2. Полиномы (Арсений)
        Cards.Add(new AlgorithmCardViewModel(
            "Полиномы",
            ["Прямой (наивный)", "Метод Горнера"],
            ["O(n²)", "O(n)"],
            "#10B981",
            50, 2000,
            ["#34D399", "#059669", "#FDBA74", "#EA580C"],
            OpenCard));

        // 3. Возведение в степень (Арсений и Миша)
        Cards.Add(new AlgorithmCardViewModel(
            "Возведение в степень",
            ["Простой", "Рекурсивный", "Быстрый бинарный", "Быстрый классический"],
            ["O(n)", "O(log n)", "O(log n)", "O(log n)"],
            "#F59E0B",
            1000, 100000,
            ["#FDE047", "#FACC15", "#EAB308", "#CA8A04", "#FED7AA", "#FB923C", "#F97316", "#C2410C"],
            OpenCard));

        // 4. Сортировки (Миша)
        Cards.Add(new AlgorithmCardViewModel(
            "Сортировки",
            ["Пузырьковая", "Быстрая (QuickSort)", "Timsort"],
            ["O(n²)", "O(n log n)", "O(n log n)"],
            "#EF4444",
            50, 2000,
            ["#F87171", "#DC2626", "#991B1B", "#FDBA74", "#F97316", "#C2410C"],
            OpenCard));

        // 5. Матричные операции (Миша и Арсений)
        Cards.Add(new AlgorithmCardViewModel(
            "Матричные операции",
            ["Умножение матриц", "Алгоритм Карацубы", "Расстояние Левенштейна"],
            ["O(n³)", "O(n^1.585)", "O(n²)"],
            "#8B5CF6",
            10, 300,
            ["#C084FC", "#9333EA", "#581C87", "#FDBA74", "#F97316", "#C2410C"],
            OpenCard));
    }
    
    // Метод: открыть таблицу по нажатию
    public void OpenCard(AlgorithmCardViewModel card)
    {
        SelectedCard = card;
        IsCardsVisible = false;
        IsTableVisible = true;
        OnPlotRequested?.Invoke(card);
        
    }
    // Метод: вернуться назад к карточкам
    [RelayCommand]
    public void GoBack()
    {
        IsCardsVisible = true;
        IsTableVisible = false;
    }
}