using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AsynAwaitExamples.ViewModels;

/// <summary>
/// ViewModel for the MainWindow. Manages step navigation.
/// </summary>
public partial class MainWindowViewModel : ObservableObject
{
    private readonly Page[] _steps;

    [ObservableProperty]
    private Page? _currentPage;

    [ObservableProperty]
    private int _selectedStepIndex;

    public MainWindowViewModel()
    {
        // Create an instance of each step page.
        // Each page now uses a ViewModel as its DataContext.
        _steps =
        [
            new Steps.Step01_FreezingUiProblem(),
            new Steps.Step02_FirstAsyncAwait(),
            new Steps.Step03_ReturningValues(),
            new Steps.Step04_SequentialCalls(),
            new Steps.Step05_ParallelWhenAll(),
            new Steps.Step06_CancellationTokens(),
            new Steps.Step07_ProgressReporting(),
            new Steps.Step08_ExceptionHandling(),
            new Steps.Step09_WhenAny(),
            new Steps.Step10_ConfigureAwait(),
            new Steps.Step11_RealHttpCalls(),
            new Steps.Step12_AsyncStreams(),
        ];

        // Start on Step 1
        SelectedStepIndex = 0;
    }

    partial void OnSelectedStepIndexChanged(int value)
    {
        if (value >= 0 && value < _steps.Length)
        {
            CurrentPage = _steps[value];
        }
    }
}
