using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlgorithmsTester.Desktop.ViewMadels;

public partial class AlgorithmCardViewModel : ObservableObject
{
    public string Title {get; init;} //использовать строгие названия из тг. через название карточек вычисляется какой график рисовать
    public  string[] Names {get; init;} // название алгоритмов содержащихся в карточке
    public string[] Theoretical_Names {get; init;}
    public string[] Difficult {get; init;} //сложность алгоритмов карточки в том же порядке что и имена
    public string AccentColor { get; init; }
    public int DefaultStartN { get; init; }
    public int DefaultEndN { get; init; }
    public string RangeText => $"Диапазон n: от {DefaultStartN:N0} до {DefaultEndN:N0}";
    public bool[] AlghoritmEnabled {get; init;}
    public string[] AlghorithmColor {get; init;} //цвета для графиков алгоритмов, сначала цвета для реальных тестов потом для теоретических в том же порядке, цвета придумывать из палитры оттенков AccentColor карточки. Для теоретических графиков всегда использовать одни и теже оттенки оранжевых цветов
    public string[] Theoretical_AlghorithmColor {get; init;}
    private readonly Action<AlgorithmCardViewModel>? _onOpen;
    
    public List<AlgorithmSeriesItem> SeriesList { get; } = new();
    
    public AlgorithmCardViewModel(string title, string[] names, string[] difficult, string accentColor,int defaultStartN, int defaultEndN, string[] alghorithmColor,Action<AlgorithmCardViewModel>? onOpen = null)
    {
        Title = title;
        Names = names;
        Difficult = difficult;
        AccentColor = accentColor;
        DefaultStartN = defaultStartN;
        DefaultEndN = defaultEndN;
        _onOpen = onOpen;
        Theoretical_Names = new string[names.Length];
        for(int i = 0; i < names.Length; i++)
        {
            Theoretical_Names[i] = $"{names[i]} теоретический";
        }
        AlghoritmEnabled = new bool[names.Length *2];
        for (int i = 0; i < names.Length*2; i++)
        {
            AlghoritmEnabled[i] = true;
        }
        
        int half = alghorithmColor.Length / 2;
        AlghorithmColor = alghorithmColor[..half];
        Theoretical_AlghorithmColor = alghorithmColor[half..];

        for (int i = 0; i < names.Length; i++)
        {
            SeriesList.Add(new AlgorithmSeriesItem
            {
                Name = names[i],
                Color = AlghorithmColor[i],
                TheoreticalName = Theoretical_Names[i],
                TheoreticalColor = Theoretical_AlghorithmColor[i]
            });
        }
    }
    [RelayCommand]
    public void Open() => _onOpen?.Invoke(this);
    
}

public partial class AlgorithmSeriesItem : ObservableObject
{
    public string Name { get; set; } = "";
    public string Color { get; set; } = "";
    public string TheoreticalName { get; set; } = "";
    public string TheoreticalColor { get; set; } = "";
    
    [ObservableProperty]
    private bool _isEnabled = true;
    
    [ObservableProperty]
    private bool _isTheoreticalEnabled = true;
}