using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

public class PythonScriptEngine
{
    public async Task<string> ExecuteScriptAsync(string scriptPath, string dataJson, bool usePort = true)
    {
        if (usePort)
        {
            return await ExecuteScriptWithPortAsync(scriptPath, dataJson);
        }
        else
        {
            return await ExecuteScriptWithoutPortAsync(scriptPath, dataJson);
        }
    }

    private async Task<string> ExecuteScriptWithPortAsync(string scriptPath, string dataJson)
    {
        string response = string.Empty;
        TcpListener listener = null;
        int fixedPort = 5000; // Choose an appropriate port

        try
        {
            listener = new TcpListener(IPAddress.Loopback, fixedPort);
            listener.Start();
            Console.WriteLine($"TCP Listener started on port {fixedPort}");

            var processStartInfo = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = $"\"{scriptPath}\" {fixedPort}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var pythonProcess = new Process { StartInfo = processStartInfo };
            pythonProcess.Start();

            using (TcpClient client = await listener.AcceptTcpClientAsync())
            using (NetworkStream stream = client.GetStream())
            {
                byte[] dataBytes = Encoding.UTF8.GetBytes(dataJson);
                await stream.WriteAsync(dataBytes, 0, dataBytes.Length);

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
            response = $"Error executing Python script with port: {ex.Message}";
        }
        finally
        {
            listener?.Stop();
            Console.WriteLine("TCP Listener stopped.");
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

            var pythonProcess = new Process { StartInfo = processStartInfo };
            pythonProcess.Start();

            using (StreamWriter writer = pythonProcess.StandardInput)
            {
                if (writer.BaseStream.CanWrite)
                {
                    await writer.WriteLineAsync(dataJson);
                }
            }

            using (StreamReader reader = pythonProcess.StandardOutput)
            {
                response = await reader.ReadToEndAsync();
            }

            string errorOutput = await pythonProcess.StandardError.ReadToEndAsync();
            if (!string.IsNullOrEmpty(errorOutput))
            {
                throw new Exception($"Python script error: {errorOutput}");
            }
        }
        catch (Exception ex)
        {
            response = $"Error executing Python script without port: {ex.Message}";
        }

        return response;
    }
}

