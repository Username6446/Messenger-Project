using System.Configuration;
using System.Data;
using System.Windows;
using Messenger_Project.Services;

namespace Messenger_Project
{
    public partial class App : Application
    {
        public static ServerService? ServerService { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                ServerService = new ServerService();

                try
                {
                    ServerService.ConnectAsync(ServerService.ServerHost, ServerService.ServerPort).Wait(10000);

                    if (ServerService.IsConnected)
                    {
                        MessageBox.Show(
                            "✅ Connected to server successfully!",
                            "Connection Successful",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show(
                            $"⚠️ Could not connect to server.\n\nMake sure server is running on {ServerService.ServerHost}:{ServerService.ServerPort}\n\nSteps:\n1. Start MessengerServer project first\n2. Wait for 'Server started' message\n3. Then run this client",
                            "Connection Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }
                }
                catch (TimeoutException)
                {
                    MessageBox.Show(
                        $"⚠️ Connection timeout!\n\nServer is not responding within 10 seconds.\nMake sure MessengerServer is running on {ServerService.ServerHost}:{ServerService.ServerPort}",
                        "Connection Timeout",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"⚠️ Server connection failed:\n{ex.Message}\n\nServer must be running on {ServerService.ServerHost}:{ServerService.ServerPort}\n\nMake sure:\n1. MessengerServer.exe is running\n2. Database connection is configured\n3. Port {ServerService.ServerPort} is not blocked",
                        "Connection Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Startup error:\n\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            ServerService?.Disconnect();
            base.OnExit(e);
        }
    }
}