using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using Xunit;
using System.Text.Json;

public class TestTcpPythonServer : IDisposable
{
    private readonly string _scriptsDirectory;
    private readonly string _pythonPath = "python"; // Adjust path if Python is not in PATH
    private readonly int _testPort = 5005;
    private Process _pythonServerProcess;
    private readonly string _scriptPath;

    public TestTcpPythonServer()
    {
        // Set up directory for the Python script
        _scriptsDirectory = Path.Combine(Path.GetTempPath(), "PythonScriptsTest_" + Guid.NewGuid());
        Directory.CreateDirectory(_scriptsDirectory);

        // Define the Python script path
        _scriptPath = Path.Combine(_scriptsDirectory, "test_script.py");

        // Write the Python server script to the file
        File.WriteAllText(_scriptPath, GetPythonServerScript());

        // Start the Python server process
        StartPythonServer();

        // Wait briefly to ensure the server is up
        Thread.Sleep(1000);
    }

    private string GetPythonServerScript()
    {
        return @"
import socket
import json
import threading

def process_data(data):
    if 'CampaignName' in data:
        data['processed'] = True
    else:
        data['error'] = 'Missing CampaignName'
    return data

def process_with_port(port):
    server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server_socket.bind(('127.0.0.1', port))  # Explicitly bind to IPv4
    server_socket.listen(1)
    print(f'Python server is listening on port {port}')

    while True:
        client_socket, _ = server_socket.accept()
        data = client_socket.recv(1024).decode()

        try:
            if data == 'health_check':
                client_socket.sendall(b'OK')
            else:
                payload = json.loads(data)
                response = process_data(payload)
                client_socket.sendall(json.dumps(response).encode())
        except json.JSONDecodeError:
            client_socket.sendall(json.dumps({'error': 'Invalid JSON'}).encode())
        finally:
            client_socket.close()

if __name__ == '__main__':
    import sys
    port = int(sys.argv[1])
    health_check_thread = threading.Thread(target=process_with_port, args=(port,))
    health_check_thread.daemon = True
    health_check_thread.start()
    
    # Keep the main thread alive to allow the server to run
    try:
        while True:
            pass
    except KeyboardInterrupt:
        pass
";
    }

    private void StartPythonServer()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = _pythonPath,
            Arguments = $"{_scriptPath} {_testPort}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = _scriptsDirectory
        };

        _pythonServerProcess = new Process { StartInfo = startInfo };
        _pythonServerProcess.OutputDataReceived += (sender, args) =>
        {
            if (!string.IsNullOrEmpty(args.Data))
            {
                Console.WriteLine($"[Python] {args.Data}");
            }
        };
        _pythonServerProcess.ErrorDataReceived += (sender, args) =>
        {
            if (!string.IsNullOrEmpty(args.Data))
            {
                Console.WriteLine($"[Python Error] {args.Data}");
            }
        };

        _pythonServerProcess.Start();
        _pythonServerProcess.BeginOutputReadLine();
        _pythonServerProcess.BeginErrorReadLine();

        // Optionally, wait for the server to start listening
        WaitForServerToStart();
    }

    private void WaitForServerToStart()
    {
        int retries = 10;
        while (retries > 0)
        {
            try
            {
                using (var client = new TcpClient("127.0.0.1", _testPort))
                {
                    // If connection succeeds, server is up
                    return;
                }
            }
            catch (SocketException)
            {
                // Server not ready yet
                Thread.Sleep(500);
                retries--;
            }
        }

        throw new Exception("Failed to start Python server.");
    }

    [Fact]
    public void HealthCheck_ShouldReturnOK()
    {
        var response = SendMessage("health_check");
        Assert.Equal("OK", response);
    }

    [Fact]
    public void ValidJsonWithCampaignName_ShouldBeProcessed()
    {
        var payload = new { CampaignName = "TestCampaign" };
        var response = SendMessage(JsonSerialize(payload));
        var actual = JsonDeserialize(response);

        Assert.True(actual.ContainsKey("CampaignName"), "Response does not contain 'CampaignName'");
        Assert.Equal("TestCampaign", actual["CampaignName"].GetString());

        Assert.True(actual.ContainsKey("processed"), "Response does not contain 'processed'");
        Assert.True(actual["processed"].GetBoolean());
    }

    [Fact]
    public void ValidJsonWithoutCampaignName_ShouldReturnError()
    {
        var payload = new { SomeOtherField = "Value" };
        var response = SendMessage(JsonSerialize(payload));
        var actual = JsonDeserialize(response);

        Assert.True(actual.ContainsKey("SomeOtherField"), "Response does not contain 'SomeOtherField'");
        Assert.Equal("Value", actual["SomeOtherField"].GetString());

        Assert.True(actual.ContainsKey("error"), "Response does not contain 'error'");
        Assert.Equal("Missing CampaignName", actual["error"].GetString());
    }

    [Fact]
    public void InvalidJson_ShouldReturnError()
    {
        var invalidJson = "{ this is not valid JSON }";
        var response = SendMessage(invalidJson);
        var actual = JsonDeserialize(response);

        Assert.True(actual.ContainsKey("error"), "Response does not contain 'error'");
        Assert.Equal("Invalid JSON", actual["error"].GetString());
    }

    private string SendMessage(string message)
    {
        using (var client = new TcpClient())
        {
            client.Connect("127.0.0.1", _testPort);
            using (var networkStream = client.GetStream())
            {
                var data = Encoding.UTF8.GetBytes(message);
                networkStream.Write(data, 0, data.Length);

                // Buffer to store the response bytes.
                var buffer = new byte[1024];
                int bytesRead = networkStream.Read(buffer, 0, buffer.Length);
                return Encoding.UTF8.GetString(buffer, 0, bytesRead);
            }
        }
    }

    private string JsonSerialize(object obj)
    {
        return System.Text.Json.JsonSerializer.Serialize(obj, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = null, // Preserve property names as-is
            WriteIndented = false
        });
    }

    private Dictionary<string, JsonElement> JsonDeserialize(string json)
    {
        return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
    }

    public void Dispose()
    {
        try
        {
            if (!_pythonServerProcess.HasExited)
            {
                _pythonServerProcess.Kill();
                _pythonServerProcess.WaitForExit(2000);
            }
        }
        catch
        {
            // Ignore any exceptions during cleanup
        }

        // Clean up the scripts directory
        try
        {
            if (Directory.Exists(_scriptsDirectory))
            {
                Directory.Delete(_scriptsDirectory, true);
            }
        }
        catch
        {
            // Ignore any exceptions during cleanup
        }
    }
}
