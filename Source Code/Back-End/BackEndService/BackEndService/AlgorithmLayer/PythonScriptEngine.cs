using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

public class PythonScriptEngine
{
    private Process _pythonProcess; // Track the Python process
    private bool _isStopped = false; // Guard to prevent multiple stops

    private const int InitialPort = 5000; // Starting port number
    private int currentPort = InitialPort; // Current port to use

    public async Task<string> ExecuteScriptWithTcpAsync(string scriptPath, string campaignDataJson)
    {
        string response = "";

        // Ensure any existing process using the port is terminated
        KillProcessUsingPort(currentPort);

        // Attempt to start a listener on the current port, with a retry on failure
        TcpListener listener = null;
        for (int attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                listener = new TcpListener(IPAddress.Loopback, currentPort);
                listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                listener.Start();
                break; // Break out of the loop if we successfully start the listener
            }
            catch (SocketException)
            {
                currentPort++; // Increment port and retry
                continue;
            }
        }

        if (listener == null)
        {
            string errorMsg = "Failed to start TCP listener after multiple attempts.";
            MessageBox.Show(errorMsg, "Execution Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return errorMsg;
        }

        try
        {
            // Start the Python process with the selected port
            _pythonProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "python",
                    Arguments = $"\"{scriptPath}\" {currentPort}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            _pythonProcess.Start();

            // Accept TCP connection from Python
            using (TcpClient client = await listener.AcceptTcpClientAsync())
            using (NetworkStream stream = client.GetStream())
            {
                // Send campaign data to Python script
                byte[] dataToSend = Encoding.UTF8.GetBytes(campaignDataJson);
                await stream.WriteAsync(dataToSend, 0, dataToSend.Length);

                // Read response from Python script
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    response = await reader.ReadToEndAsync();
                }
            }
        }
        catch (Exception ex)
        {
            string errorMsg = $"Execution Error: {ex.Message}";
            MessageBox.Show(errorMsg, "Execution Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return $"{errorMsg}\nDetails: {ex.StackTrace}";
        }
        finally
        {
            listener.Stop();
            _pythonProcess?.WaitForExit();
            _pythonProcess?.Dispose();
            _pythonProcess = null; // Reset the process reference
        }

        return response;
    }

    // Method to stop the Python script execution
    public void StopExecution()
    {
        lock (this)
        {
            if (_isStopped) return; // Prevent multiple stop calls
            _isStopped = true;

            try
            {
                if (_pythonProcess != null && !_pythonProcess.HasExited)
                {
                    _pythonProcess.Kill();
                }
            }
            catch (Exception ex)
            {
                // Handle exception if needed
            }
            finally
            {
                _pythonProcess?.Dispose();
                _pythonProcess = null;
            }
        }
    }

    // Method to kill any process using the specified port
    private void KillProcessUsingPort(int port)
    {
        var processInfo = new ProcessStartInfo
        {
            FileName = "netstat",
            Arguments = $"-aon | findstr :{port}",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (Process process = Process.Start(processInfo))
        {
            using (StreamReader reader = process.StandardOutput)
            {
                string result = reader.ReadToEnd();
                if (!string.IsNullOrWhiteSpace(result))
                {
                    // Extract the PID from the netstat output
                    var lines = result.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines)
                    {
                        var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 5 && int.TryParse(parts[4], out int pid))
                        {
                            try
                            {
                                Process.GetProcessById(pid).Kill();
                            }
                            catch (Exception ex)
                            {
                                // Handle exception if needed
                            }
                        }
                    }
                }
            }
        }
    }
}
