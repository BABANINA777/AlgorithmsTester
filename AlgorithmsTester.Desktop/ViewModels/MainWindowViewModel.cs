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
        Cards.Add(new AlgorithmCardViewModel("Векторные операции", ["Константа", "Сумма", "Произведение"], ["O(1)", "O(n)", "O(n)"], "#5B8FB9", 80000, 100000,["#38BDF8", "#2563EB","#1E3A8A", "#FDBA74","#F97316","#C2410C"] ,OpenCard));
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