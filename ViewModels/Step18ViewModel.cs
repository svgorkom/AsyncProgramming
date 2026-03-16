using CommunityToolkit.Mvvm.Input;

namespace AsynAwaitExamples.ViewModels;

// ============================================================================
// STEP 18 VIEWMODEL: ASYNC DISPOSAL (IAsyncDisposable / await using)
// ============================================================================
//
// KEY CONCEPTS:
// -------------
// 1. IAsyncDisposable -- The async version of IDisposable.
//    - Defines: ValueTask DisposeAsync()
//    - Used when cleanup requires async work (flushing a stream, closing a
//      database connection, sending a "goodbye" message over network, etc.)
//
// 2. "await using" -- The async version of "using".
//    - Regular:  using var file = new FileStream(...);
//    - Async:    await using var conn = new AsyncConnection(...);
//    - At the end of the scope, DisposeAsync() is called automatically.
//
// 3. WHEN TO IMPLEMENT IAsyncDisposable:
//    - Your class holds async resources (network connections, async streams).
//    - Cleanup requires async operations (flush, disconnect, commit).
//    - You want consumers to be able to use "await using".
//
// 4. PATTERN: Implement BOTH IDisposable and IAsyncDisposable.
//    - IDisposable for synchronous callers (legacy code, "using").
//    - IAsyncDisposable for async callers ("await using").
//    - The sync Dispose() can call DisposeAsync().AsTask().Wait() as fallback.
//
// ANALOGY:
// --------
// IDisposable = hanging up the phone (instant).
// IAsyncDisposable = ending a video call (need to flush buffers, send "bye"
//   packet, wait for acknowledgment -- all async operations).
// ============================================================================

public partial class Step18ViewModel : StepViewModelBase
{
    // ========================================================================
    // DEMO 1: Basic await using -- async resource cleanup.
    // ========================================================================
    [RelayCommand]
    private async Task BasicAwaitUsing()
    {
        Log("--- Basic 'await using' ---\n");

        Log("   [>] Opening async resource...");

        await using (var connection = new SimulatedAsyncConnection("Database-1"))
        {
            Log($"   [OK] Connected to: {connection.Name}");
            Log("   [>] Doing some work...");
            await connection.ExecuteQueryAsync("SELECT * FROM Users");
            Log("   [OK] Query executed.");
            Log("   [i] Scope is about to end -- DisposeAsync() will be called...");
        }
        // DisposeAsync() is called here automatically!

        Log("   [OK] Connection was disposed asynchronously (flushed + disconnected).\n");
    }

    // ========================================================================
    // DEMO 2: Multiple async disposable resources.
    // ========================================================================
    [RelayCommand]
    private async Task MultipleResources()
    {
        Log("--- Multiple 'await using' Resources ---\n");

        await using var conn1 = new SimulatedAsyncConnection("Primary-DB");
        await using var conn2 = new SimulatedAsyncConnection("Cache-Server");
        await using var conn3 = new SimulatedAsyncConnection("Message-Queue");

        Log($"   [OK] Opened: {conn1.Name}");
        Log($"   [OK] Opened: {conn2.Name}");
        Log($"   [OK] Opened: {conn3.Name}");

        Log("   [>] Doing work with all 3 connections...");
        await Task.Delay(500);

        Log("   [i] Method ending -- all 3 will be disposed in reverse order...");
        // conn3 disposed first, then conn2, then conn1 (LIFO order).
    }

    // ========================================================================
    // DEMO 3: Show what happens without await using (resource leak).
    // ========================================================================
    [RelayCommand]
    private async Task WithoutAwaitUsing()
    {
        Log("--- Without 'await using' (BAD -- resource leak risk) ---\n");

        Log("   [X] BAD pattern:");
        Log("   +-------------------------------------------------------+");
        Log("   |  var conn = new AsyncConnection();                     |");
        Log("   |  await conn.DoWorkAsync();                             |");
        Log("   |  // If DoWorkAsync throws, Dispose is NEVER called!   |");
        Log("   |  await conn.DisposeAsync(); // might not execute      |");
        Log("   +-------------------------------------------------------+");
        Log("");
        Log("   [OK] GOOD pattern:");
        Log("   +-------------------------------------------------------+");
        Log("   |  await using var conn = new AsyncConnection();         |");
        Log("   |  await conn.DoWorkAsync();                             |");
        Log("   |  // DisposeAsync() called automatically, even          |");
        Log("   |  // if DoWorkAsync throws an exception!                |");
        Log("   +-------------------------------------------------------+");
        Log("");

        // Demonstrate: exception doesn't prevent disposal.
        Log("   [>] Demonstrating: exception + await using = still disposed:\n");

        try
        {
            await using var conn = new SimulatedAsyncConnection("Test-DB");
            Log($"   [OK] Connected to: {conn.Name}");
            Log("   [>] About to throw an exception...");
            throw new InvalidOperationException("Something went wrong!");
        }
        catch (InvalidOperationException ex)
        {
            Log($"   [!] Caught: {ex.Message}");
            Log("   [OK] DisposeAsync() was STILL called! Resource was cleaned up.\n");
        }
    }

    // ========================================================================
    // DEMO 4: Show the IAsyncDisposable implementation pattern.
    // ========================================================================
    [RelayCommand]
    private void ShowPattern()
    {
        Log("--- IAsyncDisposable Implementation Pattern ---\n");
        Log("   public class MyResource : IAsyncDisposable, IDisposable");
        Log("   {");
        Log("       private bool _disposed;");
        Log("");
        Log("       // Async cleanup -- preferred path.");
        Log("       public async ValueTask DisposeAsync()");
        Log("       {");
        Log("           if (_disposed) return;");
        Log("           await FlushBuffersAsync();");
        Log("           await CloseConnectionAsync();");
        Log("           _disposed = true;");
        Log("           GC.SuppressFinalize(this);");
        Log("       }");
        Log("");
        Log("       // Sync fallback for callers that use 'using' (not 'await using').");
        Log("       public void Dispose()");
        Log("       {");
        Log("           DisposeAsync().AsTask().GetAwaiter().GetResult();");
        Log("           GC.SuppressFinalize(this);");
        Log("       }");
        Log("   }");
        Log("");
        Log("   [TIP] Always implement both interfaces for maximum compatibility.\n");
    }
}

// ============================================================================
// Simulated async disposable resource.
// ============================================================================
internal class SimulatedAsyncConnection : IAsyncDisposable
{
    private bool _disposed;

    public string Name { get; }

    public SimulatedAsyncConnection(string name)
    {
        Name = name;
    }

    public async Task ExecuteQueryAsync(string query)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        await Task.Delay(300); // Simulate query execution.
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        // Simulate async cleanup: flush buffers, close connection.
        await Task.Delay(200);
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
