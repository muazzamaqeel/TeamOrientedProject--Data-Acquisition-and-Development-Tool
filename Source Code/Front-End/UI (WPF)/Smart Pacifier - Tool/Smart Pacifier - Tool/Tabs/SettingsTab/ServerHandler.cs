using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Renci.SshNet;
using Renci.SshNet.Common;

namespace Smart_Pacifier___Tool.Tabs.SettingsTab
{
    public class ServerHandler
    {
        private SshClient? sshClient;
        private ShellStream? shellStream;
        public event Action<string>? TerminalOutputReceived;

        public void InitializeSshConnection(string host, string username, string privateKeyPath)
        {
            try
            {
                var keyFile = new PrivateKeyFile(privateKeyPath);
                var keyFiles = new[] { keyFile };
                var connectionInfo = new ConnectionInfo(host, username, new PrivateKeyAuthenticationMethod(username, keyFiles));
                sshClient = new SshClient(connectionInfo);
                sshClient.Connect();

                if (sshClient.IsConnected)
                {
                    shellStream = sshClient.CreateShellStream("xterm", 80, 24, 800, 600, 1024);
                    Task.Run(() => ReadFromShellStream());
                    TerminalOutputReceived?.Invoke("Connected to server.\n");
                }
                else
                {
                    TerminalOutputReceived?.Invoke("Failed to connect to the server.\n");
                }
            }
            catch (SshConnectionException ex)
            {
                TerminalOutputReceived?.Invoke($"Connection error: {ex.Message}\n");
            }
            catch (SocketException ex)
            {
                TerminalOutputReceived?.Invoke($"Socket error: {ex.Message}. Verify network and host availability.\n");
            }
            catch (Exception ex)
            {
                TerminalOutputReceived?.Invoke($"Error: {ex.Message}\n");
            }
        }

        private async Task ReadFromShellStream()
        {
            var buffer = new byte[1024];
            int bytesRead;

            while (sshClient.IsConnected && (bytesRead = await shellStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                string output = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                output = Regex.Replace(output, @"\[\?\d{4}[lh]", "");
                TerminalOutputReceived?.Invoke(output);
            }
        }

        public void ExecuteCommand(string command)
        {
            if (shellStream != null && sshClient.IsConnected)
            {
                shellStream.WriteLine(command);
                TerminalOutputReceived?.Invoke($"\n$ {command}\n");
            }
        }

        public void DisconnectSsh()
        {
            if (sshClient != null && sshClient.IsConnected)
            {
                sshClient.Disconnect();
                sshClient.Dispose();
            }
        }

        // New methods for Docker and file operations, prefixed with "Server_"

        public void Server_CopyDockerFile(string sourcePath, string destinationPath)
        {
            try
            {
                File.Copy(sourcePath, destinationPath, true);
                TerminalOutputReceived?.Invoke($"Docker file copied from {sourcePath} to {destinationPath}.\n");
            }
            catch (Exception ex)
            {
                TerminalOutputReceived?.Invoke($"Error copying Docker file: {ex.Message}\n");
            }
        }

        public void Server_InitializeDockerImage()
        {
            ExecuteCommand("docker image build -t my_docker_image .");
            TerminalOutputReceived?.Invoke("Docker image initialization command executed.\n");
        }

        public void Server_StartDocker()
        {
            ExecuteCommand("docker start my_docker_container");
            TerminalOutputReceived?.Invoke("Docker start command executed.\n");
        }

        public void Server_StopDocker()
        {
            ExecuteCommand("docker stop my_docker_container");
            TerminalOutputReceived?.Invoke("Docker stop command executed.\n");
        }
    }
}
