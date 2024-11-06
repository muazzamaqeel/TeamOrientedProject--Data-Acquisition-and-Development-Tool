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
        private readonly string dockerComposeFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "docker-compose.yml");
        private readonly string mosquitoFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mosquitto.conf");
        private string remoteDirectory;

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

                    // Set the remote directory dynamically based on the current user's home directory
                    remoteDirectory = GetRemoteHomeDirectory();
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

        private string GetRemoteHomeDirectory()
        {
            // Run 'whoami' command to get the current user's home directory path
            string command = "echo $HOME";
            string homeDirectory = ExecuteCommandWithResult(command).Trim();

            if (string.IsNullOrEmpty(homeDirectory))
            {
                TerminalOutputReceived?.Invoke("Failed to retrieve remote home directory.\n");
                throw new Exception("Failed to retrieve remote home directory.");
            }

            TerminalOutputReceived?.Invoke($"Remote home directory set to: {homeDirectory}/SmartPacifier\n");
            return $"{homeDirectory}/SmartPacifier";
        }

        private string ExecuteCommandWithResult(string command)
        {
            if (sshClient == null || !sshClient.IsConnected) return string.Empty;

            using (var cmd = sshClient.CreateCommand(command))
            {
                return cmd.Execute();
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

        // Copy necessary Docker and config files to the server
        public void Server_CopyDockerFiles()
        {
            try
            {
                // Ensure the remote directory exists, using sudo for permissions
                ExecuteCommand($"sudo mkdir -p {remoteDirectory}");

                string remoteComposeFilePath = $"{remoteDirectory}/docker-compose.yml";
                string remoteMosquittoConfigPath = $"{remoteDirectory}/mosquitto.conf";

                if (sshClient?.IsConnected == true)
                {
                    using (var sftp = new SftpClient(sshClient.ConnectionInfo))
                    {
                        sftp.Connect();

                        // Re-upload docker-compose.yml
                        if (UploadFile(sftp, dockerComposeFilePath, remoteComposeFilePath, "docker-compose.yml"))
                        {
                            // Re-upload mosquitto.conf only if docker-compose.yml was uploaded
                            UploadFile(sftp, mosquitoFilePath, remoteMosquittoConfigPath, "mosquitto.conf");
                        }

                        sftp.Disconnect();
                        TerminalOutputReceived?.Invoke("All files re-uploaded to the user's SmartPacifier directory.\n");
                    }
                }
                else
                {
                    TerminalOutputReceived?.Invoke("SSH client is not connected.\n");
                }
            }
            catch (Exception ex)
            {
                TerminalOutputReceived?.Invoke($"Error in Server_CopyDockerFiles: {ex.Message}\n");
            }
        }

        // Helper method to upload a file via SFTP
        private bool UploadFile(SftpClient sftp, string localPath, string remotePath, string fileName)
        {
            if (File.Exists(localPath))
            {
                try
                {
                    using (var fileStream = new FileStream(localPath, FileMode.Open))
                    {
                        sftp.UploadFile(fileStream, remotePath, true);
                        TerminalOutputReceived?.Invoke($"{fileName} uploaded to {remotePath}.\n");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    TerminalOutputReceived?.Invoke($"Error uploading {fileName}: {ex.Message}\n");
                    return false;
                }
            }
            else
            {
                TerminalOutputReceived?.Invoke($"Local {fileName} not found.\n");
                return false;
            }
        }

        public void Server_InitializeDockerImage()
        {
            // Ensure that Docker Compose file and config file exist remotely before initializing
            ExecuteCommand($"export MOSQUITTO_CONF_PATH='{remoteDirectory}/mosquitto.conf' && sudo docker-compose -f {remoteDirectory}/docker-compose.yml up --build");
            TerminalOutputReceived?.Invoke("Docker Compose build and up command executed with sudo.\n");
        }

        public void Server_StartDocker()
        {
            ExecuteCommand($"sudo docker start mosquitto");
            ExecuteCommand($"sudo docker start influxdb");
            TerminalOutputReceived?.Invoke("Docker containers started with sudo.\n");
        }

        public void Server_StopDocker()
        {
            ExecuteCommand($"sudo docker stop mosquitto");
            ExecuteCommand($"sudo docker stop influxdb");
            TerminalOutputReceived?.Invoke("Docker containers stopped with sudo.\n");
        }
    }
}
