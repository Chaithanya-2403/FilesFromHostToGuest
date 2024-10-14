using Renci.SshNet;
using System;
using System.IO;

public class Program
{
    public static void Main(string[] args)
    {
        string host = "127.0.0.1"; // IP of the VM
        string username = "chaithanya";
        string password = "Chaithanya@24";

        // 1. Upload a single file
        string singleFilePath = @"D:\Linux\Host_to_Guest\Oct.docx"; // Path to the single file
        string remoteSingleFilePath = "/home/chaithanya/Documents/Host_to_Guest/Oct.docx"; // Path to the remote file

        // 2. Upload multiple files from a folder
        string localMultipleFilesDirectory = @"D:\Linux\Host_to_Guest\Multiple_Files"; // Path to the local folder
        string remoteMultipleFilesDirectory = "/home/chaithanya/Documents/Host_to_Guest"; // Path to the remote directory

        // 3. Upload an entire folder
        string localFolderPath = @"D:\Linux\Host_to_Guest\Folder"; // Path to the folder
        string remoteFolderPath = "/home/chaithanya/Documents/Host_to_Guest/Folder"; // Path to the remote folder

        // 4. Path for a file to download back from guest
        string fileToDownload = "/home/chaithanya/Documents/csvtotsv.tar";
        string localDownloadPath = @"D:\Linux\Guest_to_Host\downloaded_csvtotsv.tar";

        // Set up the connection
        using (var client = new SshClient(host, username, password))
        {
            try
            {
                client.Connect();
                Console.WriteLine("Connected to VM.");

                // Upload a single file
                UploadFile(client, singleFilePath, remoteSingleFilePath);

                // Upload multiple files from a folder
                UploadMultipleFiles(client, localMultipleFilesDirectory, remoteMultipleFilesDirectory);

                // Upload an entire folder (including subfolders)
                UploadDirectory(client, localFolderPath, remoteFolderPath);

                // Run a command on the guest and get output
                RunRemoteCommand(client, "ls -la /home/chaithanya");

                // Download a file from guest to host
                DownloadFile(client, fileToDownload, localDownloadPath);

                client.Disconnect();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }

    public static void UploadFile(SshClient client, string localFilePath, string remoteFilePath)
    {
        using (var scp = new ScpClient(client.ConnectionInfo))
        {
            try
            {
                scp.Connect();
                using (var fileStream = new FileStream(localFilePath, FileMode.Open))
                {
                    scp.Upload(fileStream, remoteFilePath);
                    Console.WriteLine($"Uploaded single file: {remoteFilePath}");
                }
                scp.Disconnect();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during upload: {ex.Message}");
            }
            finally
            {
                // Ensure the SCP connection is disconnected
                if (scp.IsConnected)
                {
                    scp.Disconnect();
                    Console.WriteLine("Disconnected from SCP.");
                }
            }
        }
    }

    public static void UploadMultipleFiles(SshClient client, string localDirectory, string remoteDirectory)
    {
        using (var scp = new ScpClient(client.ConnectionInfo))
        {
            try
            {
                scp.Connect();
                // Create remote directory if it doesn't exist
                client.RunCommand($"mkdir -p {remoteDirectory}");

                // Get all files in the local directory
                foreach (var filePath in Directory.GetFiles(localDirectory))
                {
                    // Use the remote directory without adding the local directory structure
                    //string remoteFilePath = Path.Combine(remoteDirectory, Path.GetFileName(filePath));
                    string remoteFilePath = Path.Combine(remoteDirectory, Path.GetFileName(filePath)).Replace("\\", "/"); // Replace backslash with forward slash
                    using (var fileStream = new FileStream(filePath, FileMode.Open))
                    {
                        scp.Upload(fileStream, remoteFilePath);
                        Console.WriteLine($"Uploaded: {remoteFilePath}");
                    }
                }
                scp.Disconnect();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during upload: {ex.Message}");
            }
            finally
            {
                // Ensure the SCP connection is disconnected
                if (scp.IsConnected)
                {
                    scp.Disconnect();
                    Console.WriteLine("Disconnected from SCP.");
                }
            }
        }
    }

    public static void UploadDirectory(SshClient client, string localDirectory, string remoteDirectory)
    {
        using (var scp = new ScpClient(client.ConnectionInfo))
        {
            try
            {
                scp.Connect();

                // Create the remote directory if it doesn't exist
                client.RunCommand($"mkdir -p {remoteDirectory}");

                // Upload files in the current directory
                foreach (var filePath in Directory.GetFiles(localDirectory))
                {
                    // Create remote file path
                    string remoteFilePath = $"{remoteDirectory}/{Path.GetFileName(filePath)}";
                    using (var fileStream = new FileStream(filePath, FileMode.Open))
                    {
                        scp.Upload(fileStream, remoteFilePath);
                        Console.WriteLine($"Uploaded: {remoteFilePath}");
                    }
                }

                // Recursively upload subdirectories
                foreach (var directory in Directory.GetDirectories(localDirectory))
                {
                    // Create remote subdirectory path
                    string remoteSubDirectory = $"{remoteDirectory}/{Path.GetFileName(directory)}";
                    UploadDirectory(client, directory, remoteSubDirectory); // Recursive call
                }
                scp.Disconnect();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during upload: {ex.Message}");
            }
            finally
            {
                // Ensure the SCP connection is disconnected
                if (scp.IsConnected)
                {
                    scp.Disconnect();
                    Console.WriteLine("Disconnected from SCP.");
                }
            }
        }
    }

    public static void RunRemoteCommand(SshClient client, string command)
    {
        try
        {
            var cmd = client.RunCommand(command);
            Console.WriteLine("Remote Command Output:");
            Console.WriteLine(cmd.Result);  // Display the output of the command
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error running command: {ex.Message}");
        }
    }

    public static void DownloadFile(SshClient client, string remoteFilePath, string localFilePath)
    {
        using (var scp = new ScpClient(client.ConnectionInfo))
        {
            try
            {
                scp.Connect();
                using (var fileStream = new FileStream(localFilePath, FileMode.Create))
                {
                    scp.Download(remoteFilePath, fileStream);
                    Console.WriteLine($"Downloaded file from guest: {remoteFilePath} to {localFilePath}");
                }
            }
            finally
            {
                scp.Disconnect();
            }
        }
    }

}
