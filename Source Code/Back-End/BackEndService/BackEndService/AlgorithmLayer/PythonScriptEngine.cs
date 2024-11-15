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
    public async Task<string> ExecuteScriptWithTcpAsync(string scriptPath, string campaignDataJson)
    {
        string debugLogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "execute_script_debug_log.txt");
        File.AppendAllText(debugLogPath, "Starting ExecuteScriptAsync with TCP Sockets\n");

        int port = 5000; // Define a port number for local communication
        string response = "";

        // Start the Python process
        var processInfo = new ProcessStartInfo
        {
            FileName = "python",
            Arguments = $"\"{scriptPath}\" {port}", // Pass the port to Python
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (Process process = Process.Start(processInfo))
        {
            // Start TCP listener to receive data from Python
            TcpListener listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start();

            try
            {
                File.AppendAllText(debugLogPath, "Waiting for a connection...\n");

                using (TcpClient client = await listener.AcceptTcpClientAsync())
                using (NetworkStream stream = client.GetStream())
                {
                    // Send campaign data to the Python script
                    byte[] dataToSend = Encoding.UTF8.GetBytes(campaignDataJson);
                    await stream.WriteAsync(dataToSend, 0, dataToSend.Length);
                    File.AppendAllText(debugLogPath, "Sent data to Python script.\n");

                    // Read response from the Python script
                    using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        response = await reader.ReadToEndAsync();
                        File.AppendAllText(debugLogPath, "Received response from Python script.\n");
                    }
                }
            }
            catch (Exception ex)
            {
                string errorMsg = $"Execution Error: {ex.Message}";
                File.AppendAllText(debugLogPath, errorMsg + "\nDetails:\n" + ex.StackTrace + "\n");
                MessageBox.Show(errorMsg, "Execution Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return $"{errorMsg}\nDetails: {ex.StackTrace}";
            }
            finally
            {
                listener.Stop();
                process.WaitForExit();
            }
        }

        return response;
    }
}
