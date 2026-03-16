using CommunityToolkit.Mvvm.Input;

namespace AsynAwaitExamples.ViewModels;

// ============================================================================
// STEP 23 VIEWMODEL: ASYNC UNIT TESTING PATTERNS
// ============================================================================
//
// KEY CONCEPTS:
// -------------
// 1. Test methods can be "async Task" -- all major test frameworks support it.
//    - [Fact] public async Task MyTest() { ... await ... }
//    - [Test] public async Task MyTest() { ... await ... }
//    - The test runner awaits the method, so it fully completes before moving on.
//
// 2. Testing async exceptions:
//    - Use Assert.ThrowsAsync<T>() to verify an async method throws.
//    - You pass a Func<Task>, not invoke the method directly.
//    - BAD:  Assert.Throws<Exception>(() => MyAsync());  // won't catch async throws!
//    - GOOD: await Assert.ThrowsAsync<Exception>(() => MyAsync());
//
// 3. Testing cancellation:
//    - Create a CancellationTokenSource, cancel it, pass the token.
//    - Verify OperationCanceledException is thrown.
//
// 4. Testing timeouts:
//    - Use Task.WaitAsync(timeout) to prevent tests from hanging.
//    - If the method takes too long, TimeoutException is thrown -> test fails fast.
//
// 5. Mocking async dependencies:
//    - Return Task.FromResult(value) from mocks for synchronous completion.
//    - Return Task.CompletedTask for void async mocks.
//    - Return Task.FromException<T>(ex) to simulate failures.
//
// NOTE: This step SIMULATES test runs in the UI. Real tests would use
// xUnit/NUnit/MSTest. The patterns shown here are directly transferable.
// ============================================================================

public partial class Step23ViewModel : StepViewModelBase
{
    // ========================================================================
    // DEMO 1: Basic async test pattern.
    // ========================================================================
    [RelayCommand]
    private async Task BasicAsyncTest()
    {
        Log("--- Pattern 1: Basic Async Test ---\n");

        Log("   ?? Test code (xUnit style):");
        Log("   [Fact]");
        Log("   public async Task GetUser_ReturnsCorrectName()");
        Log("   {");
        Log("       var service = new UserService();");
        Log("       string name = await service.GetUserNameAsync(1);");
        Log("       Assert.Equal(\"Alice\", name);");
        Log("   }\n");

        // Simulate running the test.
        Log("   [>] Running simulated test...");
        string result = await SimulatedGetUserNameAsync(1);
        bool passed = result == "Alice";
        Log($"   {(passed ? "[OK] PASSED" : "[X] FAILED")}: " +
            $"Expected \"Alice\", got \"{result}\"\n");
    }

    // ========================================================================
    // DEMO 2: Testing async exceptions.
    // ========================================================================
    [RelayCommand]
    private async Task ExceptionTest()
    {
        Log("--- Pattern 2: Testing Async Exceptions ---\n");

        Log("   ?? Test code (xUnit style):");
        Log("   [Fact]");
        Log("   public async Task GetUser_InvalidId_ThrowsArgException()");
        Log("   {");
        Log("       var service = new UserService();");
        Log("       await Assert.ThrowsAsync<ArgumentException>(");
        Log("           () => service.GetUserNameAsync(-1));");
        Log("   }\n");

        Log("   [i] Common mistake:");
        Log("   [X] Assert.Throws<ArgumentException>(() => GetUserAsync(-1));");
        Log("   [i] This passes a Func<Task>, not Func<T>. The exception is");
        Log("     inside the Task and won't be caught by Assert.Throws!\n");

        // Simulate running the test.
        Log("   [>] Running simulated test...");
        try
        {
            await SimulatedGetUserNameAsync(-1);
            Log("   [X] FAILED: No exception was thrown.");
        }
        catch (ArgumentException ex)
        {
            Log($"   [OK] PASSED: Caught expected {ex.GetType().Name}: \"{ex.Message}\"\n");
        }
    }

    // ========================================================================
    // DEMO 3: Testing cancellation.
    // ========================================================================
    [RelayCommand]
    private async Task CancellationTest()
    {
        Log("--- Pattern 3: Testing Cancellation ---\n");

        Log("   ?? Test code:");
        Log("   [Fact]");
        Log("   public async Task LongOp_WhenCancelled_ThrowsOpCancelled()");
        Log("   {");
        Log("       using var cts = new CancellationTokenSource();");
        Log("       cts.Cancel(); // Cancel immediately");
        Log("       await Assert.ThrowsAsync<OperationCanceledException>(");
        Log("           () => service.DoLongWorkAsync(cts.Token));");
        Log("   }\n");

        // Simulate running the test.
        Log("   [>] Running simulated test...");

        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately.

        try
        {
            await SimulatedLongWorkAsync(cts.Token);
            Log("   [X] FAILED: No exception was thrown.");
        }
        catch (OperationCanceledException)
        {
            Log("   [OK] PASSED: OperationCanceledException was thrown as expected.\n");
        }
    }

    // ========================================================================
    // DEMO 4: Testing with timeouts (prevent hanging tests).
    // ========================================================================
    [RelayCommand]
    private async Task TimeoutTest()
    {
        Log("--- Pattern 4: Timeout in Tests ---\n");

        Log("   ?? Test code:");
        Log("   [Fact]");
        Log("   public async Task FastOp_CompletesWithin1Second()");
        Log("   {");
        Log("       // WaitAsync throws TimeoutException if too slow.");
        Log("       var result = await service.FastOpAsync()");
        Log("           .WaitAsync(TimeSpan.FromSeconds(1));");
        Log("       Assert.NotNull(result);");
        Log("   }\n");

        // Test 1: Fast operation -- should succeed.
        Log("   [>] Test A: Fast operation (200ms) with 1-second timeout...");
        try
        {
            string result = await SimulatedFastOpAsync()
                .WaitAsync(TimeSpan.FromSeconds(1));
            Log($"   [OK] PASSED: Got \"{result}\" within timeout.\n");
        }
        catch (TimeoutException)
        {
            Log("   [X] FAILED: Operation timed out.\n");
        }

        // Test 2: Slow operation -- should timeout.
        Log("   [>] Test B: Slow operation (3s) with 1-second timeout...");
        try
        {
            string result = await SimulatedSlowOpAsync()
                .WaitAsync(TimeSpan.FromSeconds(1));
            Log($"   [X] FAILED: Should have timed out, got \"{result}\".");
        }
        catch (TimeoutException)
        {
            Log("   [OK] PASSED (expected): TimeoutException thrown -- test didn't hang!\n");
        }
    }

    // ========================================================================
    // DEMO 5: Mocking async dependencies.
    // ========================================================================
    [RelayCommand]
    private async Task MockingTest()
    {
        Log("--- Pattern 5: Mocking Async Dependencies ---\n");

        Log("   ?? Common mock return patterns:\n");

        Log("   // Return a value immediately (synchronous completion):");
        Log("   mock.Setup(s => s.GetAsync(1))");
        Log("       .ReturnsAsync(\"Alice\");");
        Log("   // Which is shorthand for:");
        Log("   mock.Setup(s => s.GetAsync(1))");
        Log("       .Returns(Task.FromResult(\"Alice\"));\n");

        Log("   // Return completed task (for async void-like methods):");
        Log("   mock.Setup(s => s.SaveAsync())");
        Log("       .Returns(Task.CompletedTask);\n");

        Log("   // Simulate failure:");
        Log("   mock.Setup(s => s.GetAsync(-1))");
        Log("       .Returns(Task.FromException<string>(");
        Log("           new InvalidOperationException(\"Not found\")));\n");

        // Demonstrate the patterns.
        Log("   [>] Demonstrating mock patterns:\n");

        // Task.FromResult
        Task<string> mockSuccess = Task.FromResult("Alice");
        string result = await mockSuccess;
        Log($"   [OK] Task.FromResult: {result}");

        // Task.CompletedTask
        Task mockVoid = Task.CompletedTask;
        await mockVoid;
        Log("   [OK] Task.CompletedTask: completed immediately");

        // Task.FromException
        Task<string> mockFailure = Task.FromException<string>(
            new InvalidOperationException("Not found"));
        try
        {
            await mockFailure;
        }
        catch (InvalidOperationException ex)
        {
            Log($"   [OK] Task.FromException: threw \"{ex.Message}\"");
        }

        Log("\n   [TIP] These patterns let you test async code without real I/O.\n");
    }

    // --- Simulated services for test demos ---

    private static async Task<string> SimulatedGetUserNameAsync(int userId)
    {
        if (userId < 0) throw new ArgumentException("User ID must be positive.", nameof(userId));
        await Task.Delay(100);
        return userId == 1 ? "Alice" : "Unknown";
    }

    private static async Task SimulatedLongWorkAsync(CancellationToken token)
    {
        for (int i = 0; i < 10; i++)
        {
            token.ThrowIfCancellationRequested();
            await Task.Delay(500, token);
        }
    }

    private static async Task<string> SimulatedFastOpAsync()
    {
        await Task.Delay(200);
        return "Fast result";
    }

    private static async Task<string> SimulatedSlowOpAsync()
    {
        await Task.Delay(3000);
        return "Slow result";
    }
}
