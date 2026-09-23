namespace AlgorithmsTester.Core;

public class RunHistoryItem
{
    public long Id { get; set; }
    public string AlgorithmName { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string ExperimentDate { get; set; } = string.Empty;
    public int NStart { get; set; }
    public int NStop { get; set; }
    public int Step { get; set; }
    public int RunsCount { get; set; }
    public bool IsDisplayed { get; set; } = false;

    // Текст для отображения в выпадающем списке ComboBox
    public string DisplayText
    {
        get
        {
            return AlgorithmName + " | " + ExperimentDate + " | N: " + NStart + ".." + NStop;
        }
    }

    public override string ToString()
    {
        return DisplayText;
    }
}
