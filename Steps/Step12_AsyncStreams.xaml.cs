using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace AsynAwaitExamples.Steps;

// ============================================================================
// STEP 12: ASYNC STREAMS (IAsyncEnumerable<T>)
// ============================================================================
//
// KEY CONCEPTS:
// -------------
// 1. IAsyncEnumerable<T>
//    - The async version of IEnumerable<T>.
//    - Instead of returning ALL items at once, it returns them one at a time,
//      with an async pause between each one.
//    - The method uses "yield return" to produce items (just like IEnumerable).
//    - The caller uses "await foreach" to consume items.
//
// 2. "yield return" in async methods
//    - Works exactly like regular yield return, but the method is async.
//    - Each "yield return" sends one item to the caller.
//    - Between yields, the method can do async work (like await Task.Delay).
//
// 3. "await foreach"
//    - The async version of "foreach".
//    - It awaits each item from the IAsyncEnumerable.
//    - The loop body runs once per item, as each item becomes available.
//
// 4. Cancellation with [EnumeratorCancellation]
//    - You can pass a CancellationToken to an async stream using:
//      await foreach (var item in GetDataAsync().WithCancellation(token))
//    - In the producer method, decorate the CancellationToken parameter with
//      [EnumeratorCancellation] to receive the token.
//
// COMPARISON:
// -----------
//   Traditional:  Task<List<Reading>> GetAllReadingsAsync()
//                 ? You wait for ALL readings, then process them.
//                 ? Memory: stores ALL readings in a list.
//
//   Async Stream: IAsyncEnumerable<Reading> GetReadingsAsync()
//                 ? You process EACH reading as it arrives.
//                 ? Memory: only ONE reading at a time.
//                 ? Faster perceived response (first item shows up immediately).
// ============================================================================

public partial class Step12_AsyncStreams : Page
{
    private CancellationTokenSource? _cts;

    public Step12_AsyncStreams()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Consumes an async stream of sensor readings.
    /// Each reading is displayed as soon as it arrives — no waiting for all data!
    /// </summary>
    private async void StreamData_Click(object sender, RoutedEventArgs e)
    {
        _cts = new CancellationTokenSource();
        StartButton.IsEnabled = false;
        StopButton.IsEnabled = true;

        Log("?? Starting async stream of sensor data...\n");
        Log("   Each reading arrives one at a time (every 800ms).");
        Log("   Notice: items appear IMMEDIATELY — no waiting for all data!\n");

        try
        {
            int count = 0;

            // ================================================================
            // "await foreach" — the key syntax for consuming async streams.
            // 
            // This loop runs once per item. Between iterations, it AWAITS
            // the next item — the UI stays responsive!
            //
            // .WithCancellation() passes our CancellationToken into the stream,
            // so the producer can stop generating items when we cancel.
            // ================================================================
            await foreach (SensorReading reading in
                GetSensorReadingsAsync().WithCancellation(_cts.Token))
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
            _cts.Dispose();
            _cts = null;
            StartButton.IsEnabled = true;
            StopButton.IsEnabled = false;
        }
    }

    private void Stop_Click(object sender, RoutedEventArgs e)
    {
        _cts?.Cancel();
    }

    // ========================================================================
    // THE ASYNC STREAM PRODUCER
    // ========================================================================
    // This method returns IAsyncEnumerable<SensorReading>.
    // It uses "yield return" to produce items one at a time.
    // Between each item, it does async work (simulated delay).
    //
    // [EnumeratorCancellation] tells the compiler: "When someone calls
    // .WithCancellation(token), put that token into this parameter."
    // ========================================================================

    private static async IAsyncEnumerable<SensorReading> GetSensorReadingsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var random = new Random();
        string[] sensors = ["Living Room", "Kitchen", "Bedroom", "Garage", "Garden"];

        // Simulate 15 sensor readings arriving over time.
        for (int i = 0; i < 15; i++)
        {
            // Check if we should stop.
            cancellationToken.ThrowIfCancellationRequested();

            // Simulate waiting for the next sensor reading.
            await Task.Delay(800, cancellationToken);

            // "yield return" sends ONE item to the caller immediately.
            // The caller's "await foreach" loop processes it right away.
            // Then execution comes back here for the next iteration.
            yield return new SensorReading
            {
                SensorName = sensors[random.Next(sensors.Length)],
                Temperature = 18.0 + random.NextDouble() * 12.0,  // 18-30°C
                Humidity = random.Next(30, 80),                     // 30-80%
                Timestamp = DateTime.Now
            };
        }
    }

    private void Log(string message)
    {
        Output.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
        Output.ScrollToEnd();
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
