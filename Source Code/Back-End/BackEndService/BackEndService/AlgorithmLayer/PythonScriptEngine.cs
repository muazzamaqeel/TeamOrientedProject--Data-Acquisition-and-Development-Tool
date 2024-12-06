using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

public class PythonScriptEngine
{
    private const int FixedPort = 5000; // Choose a fixed port for reuse

    public async Task<string> ExecuteScriptAsync(string scriptPath, string dataJson, bool usePort = true)
    {
        return usePort
            ? await ExecuteScriptWithPortAsync(scriptPath, dataJson)
            : await ExecuteScriptWithoutPortAsync(scriptPath, dataJson);
    }

    private async Task<string> ExecuteScriptWithPortAsync(string scriptPath, string dataJson)
    {
        string response = string.Empty;

        try
        {
            using (var listener = new TcpListener(IPAddress.Loopback, FixedPort))
            {
                listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                listener.Start();

                // Start the Python process
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "python",
                    Arguments = $"\"{scriptPath}\" {FixedPort}",
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                var pythonProcess = new Process { StartInfo = processStartInfo };
                pythonProcess.Start();

                // Wait for the client to connect and send data
                using (TcpClient client = await listener.AcceptTcpClientAsync())
                using (NetworkStream stream = client.GetStream())
                {
                    byte[] dataBytes = Encoding.UTF8.GetBytes(dataJson);
                    await stream.WriteAsync(dataBytes, 0, dataBytes.Length);
                    await stream.FlushAsync();

                    response = await ReadStreamDataAsync(stream);
                }

                string errorOutput = await pythonProcess.StandardError.ReadToEndAsync();
                if (!string.IsNullOrEmpty(errorOutput))
                {
                    throw new Exception($"Python script error: {errorOutput}");
                }
            }
        }
        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
        {
            response = $"Error: Port {FixedPort} is already in use. Ensure no other processes are using this port.";
        }
        catch (Exception ex)
        {
            response = $"Error executing Python script with port: {ex.Message}";
        }

        return response;
    }

    private async Task<string> ExecuteScriptWithoutPortAsync(string scriptPath, string dataJson)
    {
        string response = string.Empty;

        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = $"\"{scriptPath}\"",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var pythonProcess = new Process { StartInfo = processStartInfo })
            {
                pythonProcess.Start();

                // Write data to the process's input stream
                using (StreamWriter writer = pythonProcess.StandardInput)
                {
                    if (writer.BaseStream.CanWrite)
                    {
                        await writer.WriteAsync(dataJson);
                    }
                }

                // Read the output from the process
                response = await pythonProcess.StandardOutput.ReadToEndAsync();

                string errorOutput = await pythonProcess.StandardError.ReadToEndAsync();
                if (!string.IsNullOrEmpty(errorOutput))
                {
                    throw new Exception($"Python script error: {errorOutput}");
                }
            }
        }
        catch (Exception ex)
        {
            response = $"Error executing Python script without port: {ex.Message}";
        }

        return response;
    }

    private async Task<string> ReadStreamDataAsync(NetworkStream stream)
    {
        using (var memoryStream = new MemoryStream())
        {
            byte[] buffer = new byte[4096];
            int bytesRead;

            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                memoryStream.Write(buffer, 0, bytesRead);
            }

            return Encoding.UTF8.GetString(memoryStream.ToArray());
        }
    }
}
