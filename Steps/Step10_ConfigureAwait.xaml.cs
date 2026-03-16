using System.Windows.Controls;

namespace AsynAwaitExamples.Steps;

// ============================================================================
// STEP 10: ConfigureAwait, Task.Run, AND THREAD AWARENESS
// ============================================================================
//
// KEY CONCEPTS:
// -------------
//
// 1. SynchronizationContext (Don't panic -- it's simpler than it sounds!)
//    - WPF has a special "context" that knows about the UI thread.
//    - When you "await" something, this context is captured.
//    - After the await, the context brings you BACK to the UI thread.
//    - This is why you can do: await Task.Delay(1000); MyLabel.Text = "Done!";
//      The label update works because you're back on the UI thread.
//
// 2. ConfigureAwait(false)
//    - Tells the await: "I DON'T need to come back to the original thread."
//    - After the await, code may run on ANY thread pool thread.
//    - DO NOT use this if you need to update UI after the await!
//    - DO use this in library code, data access layers, or non-UI helper methods.
//    - Why? It avoids the overhead of switching back to the UI thread (slightly faster).
//
// 3. Task.Run(() => { ... })
//    - Explicitly runs code on a BACKGROUND THREAD (from the thread pool).
//    - Perfect for CPU-intensive work (calculations, image processing, etc.).
//    - Inside Task.Run, you CANNOT touch UI controls directly!
//    - To update UI from inside Task.Run, use Dispatcher.Invoke().
//
// 4. When to use what:
//    +-----------------------------------------------------------+
//    | Scenario                | Use                              |
//    +-----------------------------------------------------------+
//    | I/O (network, file)     | await directly (no Task.Run)    |
//    | CPU-heavy work          | await Task.Run(() => Work())    |
//    | Library (no UI)         | .ConfigureAwait(false) on awaits|
//    | After await, update UI  | Default (no ConfigureAwait)     |
//    +-----------------------------------------------------------+
// ============================================================================

public partial class Step10_ConfigureAwait : Page
{
    public Step10_ConfigureAwait()
    {
        InitializeComponent();
    }
}
