using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AsynAwaitExamples.ViewModels;

// ============================================================================
// STEP 7 VIEWMODEL: PROGRESS REPORTING
// ============================================================================
//
// KEY CONCEPTS:
// -------------
// 1. IProgress<T> — The interface your async methods should accept.
// 2. Progress<T> — Automatically marshals callbacks to the UI thread.
//
// MVVM NOTE:
// ----------
// The ViewModel exposes ProgressPercentage and ProgressMessage as bindable
// properties. The View's ProgressBar and TextBlock bind directly to these.
// Progress<T> callback updates ViewModel properties — no direct UI coupling.
// ============================================================================

public partial class Step07ViewModel : StepViewModelBase
{
    [ObservableProperty]
    private double _progressPercentage;

    [ObservableProperty]
    private string _progressMessage = "Ready";

    [ObservableProperty]
    private bool _isRunning;

    [RelayCommand]
    private async Task StartProcessing()
    {
        IsRunning = true;
        ProgressPercentage = 0;

        Log("?? Starting operation with progress reporting...\n");

        // Create a Progress<T> that updates ViewModel properties.
        // Progress<T> automatically marshals to the UI thread.
        var progress = new Progress<ProgressInfo>(info =>
        {
            ProgressPercentage = info.Percentage;
            ProgressMessage = $"{info.Percentage}% — {info.Message}";
            Log($"   ?? {info.Percentage}% — {info.Message}");
        });

        await ProcessFilesAsync(progress);

        ProgressMessage = "? Done!";
        Log("\n? All files processed!\n");
        IsRunning = false;
    }

    /// <summary>
    /// Simulates processing multiple files. Reports progress via IProgress&lt;T&gt;.
    /// This method has NO reference to any UI control — pure logic.
    /// </summary>
    private static async Task ProcessFilesAsync(IProgress<ProgressInfo> progress)
    {
        string[] files = ["report.pdf", "photo.jpg", "data.csv", "backup.zip", "notes.txt"];

        for (int i = 0; i < files.Length; i++)
        {
            await Task.Delay(1000);

            int percentage = (int)((i + 1) / (double)files.Length * 100);
            progress.Report(new ProgressInfo(percentage, $"Processed {files[i]}"));
        }
    }
}

// ============================================================================
// A simple record to carry progress information.
// ============================================================================
public record ProgressInfo(int Percentage, string Message);
