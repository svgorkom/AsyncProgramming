using System.Windows;
using System.Windows.Controls;

namespace AsynAwaitExamples.Steps;

// ============================================================================
// STEP 6: CANCELLING ASYNC OPERATIONS WITH CancellationToken
// ============================================================================
//
// KEY CONCEPTS:
// -------------
// 1. CancellationTokenSource (CTS)
//    - The CONTROLLER. You create it and call .Cancel() when you want to stop.
//    - Think of it as the "cancel button" itself.
//
// 2. CancellationToken
//    - The SIGNAL that you pass into async methods.
//    - The method checks this token to see if cancellation was requested.
//    - Get it from: cts.Token
//
// 3. token.ThrowIfCancellationRequested()
//    - Call this inside your method periodically (e.g., in a loop).
//    - If Cancel() was called, it throws an OperationCanceledException.
//    - You catch that exception to know the operation was cancelled.
//
// 4. Task.Delay(milliseconds, token)
//    - Many built-in async methods accept a CancellationToken.
//    - Task.Delay will stop immediately when the token is cancelled,
//      instead of waiting the full duration.
//
// PATTERN:
// --------
//   var cts = new CancellationTokenSource();
//   // pass cts.Token to your async method
//   // when user clicks cancel: cts.Cancel();
//   // the method will throw OperationCanceledException
//   // catch it and handle gracefully
// ============================================================================

public partial class Step06_CancellationTokens : Page
{
    // We store the CancellationTokenSource as a field so the Cancel button can access it.
    private CancellationTokenSource? _cts;

    public Step06_CancellationTokens()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Starts a long-running operation that can be cancelled.
    /// </summary>
    private async void Start_Click(object sender, RoutedEventArgs e)
    {
        // Create a NEW CancellationTokenSource each time we start.
        // (A used CTS cannot be reused — you must create a new one.)
        _cts = new CancellationTokenSource();

        StartButton.IsEnabled = false;
        CancelButton.IsEnabled = true;

        Log("? Starting a long operation (10 steps)...");
        Log("   Press Cancel to stop it early!\n");

        try
        {
            // Pass the TOKEN (not the source!) to our async method.
            await DoLongWorkAsync(_cts.Token);
            Log("? Operation completed successfully!\n");
        }
        catch (OperationCanceledException)
        {
            // This exception is thrown when the operation is cancelled.
            // This is EXPECTED behavior, not an error!
            Log("?? Operation was CANCELLED by the user.\n");
        }
        finally
        {
            // Clean up: dispose the CTS and reset button states.
            _cts.Dispose();
            _cts = null;
            StartButton.IsEnabled = true;
            CancelButton.IsEnabled = false;
        }
    }

    /// <summary>
    /// When the user clicks Cancel, we call .Cancel() on the CancellationTokenSource.
    /// This sends a signal to all methods that have the matching CancellationToken.
    /// </summary>
    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Log("   ? Cancel requested!");
        _cts?.Cancel();
    }

    /// <summary>
    /// A simulated long-running operation with 10 steps.
    /// It checks the CancellationToken at each step.
    /// </summary>
    private async Task DoLongWorkAsync(CancellationToken token)
    {
        for (int i = 1; i <= 10; i++)
        {
            // METHOD 1: Check the token and throw if cancelled.
            // This is the most common way to check for cancellation.
            token.ThrowIfCancellationRequested();

            Log($"   ?? Processing step {i} of 10...");

            // METHOD 2: Pass the token to Task.Delay (and other built-in async methods).
            // If the token is cancelled during the delay, it immediately stops waiting
            // and throws OperationCanceledException.
            await Task.Delay(800, token);
        }
    }

    private void Log(string message)
    {
        Output.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
        Output.ScrollToEnd();
    }
}
