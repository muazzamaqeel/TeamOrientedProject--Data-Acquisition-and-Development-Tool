using SmartPacifier.Interface.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace SmartPacifier.BackEnd.DatabaseLayer.InfluxDB.Connection
{
    public class LocalHostSetup : ILocalHost
    {
        // Dynamically sets the path to the `docker-compose.yml` in the Release directory
        private readonly string dockerComposeFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LocalHost", "docker-compose.yml");
        private readonly string apiKeyFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "apikey.txt");


        public LocalHostSetup()
        {
            // Check if file exists and display the path for debugging
            if (File.Exists(dockerComposeFilePath))
            {
                MessageBox.Show($"docker-compose.yml found at: {dockerComposeFilePath}", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show($"docker-compose.yml NOT found at: {dockerComposeFilePath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void StartDocker()
        {
            try
            {
                RunDockerCommand("up -d");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to start Docker: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void StopDocker()
        {
            try
            {
                RunDockerCommand("down");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to stop Docker: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void RunDockerCommand(string command)
        {
            if (!File.Exists(dockerComposeFilePath))
            {
                MessageBox.Show($"docker-compose.yml not found at: {dockerComposeFilePath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/C docker-compose -f \"{dockerComposeFilePath}\" {command}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = false
            };

            using (Process process = Process.Start(startInfo))
            {
                if (process == null)
                {
                    throw new Exception("Failed to start Docker process.");
                }

                process.WaitForExit();

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                // Check for critical errors only (e.g., actual errors, not standard operation messages)
                if (!string.IsNullOrWhiteSpace(error) && !IsNormalDockerOutput(error))
                {
                    MessageBox.Show($"Docker command error: {error}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    // Show success or informational messages for non-critical output
                    if (!string.IsNullOrWhiteSpace(output))
                    {
                        MessageBox.Show($"Docker command output:\n{output}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }

                    if (!string.IsNullOrWhiteSpace(error))
                    {
                        MessageBox.Show($"Docker command info:\n{error}", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
        }

        // Helper method to determine if the output is a normal Docker action, not an error
        private bool IsNormalDockerOutput(string message)
        {
            return message.Contains("Creating") || message.Contains("Started") || message.Contains("Stopping") ||
                   message.Contains("Removing") || message.Contains("Created") || message.Contains("Started") ||
                   message.Contains("Network") || message.Contains("Container");
        }

        // Store API Key in a text file
        public void SaveApiKey(string apiKey)
        {
            try
            {
                File.WriteAllText(apiKeyFilePath, apiKey);
                MessageBox.Show("API Key saved successfully.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save API Key: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Retrieve API Key from the text file
        public string GetApiKey()
        {
            try
            {
                if (File.Exists(apiKeyFilePath))
                {
                    return File.ReadAllText(apiKeyFilePath);
                }
                else
                {
                    MessageBox.Show("API Key file not found.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to retrieve API Key: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return string.Empty;
            }
        }
    }
}
