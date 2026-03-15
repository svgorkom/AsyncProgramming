using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AsynAwaitExamples.ViewModels;

// ============================================================================
// STEP 12 VIEWMODEL: ASYNC STREAMS (IAsyncEnumerable<T>)
// ============================================================================
//
// KEY CONCEPTS:
// -------------
// 1. IAsyncEnumerable<T> — returns items one at a time with async pauses.
// 2. "yield return" in async methods — produces items lazily.
// 3. "await foreach" — consumes items as they arrive.
// 4. [EnumeratorCancellation] — receives WithCancellation token.
//
// MVVM NOTE:
// ----------
// Uses [RelayCommand(IncludeCancelCommand = true)] for built-in cancellation
// support, just like Step 6. The toolkit wires CancellationToken automatically.
// ============================================================================

public partial class Step12ViewModel : StepViewModelBase
{
    [ObservableProperty]
    private bool _isStreaming;

    /// <summary>
    /// Consumes an async stream of sensor readings.
    /// Each reading is displayed as soon as it arrives.
    /// </summary>
    [RelayCommand(IncludeCancelCommand = true)]
    private async Task StreamData(CancellationToken token)
    {
        IsStreaming = true;

        Log("?? Starting async stream of sensor data...\n");
        Log("   Each reading arrives one at a time (every 800ms).");
        Log("   Notice: items appear IMMEDIATELY — no waiting for all data!\n");

        try
        {
            int count = 0;

            await foreach (SensorReading reading in
                GetSensorReadingsAsync().WithCancellation(token))
            {
                count++;
                Log($"   ?? #{count}: Sensor={reading.SensorName}, " +
                    $"Temp={reading.Temperature:F1}°C, " +
                    $"Humidity={reading.Humidity}%");
            }

            Log($"\n? Stream completed. Received {count} readings.\n");
        }
        catch (OperationCanceledException)
        {
            Log("\n?? Stream was cancelled by user.\n");
        }
        finally
        {
            IsStreaming = false;
        }
    }

    // ========================================================================
    // THE ASYNC STREAM PRODUCER
    // ========================================================================

    private static async IAsyncEnumerable<SensorReading> GetSensorReadingsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var random = new Random();
        string[] sensors = ["Living Room", "Kitchen", "Bedroom", "Garage", "Garden"];

        for (int i = 0; i < 15; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(800, cancellationToken);

            yield return new SensorReading
            {
                SensorName = sensors[random.Next(sensors.Length)],
                Temperature = 18.0 + random.NextDouble() * 12.0,
                Humidity = random.Next(30, 80),
                Timestamp = DateTime.Now
            };
        }
    }
}

// ============================================================================
// A simple data class representing one sensor reading.
// ============================================================================
public class SensorReading
{
    public required string SensorName { get; init; }
    public double Temperature { get; init; }
    public int Humidity { get; init; }
    public DateTime Timestamp { get; init; }
}
