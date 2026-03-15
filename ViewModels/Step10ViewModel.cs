using System.Windows.Threading;
using CommunityToolkit.Mvvm.Input;

namespace AsynAwaitExamples.ViewModels;

// ============================================================================
// STEP 10 VIEWMODEL: ConfigureAwait, Task.Run, AND THREAD AWARENESS
// ============================================================================
//
// KEY CONCEPTS:
// -------------
// 1. SynchronizationContext — WPF captures it so await resumes on UI thread.
// 2. ConfigureAwait(false) — skip returning to UI thread after await.
// 3. Task.Run — explicitly runs code on a background thread.
//
// MVVM NOTE:
// ----------
// Dispatcher.Invoke is still needed inside Task.Run blocks to marshal back
// to the UI thread for property updates. This is inherent to the threading
// model — MVVM doesn't eliminate threading concerns, it organizes them.
// ============================================================================

public partial class Step10ViewModel : StepViewModelBase
{
    // We need a reference to the Dispatcher for Task.Run scenarios.
    private readonly Dispatcher _dispatcher;

    public Step10ViewModel()
    {
        _dispatcher = Dispatcher.CurrentDispatcher;
    }

    // ========================================================================
    // DEMO 1: Show which thread you're on before and after await.
    // ========================================================================
    [RelayCommand]
    private async Task ShowThreads()
    {
        Log("--- Thread ID Demonstration ---\n");

        Log($"   ?? BEFORE await: Thread ID = {Environment.CurrentManagedThreadId} (UI thread)");

        await Task.Delay(500);
        Log($"   ?? AFTER await Task.Delay: Thread ID = {Environment.CurrentManagedThreadId} (still UI thread!)");

        await Task.Run(() =>
        {
            int bgThreadId = Environment.CurrentManagedThreadId;
            _dispatcher.Invoke(() =>
            {
                Log($"   ?? INSIDE Task.Run: Thread ID = {bgThreadId} (background thread!)");
            });
        });

        Log($"   ?? AFTER await Task.Run: Thread ID = {Environment.CurrentManagedThreadId} (back on UI thread!)");

        int threadBeforeConfigureAwait = Environment.CurrentManagedThreadId;
        await Task.Delay(500).ConfigureAwait(false);
        int threadAfterConfigureAwait = Environment.CurrentManagedThreadId;

        _dispatcher.Invoke(() =>
        {
            Log($"\n   ?? BEFORE ConfigureAwait(false): Thread ID = {threadBeforeConfigureAwait}");
            Log($"   ?? AFTER  ConfigureAwait(false): Thread ID = {threadAfterConfigureAwait}");
            Log("   ?? With ConfigureAwait(false), the thread MAY change after await.\n");
        });
    }

    // ========================================================================
    // DEMO 2: Using Task.Run for CPU-heavy work.
    // ========================================================================
    [RelayCommand]
    private async Task TaskRun()
    {
        Log("--- Task.Run for CPU-Heavy Work ---\n");

        Log("   ?? Starting CPU-heavy calculation on background thread...");
        Log($"   ?? UI thread ID: {Environment.CurrentManagedThreadId}");

        long result = await Task.Run(() =>
        {
            long sum = 0;
            for (int i = 0; i < 100_000_000; i++)
            {
                sum += i;
            }
            return sum;
        });

        Log($"   ? Calculation result: {result:N0}");
        Log($"   ?? Back on UI thread: {Environment.CurrentManagedThreadId}");
        Log("   ?? The UI stayed responsive during the calculation!\n");
    }
}
