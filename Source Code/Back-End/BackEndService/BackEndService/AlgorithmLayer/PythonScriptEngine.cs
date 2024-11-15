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
        File.AppendAllText(debugLogPath, "Starting ExecuteScriptAsync with Docker approach\n");

        // Log the received script path and confirm it is correct
        File.AppendAllText(debugLogPath, $"Provided script path: {scriptPath}\n");

        string containerScriptPath = "/scripts/python1.py";  // Path inside the Docker container

        // Check if the Python script exists at the computed path
        if (!File.Exists(scriptPath))
        {
            string errorMsg = $"Python script file not found at path:\n{scriptPath}";
            File.AppendAllText(debugLogPath, errorMsg + "\n");
            MessageBox.Show(errorMsg, "Script Not Found", MessageBoxButton.OK, MessageBoxImage.Error);
            return $"Error: Python script file not found at path:\n{scriptPath}";
        }

        File.AppendAllText(debugLogPath, $"Script path confirmed: {scriptPath}\n");

        // Step 1: Create /scripts directory in the container if it doesn’t exist
        var mkdirProcessInfo = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = $"exec python_service mkdir -p /scripts",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        RunProcess(mkdirProcessInfo, "Ensuring /scripts directory exists in Docker container");

        // Step 2: Copy the script into the Docker container
        var copyProcessInfo = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = $"cp \"{scriptPath}\" python_service:{containerScriptPath}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        RunProcess(copyProcessInfo, "Copying Python script to Docker container");

        return await Task.Run(() =>
        {
            try
            {
                // Step 3: Execute the Python script in the container
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = $"exec python_service python {containerScriptPath} \"{campaignDataJson}\"",
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

                    File.AppendAllText(debugLogPath, "Python script executed in Docker.\n");

                    if (!string.IsNullOrWhiteSpace(output))
                    {
                        File.AppendAllText(debugLogPath, "Python script output:\n" + output + "\n");
                    }

                    if (!string.IsNullOrWhiteSpace(error))
                    {
                        File.AppendAllText(debugLogPath, "Python script error:\n" + error + "\n");
                    }

                    string result = !string.IsNullOrWhiteSpace(output) ? output : error;

                    // Save the output to a text file
                    string resultFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "python_script_output.txt");
                    File.WriteAllText(resultFilePath, result);
                    File.AppendAllText(debugLogPath, $"Python script output saved to file: {resultFilePath}\n");

                    MessageBox.Show("Script executed in Docker and saved output. Check the log file for details.", "Execution Completed", MessageBoxButton.OK, MessageBoxImage.Information);

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

    // Helper function to run a process and log output
    private void RunProcess(ProcessStartInfo processInfo, string processDescription)
    {
        using (Process process = Process.Start(processInfo))
        {
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (!string.IsNullOrEmpty(output))
            {
                File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "execute_script_debug_log.txt"), $"{processDescription} Output: {output}\n");
            }
            if (!string.IsNullOrEmpty(error))
            {
                File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "execute_script_debug_log.txt"), $"{processDescription} Error: {error}\n");
            }
        }
    }
}
