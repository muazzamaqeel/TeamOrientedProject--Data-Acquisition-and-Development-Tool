using SmartPacifier.Interface.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;

namespace SmartPacifier.BackEnd.DatabaseLayer.InfluxDB.Connection
{
    public class LocalHostSetup : ILocalHost
    {
        private readonly string dockerComposeFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "docker-compose.yml");
        private readonly string apiKeyFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "apikey.txt");
        private readonly string mosquittoConfigFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mosquitto.conf");

        public LocalHostSetup()
        {
            if (File.Exists(dockerComposeFilePath))
            {
                MessageBox.Show($"docker-compose.yml found at: {dockerComposeFilePath}", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show($"docker-compose.yml NOT found at: {dockerComposeFilePath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void DockerInitialize()
        {
            if (!File.Exists(dockerComposeFilePath))
            {
                MessageBox.Show("Docker Compose file is missing. Initialization failed.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (IsDockerInstalled())
            {
                RunDockerInstall();
            }
            else
            {
                MessageBox.Show("Docker is not installed. Please install Docker Desktop to continue.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void RunDockerInstall()
        {
            try
            {
                SetEnvironmentVariableForMosquittoConfig();

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/K docker-compose -f \"{dockerComposeFilePath}\" up",
                    WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
                    UseShellExecute = true
                };

                Process process = Process.Start(startInfo);
                if (process == null)
                {
                    MessageBox.Show("Failed to start Docker initialization process.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing Docker: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void StartDocker()
        {
            if (AreContainersRunning())
            {
                MessageBox.Show("Docker containers are already running.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

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

        private bool AreContainersRunning()
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/C docker-compose -f \"{dockerComposeFilePath}\" ps -q",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = new Process { StartInfo = startInfo })
            {
                process.Start();
                string output = process.StandardOutput.ReadToEnd().Trim();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (!string.IsNullOrEmpty(error))
                {
                    MessageBox.Show($"Error checking Docker status: {error}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                return !string.IsNullOrEmpty(output);
            }
        }

        private bool IsDockerInstalled()
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/C docker --version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = new Process { StartInfo = startInfo })
                {
                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                    return !string.IsNullOrEmpty(output) && output.Contains("Docker version");
                }
            }
            catch
            {
                return false;
            }
        }

        private void RunDockerCommand(string command)
        {
            if (!File.Exists(dockerComposeFilePath))
            {
                MessageBox.Show($"docker-compose.yml not found at: {dockerComposeFilePath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            SetEnvironmentVariableForMosquittoConfig();

            string finalCommand = $"/C docker-compose -f \"{dockerComposeFilePath}\" {command}";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                finalCommand = $"/C docker-compose -f \"{dockerComposeFilePath}\" {command}";
            }
            else
            {
                // Linux/MacOS specific logic if necessary
            }

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = finalCommand,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = new Process { StartInfo = startInfo })
            {
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0 || (!string.IsNullOrEmpty(error) && !IsNormalDockerOutput(error)))
                {
                    MessageBox.Show($"Docker command error:\n{error}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else if (!string.IsNullOrWhiteSpace(output))
                {
                    MessageBox.Show($"Docker command succeeded:\n{output}", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void SetEnvironmentVariableForMosquittoConfig()
        {
            // Set environment variable for Mosquitto config file path
            Environment.SetEnvironmentVariable("MOSQUITTO_CONF_PATH", mosquittoConfigFilePath);
        }

        private bool IsNormalDockerOutput(string message)
        {
            return message.Contains("Created") || message.Contains("Starting") || message.Contains("Started") ||
                   message.Contains("Stopping") || message.Contains("Stopped") || message.Contains("Removing") ||
                   message.Contains("Removed") || message.Contains("Network");
        }

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
