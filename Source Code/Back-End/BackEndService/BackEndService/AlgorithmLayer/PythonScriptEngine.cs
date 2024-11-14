using SmartPacifier.Interface.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;

public class PythonScriptEngine : IAlgorithmLayer
{
    private static PythonScriptEngine? _instance;
    private static readonly object _lock = new object();

    private PythonScriptEngine() { }

    public static PythonScriptEngine GetInstance()
    {
        if (_instance == null)
        {
            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = new PythonScriptEngine();
                }
            }
        }
        return _instance;
    }

    public string ExecuteScript(string scriptName, string filePath)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = $"\"{scriptName}\" \"{filePath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(startInfo))
            {
                if (process == null)
                {
                    throw new InvalidOperationException("Process failed to start.");
                }

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                if (!string.IsNullOrEmpty(error))
                {
                    throw new Exception($"Python Error: {error}");
                }

                return output;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error executing Python script: {ex.Message}", "Execution Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return $"Error: {ex.Message}";
        }
    }
}
