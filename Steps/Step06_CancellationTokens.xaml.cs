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

/// <summary>
/// Step 6 View. DataContext is set in XAML to Step06ViewModel.
/// </summary>
public partial class Step06_CancellationTokens : Page
{
    public Step06_CancellationTokens()
    {
        InitializeComponent();
    }
}
