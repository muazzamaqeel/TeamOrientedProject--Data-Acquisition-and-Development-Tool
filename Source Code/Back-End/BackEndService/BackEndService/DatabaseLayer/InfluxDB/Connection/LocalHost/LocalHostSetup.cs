using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
        private readonly string configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");

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

                Process? process = Process.Start(startInfo);
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

        public void DockerInitialize()
        {
            if (!File.Exists(dockerComposeFilePath))
            {
                MessageBox.Show("Docker Compose file is missing. Initialization failed.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!IsDockerInstalled())
            {
                MessageBox.Show("Docker is not installed. Please install Docker Desktop to continue.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!IsDockerRunning())
            {
                MessageBox.Show("Docker Desktop is not running. Please open Docker Desktop and initialize it locally to continue.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            RunDockerInstall();
        }

        private bool IsDockerRunning()
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/C docker info",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = new Process { StartInfo = startInfo })
                {
                    process.Start();
                    process.WaitForExit();
                    return process.ExitCode == 0;
                }
            }
            catch
            {
                return false;
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

        public void SaveApiKey(string apiKey, bool isLocal)
        {
            // Determine the path to the project root by navigating up from the bin directory
            string projectDirectory = AppDomain.CurrentDomain.BaseDirectory;
            for (int i = 0; i < 4; i++)  // Go up four levels
            {
                projectDirectory = Directory.GetParent(projectDirectory)?.FullName;
            }

            // Now, define the path to the original config.json in the project structure
            string configFilePath = Path.Combine(projectDirectory, "Resources", "OutputResources", "config.json");

            if (!File.Exists(configFilePath))
            {
                MessageBox.Show("Config file not found in the original Resources/OutputResources folder.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                // Load the JSON file
                var json = File.ReadAllText(configFilePath);
                dynamic config = JsonConvert.DeserializeObject(json);

                // Update the correct API key based on the isLocal flag
                if (isLocal)
                {
                    config.LocalDatabaseConfiguration.ApiKey = apiKey;
                }
                else
                {
                    config.ServerDatabaseConfiguration.ApiKey = apiKey;
                }

                // Save the updated JSON back to the original file
                string output = JsonConvert.SerializeObject(config, Formatting.Indented);
                File.WriteAllText(configFilePath, output);

                MessageBox.Show($"API Key saved successfully to config.json in {configFilePath}.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save API Key: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public string GetApiKey(bool isLocal)
        {
            // Determine the path to the project root by navigating up from the bin directory
            string projectDirectory = AppDomain.CurrentDomain.BaseDirectory;
            for (int i = 0; i < 4; i++)  // Go up four levels to reach the project root
            {
                projectDirectory = Directory.GetParent(projectDirectory)?.FullName;
            }

            // Now, define the path to the original config.json in the project structure
            string configFilePath = Path.Combine(projectDirectory, "Resources", "OutputResources", "config.json");

            if (!File.Exists(configFilePath))
            {
                MessageBox.Show("Config file not found in the original Resources/OutputResources folder.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return string.Empty;
            }

            try
            {
                // Load the config file as a JSON object
                var json = File.ReadAllText(configFilePath);
                var config = JObject.Parse(json);

                // Retrieve the API Key from the appropriate section based on isLocal
                string section = isLocal ? "LocalDatabaseConfiguration" : "ServerDatabaseConfiguration";
                return config[section]?["ApiKey"]?.ToString() ?? string.Empty;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to retrieve API Key: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return string.Empty;
            }
        }

    }
}
