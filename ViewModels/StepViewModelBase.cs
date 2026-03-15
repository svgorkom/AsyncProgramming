using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AsynAwaitExamples.ViewModels;

/// <summary>
/// Base class for all step ViewModels. Provides common logging functionality
/// that was previously handled by direct TextBox manipulation in code-behind.
/// </summary>
public abstract partial class StepViewModelBase : ObservableObject
{
    private readonly StringBuilder _logBuilder = new();

    [ObservableProperty]
    private string _outputText = string.Empty;

    /// <summary>
    /// Appends a timestamped message to the output log.
    /// Replaces the code-behind Log() helper that directly manipulated TextBox controls.
    /// </summary>
    protected void Log(string message)
    {
        _logBuilder.AppendLine($"[{DateTime.Now:HH:mm:ss}] {message}");
        OutputText = _logBuilder.ToString();
    }

    /// <summary>
    /// Clears the output log.
    /// </summary>
    protected void ClearLog()
    {
        _logBuilder.Clear();
        OutputText = string.Empty;
    }
}
