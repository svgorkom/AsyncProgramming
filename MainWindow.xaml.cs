using System.Windows;
using System.Windows.Controls;

namespace AsynAwaitExamples;

/// <summary>
/// MainWindow acts as the navigation shell.
/// When you click a step on the left, the right side loads the matching page.
/// </summary>
public partial class MainWindow : Window
{
    // Each step is a separate WPF Page stored in the Steps folder.
    private readonly Page[] _steps;

    public MainWindow()
    {
        InitializeComponent();

        // Create an instance of each step page.
        // Each page teaches ONE async/await concept.
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
        StepList.SelectedIndex = 0;
    }

    /// <summary>
    /// When the user clicks a different step in the left panel,
    /// we navigate the Frame on the right to show that step's page.
    /// </summary>
    private void StepList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        int index = StepList.SelectedIndex;
        if (index >= 0 && index < _steps.Length)
        {
            ContentFrame.Content = _steps[index];
        }
    }
}
