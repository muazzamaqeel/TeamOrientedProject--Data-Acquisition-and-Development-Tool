using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

public class PythonScriptEngine
{
    private const int InitialPort = 5000; // Starting port number
    private int _currentPort = InitialPort;

    public async Task<string> ExecuteScriptAsync(string scriptPath, string dataJson)
    {
        string response = string.Empty;
        TcpListener listener = null;

        try
        {
            // Start a TCP listener on a free port
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    listener = new TcpListener(IPAddress.Loopback, _currentPort);
                    listener.Start();
                    break;
                }
                catch (SocketException)
                {
                    _currentPort++;
                }
            }

            if (listener == null)
            {
                throw new Exception("Failed to start TCP listener.");
            }

            var processStartInfo = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = $"\"{scriptPath}\" {_currentPort}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var pythonProcess = new Process
            {
                StartInfo = processStartInfo
            };

            pythonProcess.Start();

            using (TcpClient client = await listener.AcceptTcpClientAsync())
            using (NetworkStream stream = client.GetStream())
            {
                // Send data to Python script
                byte[] dataBytes = Encoding.UTF8.GetBytes(dataJson);
                await stream.WriteAsync(dataBytes, 0, dataBytes.Length);

                // Receive response from Python script
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    response = await reader.ReadToEndAsync();
                }
            }

            string errorOutput = await pythonProcess.StandardError.ReadToEndAsync();
            if (!string.IsNullOrEmpty(errorOutput))
            {
                throw new Exception($"Python script error: {errorOutput}");
            }
        }
        catch (Exception ex)
        {
            response = $"Error executing Python script: {ex.Message}";
        }
        finally
        {
            listener?.Stop();
        }

        return response;
    }
}
