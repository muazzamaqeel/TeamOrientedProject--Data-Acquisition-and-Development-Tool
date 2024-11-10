using System;
using System.Diagnostics;
using System.Threading;

namespace SmartPacifier___TestingFramework.UnitTests.UTBackEnd.UTDatabaseLayer.UTInitializeDockerImage
{
    public class UTInitializeDockerImage
    {
        private const string DockerImage = "influxdb:2.7";
        private const string ContainerName = "influxdb-container";

        public void StartDockerContainer()
        {
            // Stop and remove any existing container with the same name
            RunDockerCommand($"rm -f {ContainerName}");

            // Check if the container is already running
            if (!IsContainerRunning())
            {
                Console.WriteLine("Starting InfluxDB container...");
                RunDockerCommand($"run --name {ContainerName} -d -p 8086:8086 {DockerImage}");
                WaitForContainerInitialization();
            }
            else
            {
                Console.WriteLine("InfluxDB container is already running.");
            }
        }

        private void RunDockerCommand(string arguments)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var process = Process.Start(startInfo);
            if (process != null)
            {
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (!string.IsNullOrEmpty(output))
                {
                    Console.WriteLine($"Docker output: {output}");
                }
                if (!string.IsNullOrEmpty(error))
                {
                    Console.WriteLine($"Docker error: {error}");
                }
            }
            else
            {
                throw new InvalidOperationException("Failed to start Docker process.");
            }
        }

        public bool IsContainerRunning()
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = $"ps -q --filter \"name={ContainerName}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var process = Process.Start(startInfo);
            var output = process?.StandardOutput.ReadToEnd();
            process?.WaitForExit();
            return !string.IsNullOrEmpty(output);
        }

        private void WaitForContainerInitialization()
        {
            Console.WriteLine("Waiting for InfluxDB container to initialize...");

            const int maxRetries = 10; // Maximum number of retries
            const int delayBetweenRetries = 3000; // Delay in milliseconds (3 seconds) between each retry

            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                if (IsContainerRunning())
                {
                    Console.WriteLine("InfluxDB container is now running and ready.");
                    return;
                }

                Thread.Sleep(delayBetweenRetries); // Wait before checking again
            }

            // Log Docker container logs if initialization fails
            Console.WriteLine("InfluxDB container failed to start within the expected time. Retrieving logs...");
            GetDockerLogs();

            throw new InvalidOperationException("InfluxDB container failed to start within the expected time.");
        }

        public void StopDockerContainer()
        {
            if (IsContainerRunning())
            {
                Console.WriteLine("Stopping InfluxDB container...");
                RunDockerCommand($"stop {ContainerName}");
                RunDockerCommand($"rm {ContainerName}");
                Console.WriteLine("InfluxDB container stopped and removed.");
            }
            else
            {
                Console.WriteLine("InfluxDB container is not running.");
            }
        }

        private void GetDockerLogs()
        {
            Console.WriteLine("Retrieving Docker logs...");
            RunDockerCommand($"logs {ContainerName}");
        }
    }
}
