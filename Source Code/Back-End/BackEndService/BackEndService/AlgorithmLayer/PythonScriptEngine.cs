using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

public class PythonScriptEngine
{
    public async Task<string> ExecuteScriptAsync(string scriptPath, string campaignDataJson)
    {
        string debugLogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "execute_script_debug_log.txt");
        File.AppendAllText(debugLogPath, "Starting ExecuteScriptAsync with Process approach\n");

        string pythonExePath = "python"; // Ensure Python is accessible in the PATH
        string resultFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "python_script_output.txt");

        MessageBox.Show(scriptPath);


        if (!File.Exists(scriptPath))
        {
            string errorMsg = $"Python script file not found at path:\n{scriptPath}";
            File.AppendAllText(debugLogPath, errorMsg + "\n");
            MessageBox.Show(errorMsg, "Script Not Found", MessageBoxButton.OK, MessageBoxImage.Error);
            return $"Error: Python script file not found at path:\n{scriptPath}";
        }

        File.AppendAllText(debugLogPath, $"Script path confirmed: {scriptPath}\n");

        return await Task.Run(() =>
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = pythonExePath,
                    Arguments = $"\"{scriptPath}\" \"{campaignDataJson}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                File.AppendAllText(debugLogPath, $"Starting Python process with arguments: {psi.Arguments}\n");

                using (Process process = new Process { StartInfo = psi })
                {
                    process.Start();

                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    process.WaitForExit();

                    // Log that the script execution completed
                    File.AppendAllText(debugLogPath, "Python script executed.\n");

                    if (!string.IsNullOrWhiteSpace(output))
                    {
                        File.AppendAllText(debugLogPath, "Python script output:\n" + output + "\n");
                    }
                    else
                    {
                        File.AppendAllText(debugLogPath, "Python script produced no standard output.\n");
                    }

                    if (!string.IsNullOrWhiteSpace(error))
                    {
                        File.AppendAllText(debugLogPath, "Python script error:\n" + error + "\n");
                    }

                    string result = !string.IsNullOrWhiteSpace(output) ? output : error;

                    // Save the output to a text file
                    File.WriteAllText(resultFilePath, result);
                    File.AppendAllText(debugLogPath, $"Python script output saved to file: {resultFilePath}\n");

                    // Display the result in a message box
                    MessageBox.Show("Script executed and saved output. Check the log file for details.", "Execution Completed", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Open the output file in the default editor
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = resultFilePath,
                        UseShellExecute = true
                    });

                    return result;
                }
            }
            catch (Exception ex)
            {
                string errorMsg = $"Execution Error: {ex.Message}";
                File.AppendAllText(debugLogPath, errorMsg + "\nDetails:\n" + ex.StackTrace + "\n");
                MessageBox.Show(errorMsg, "Execution Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return $"{errorMsg}\nDetails: {ex.StackTrace}";
            }
        });
    }
}
