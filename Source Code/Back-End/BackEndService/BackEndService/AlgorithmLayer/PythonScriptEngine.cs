using SmartPacifier.Interface.Services;
using System.Diagnostics;
using System.IO;
using System.Windows;
using Newtonsoft.Json.Linq;

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

    public string ExecuteScript(string scriptNameOrCode)
    {
        try
        {
            // Define the base directory once
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string? scriptPath = null;

            if (IsInlineCode(scriptNameOrCode))
            {
                // If it's inline code, we won't use a file path
                scriptPath = null;
            }
            else
            {
                // Assume it's a file name, construct the full path based on the configuration
                var scriptRelativePath = Path.Combine(baseDirectory, @"..\..\..\Resources\OutputResources\PythonFiles\ExecutableScript", scriptNameOrCode);
                scriptPath = Path.GetFullPath(scriptRelativePath);

                // Check if the script file exists
                if (!File.Exists(scriptPath))
                {
                    MessageBox.Show($"Python script not found at: {scriptPath}", "File Not Found", MessageBoxButton.OK, MessageBoxImage.Error);
                    throw new FileNotFoundException("Python script file not found.", scriptPath);
                }
            }

            // Define the relative path for the output directory
            var outputDirectory = Path.Combine(baseDirectory, @"..\..\..\Resources\OutputResources\PythonFiles\GeneratedData\");
            Directory.CreateDirectory(outputDirectory); // Ensure the output directory exists
            var outputFile = Path.Combine(outputDirectory, "script_output.txt");

            var startInfo = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = scriptPath != null ? $"\"{scriptPath}\"" : $"-c \"{scriptNameOrCode}\"",
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

                // Write output to a file in the GeneratedData folder
                File.WriteAllText(outputFile, output);

                return output;
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Error executing Python script: {ex.Message}");
        }
    }

    // Helper method to determine if the input is inline code or a file name
    private bool IsInlineCode(string input)
    {
        // Check if the input contains any Python code keywords or characters that are not typical in file names
        return input.Contains(" ") || input.Contains("\n") || input.Contains("=") || input.Contains("print");
    }
}
