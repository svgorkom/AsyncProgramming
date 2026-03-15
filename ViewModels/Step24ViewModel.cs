using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AsynAwaitExamples.ViewModels;

// ============================================================================
// STEP 24 VIEWMODEL: TIMEOUTS (CancelAfter & Task.WaitAsync)
// ============================================================================
//
// KEY CONCEPTS:
// -------------
// 1. CancellationTokenSource.CancelAfter(timeout)
//    - Automatically cancels the token after a specified duration.
//    - The async method receives the token and checks it periodically.
//    - Throws OperationCanceledException when time is up.
//    - Best when the method already accepts CancellationToken.
//
// 2. Task.WaitAsync(timeout) — .NET 6+
//    - Wraps an existing task with a timeout.
//    - If the task doesn't complete in time, throws TimeoutException.
//    - DOES NOT cancel the underlying task! It just stops waiting.
//    - Best when you don't control the method's internals.
//
// 3. COMBINING BOTH:
//    - Use CancelAfter for cooperative cancellation (method stops work).
//    - Use WaitAsync as a safety net (stop waiting, even if method hangs).
//    - Production pattern: CancelAfter + WaitAsync together for robustness.
//
// 4. TimeSpan vs milliseconds:
//    - CancelAfter accepts both: cts.CancelAfter(5000) or cts.CancelAfter(TimeSpan.FromSeconds(5))
//    - WaitAsync accepts TimeSpan: task.WaitAsync(TimeSpan.FromSeconds(5))
//
// ANALOGY:
// --------
// CancelAfter = setting a kitchen timer. When it rings, the cook stops.
// WaitAsync = you stop waiting at the restaurant after 30 minutes, even
//   if the kitchen is still cooking (the food may still come out, but you left).
// Best approach = set a timer for the cook AND a limit for your wait.
// ============================================================================

public partial class Step24ViewModel : StepViewModelBase
{
    [ObservableProperty]
    private bool _isRunning;

    // ========================================================================
    // DEMO 1: CancelAfter — automatic cancellation after timeout.
    // ========================================================================
    [RelayCommand]
    private async Task CancelAfterDemo()
    {
        IsRunning = true;
        Log("--- CancelAfter: Automatic Timeout Cancellation ---\n");

        Log("   ?? Setting timeout: 2 seconds");
        Log("   ? Starting operation that takes 5 seconds...\n");

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(2)); // Auto-cancel after 2s.

        var sw = Stopwatch.StartNew();

        try
        {
            await LongRunningOperationAsync(cts.Token);
            Log("   ? Operation completed (shouldn't happen here).");
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            Log($"   ?? Cancelled after {sw.ElapsedMilliseconds}ms!");
            Log("   ?? CancelAfter automatically triggered the cancellation.");
            Log("   ?? The method cooperatively stopped when it checked the token.\n");
        }
        finally
        {
            IsRunning = false;
        }
    }

    // ========================================================================
    // DEMO 2: Task.WaitAsync — timeout on the waiting side.
    // ========================================================================
    [RelayCommand]
    private async Task WaitAsyncDemo()
    {
        IsRunning = true;
        Log("--- Task.WaitAsync: Timeout Without Cancellation ---\n");

        Log("   ?? Timeout: 1.5 seconds");
        Log("   ? Calling a method that takes 4 seconds (no cancellation support)...\n");

        var sw = Stopwatch.StartNew();

        try
        {
            // WaitAsync wraps the task with a timeout.
            // If the task doesn't finish in time, TimeoutException is thrown.
            string result = await NonCancellableOperationAsync()
                .WaitAsync(TimeSpan.FromSeconds(1.5));

            Log($"   ? Got result: {result}");
        }
        catch (TimeoutException)
        {
            sw.Stop();
            Log($"   ? TimeoutException after {sw.ElapsedMilliseconds}ms!");
            Log("   ?? Note: The underlying operation is STILL RUNNING.");
            Log("   ?? WaitAsync stops waiting, but doesn't cancel the task.");
            Log("   ?? Use CancelAfter when you need to actually STOP the work.\n");
        }
        finally
        {
            IsRunning = false;
        }
    }

    // ========================================================================
    // DEMO 3: Combined pattern — CancelAfter + WaitAsync for robustness.
    // ========================================================================
    [RelayCommand]
    private async Task CombinedDemo()
    {
        IsRunning = true;
        Log("--- Combined: CancelAfter + WaitAsync ---\n");

        Log("   ?? Production pattern:");
        Log("      CancelAfter(2s) ? asks the method to stop cooperatively.");
        Log("      WaitAsync(3s)   ? safety net if method ignores cancellation.\n");

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(2));

        var sw = Stopwatch.StartNew();

        try
        {
            // The method checks the token (cooperative cancellation).
            // WaitAsync is a safety net in case the method misbehaves.
            await LongRunningOperationAsync(cts.Token)
                .WaitAsync(TimeSpan.FromSeconds(3));

            Log("   ? Operation completed.");
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            Log($"   ?? Cooperatively cancelled after {sw.ElapsedMilliseconds}ms");
            Log("   ? CancelAfter worked — the method stopped gracefully.");
        }
        catch (TimeoutException)
        {
            sw.Stop();
            Log($"   ? Hard timeout after {sw.ElapsedMilliseconds}ms");
            Log("   ?? WaitAsync safety net kicked in.");
        }

        Log("");
        IsRunning = false;
    }

    // ========================================================================
    // DEMO 4: Timeout succeeds — operation finishes in time.
    // ========================================================================
    [RelayCommand]
    private async Task SuccessfulTimeout()
    {
        IsRunning = true;
        Log("--- Timeout Success: Operation Finishes In Time ---\n");

        Log("   ?? Timeout: 3 seconds");
        Log("   ? Operation takes only 1 second...\n");

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(3));

        var sw = Stopwatch.StartNew();

        try
        {
            await FastOperationAsync(cts.Token);
            sw.Stop();
            Log($"   ? Completed in {sw.ElapsedMilliseconds}ms (well within 3s timeout).");
            Log("   ?? When the operation finishes in time, nothing special happens.");
            Log("   ?? The CancellationToken simply never triggers.\n");
        }
        catch (OperationCanceledException)
        {
            Log("   ?? Cancelled (shouldn't happen here).");
        }
        finally
        {
            IsRunning = false;
        }
    }

    // --- Helper methods ---

    /// <summary>
    /// A long operation that cooperatively checks the CancellationToken.
    /// </summary>
    private async Task LongRunningOperationAsync(CancellationToken token)
    {
        for (int i = 1; i <= 10; i++)
        {
            token.ThrowIfCancellationRequested();
            Log($"   ?? Step {i}/10...");
            await Task.Delay(500, token);
        }
    }

    /// <summary>
    /// An operation that does NOT accept a CancellationToken.
    /// Cannot be cooperatively cancelled — only WaitAsync can help.
    /// </summary>
    private static async Task<string> NonCancellableOperationAsync()
    {
        await Task.Delay(4000); // No token — can't be cancelled!
        return "Data from non-cancellable operation";
    }

    /// <summary>
    /// A fast operation that finishes well within typical timeouts.
    /// </summary>
    private static async Task FastOperationAsync(CancellationToken token)
    {
        await Task.Delay(1000, token);
    }
}
