using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlgorithmsTester.Desktop.ViewMadels;

public partial class AlgorithmCardViewModel : ObservableObject
{
    public string Title {get; init;}
    public  string[] Names {get; init;}
    public string[] Difficult {get; init;}
    public string AccentColor { get; init; }
    public int DefaultStartN { get; init; }
    public int DefaultEndN { get; init; }
    public string RangeText => $"Диапазон n: от {DefaultStartN:N0} до {DefaultEndN:N0}";
    private readonly Action<AlgorithmCardViewModel>? _onOpen;
    public AlgorithmCardViewModel(string title, string[] names, string[] difficult, string accentColor,int defaultStartN, int defaultEndN, Action<AlgorithmCardViewModel>? onOpen = null)
    {
        Title = title;
        Names = names;
        Difficult = difficult;
        AccentColor = accentColor;
        DefaultStartN = defaultStartN;
        DefaultEndN = defaultEndN;
        _onOpen = onOpen;

    }
    [RelayCommand]
    public void Open() => _onOpen?.Invoke(this);
}