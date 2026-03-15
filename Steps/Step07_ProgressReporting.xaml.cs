using System.Windows;
using System.Windows.Controls;

namespace AsynAwaitExamples.Steps;

// ============================================================================
// STEP 7: PROGRESS REPORTING
// ============================================================================
//
// KEY CONCEPTS:
// -------------
// 1. IProgress<T> — The interface your async methods should accept.
//    - It has one method: Report(T value) — call it to send a progress update.
//    - Your async method doesn't know or care about the UI; it just calls Report().
//
// 2. Progress<T> — The class you create in the UI code.
//    - You pass a callback (lambda) that runs when Report() is called.
//    - IMPORTANT: Progress<T> automatically runs the callback on the UI thread!
//      This means you can safely update TextBlocks, ProgressBars, etc.
//
// 3. Why not just update the UI directly from the async method?
//    - If the async method runs work on a background thread (e.g., via Task.Run),
//      you CAN'T touch UI controls from that thread — WPF will throw an exception.
//    - Progress<T> handles this for you by marshaling to the UI thread.
//
// PATTERN:
// --------
//   // In the UI code (event handler):
//   var progress = new Progress<int>(percent => {
//       MyProgressBar.Value = percent;
//   });
//   await DoWorkAsync(progress);
//
//   // In the async method:
//   async Task DoWorkAsync(IProgress<int> progress) {
//       for (int i = 0; i <= 100; i += 10) {
//           await Task.Delay(500);
//           progress.Report(i);  // sends update to UI safely
//       }
//   }
// ============================================================================

public partial class Step07_ProgressReporting : Page
{
    public Step07_ProgressReporting()
    {
        InitializeComponent();
    }

    private async void Start_Click(object sender, RoutedEventArgs e)
    {
        StartButton.IsEnabled = false;
        ProgressBar.Value = 0;

        Log("? Starting operation with progress reporting...\n");

        // =====================================================================
        // Create a Progress<T> object. The type parameter is a custom class
        // that carries both a percentage and a message.
        // The lambda inside runs on the UI thread every time Report() is called.
        // =====================================================================
        var progress = new Progress<ProgressInfo>(info =>
        {
            // This code runs on the UI thread — safe to update controls!
            ProgressBar.Value = info.Percentage;
            ProgressText.Text = $"{info.Percentage}% — {info.Message}";
            Log($"   ?? {info.Percentage}% — {info.Message}");
        });

        // Pass the progress reporter to the async method.
        // The method only knows about IProgress<T> (the interface), not the UI.
        await ProcessFilesAsync(progress);

        ProgressText.Text = "? Done!";
        Log("\n? All files processed!\n");
        StartButton.IsEnabled = true;
    }

    /// <summary>
    /// Simulates processing multiple files. Reports progress via IProgress&lt;T&gt;.
    /// 
    /// Notice: this method has NO reference to any UI control.
    /// It only communicates through IProgress. This is good design because:
    ///   - The method could be in a separate library with no UI reference.
    ///   - It's easy to test.
    ///   - The UI can display progress however it wants (progress bar, text, etc.).
    /// </summary>
    private static async Task ProcessFilesAsync(IProgress<ProgressInfo> progress)
    {
        string[] files = ["report.pdf", "photo.jpg", "data.csv", "backup.zip", "notes.txt"];

        for (int i = 0; i < files.Length; i++)
        {
            // Simulate processing each file (takes 1 second)
            await Task.Delay(1000);

            // Calculate percentage and report progress
            int percentage = (int)((i + 1) / (double)files.Length * 100);
            progress.Report(new ProgressInfo(percentage, $"Processed {files[i]}"));
        }
    }

    private void Log(string message)
    {
        Output.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
        Output.ScrollToEnd();
    }
}

// ============================================================================
// A simple class to carry progress information.
// You can put any data you want in here — percentage, messages, file names, etc.
// ============================================================================
public record ProgressInfo(int Percentage, string Message);
