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
                UseShellExecute = false,
                CreateNoWindow = true
            };
            var process = Process.Start(startInfo);
            process?.WaitForExit();
        }

        private bool IsContainerRunning()
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = $"ps -q --filter \"name={ContainerName}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            var process = Process.Start(startInfo);
            var output = process?.StandardOutput.ReadToEnd();
            process?.WaitForExit();
            return !string.IsNullOrEmpty(output);
        }

        private void WaitForContainerInitialization()
        {
            // Wait for a few seconds to let the container fully initialize
            Thread.Sleep(5000); // 5 seconds
            Console.WriteLine("InfluxDB container started and ready.");
        }

        public void StopDockerContainer()
        {
            if (IsContainerRunning())
            {
                Console.WriteLine("Stopping InfluxDB container...");
                RunDockerCommand($"stop {ContainerName}");
                RunDockerCommand($"rm {ContainerName}");
            }
            else
            {
                Console.WriteLine("InfluxDB container is not running.");
            }
        }
    }
}
