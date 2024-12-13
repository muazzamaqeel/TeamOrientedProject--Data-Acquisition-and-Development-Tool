using Microsoft.Extensions.DependencyInjection;
using SmartPacifier.Interface.Services;
using SmartPacifier.BackEnd.Database.InfluxDB.Connection;
using SmartPacifier.BackEnd.DatabaseLayer.InfluxDB.Connection;
using SmartPacifier.BackEnd.DatabaseLayer.InfluxDB.Managers;
using InfluxDB.Client;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using SmartPacifier.BackEnd.Database.InfluxDB.Managers;
using Smart_Pacifier___Tool.Tabs.DeveloperTab;
using Smart_Pacifier___Tool.Tabs.SettingsTab;
using Smart_Pacifier___Tool.Tabs.CampaignsTab;
using Smart_Pacifier___Tool.Tabs.AlgorithmTab;
using SmartPacifier.BackEnd.CommunicationLayer.MQTT;
using Smart_Pacifier___Tool.Tabs.MonitoringTab;
using System.Configuration;
using System.IO;
using System.Text.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Smart_Pacifier___Tool.Tabs.AlgorithmTab.AlgoExtra;
using SmartPacifier.BackEnd.DatabaseLayer.InfluxDB.LineProtocol;

namespace Smart_Pacifier___Tool
{
    public partial class App : Application
    {
        // The IServiceProvider instance that manages the lifetime of services and dependencies
        private IServiceProvider? _serviceProvider;

        // Mutex instances to prevent multiple instances of the application and the broker
        private static Mutex? _appMutex;
        private static Mutex? _brokerMutex;

        // Expose the service provider publicly so other components can access it
        public IServiceProvider ServiceProvider => _serviceProvider!;

        [DllImport("kernel32.dll")]
        private static extern bool AllocConsole();

        [DllImport("kernel32.dll")]
        private static extern bool FreeConsole();

        private const string ThemeKey = "SelectedTheme";

        /// <summary>
        /// The entry point of the application.
        /// This method is executed when the application starts.
        /// We use the Microsoft.Extensions.DependencyInjection library to set up Dependency Injection (DI).
        /// </summary>
        /// <param name="e">Startup event arguments.</param>
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Ensure only one instance of the main application
            bool isAppNewInstance;
            _appMutex = new Mutex(true, "SmartPacifierMainAppMutex", out isAppNewInstance);
            if (!isAppNewInstance)
            {
                MessageBox.Show("The application is already running.");
                Shutdown();
                return;
            }

            // Ensure only one instance of the MQTT client
            bool isBrokerNewInstance;
            _brokerMutex = new Mutex(true, "SmartPacifierBrokerMutex", out isBrokerNewInstance);

            if (!isBrokerNewInstance)
            {
                MessageBox.Show("The MQTT client is already running.");
                Shutdown();
                return;
            }

            // Allocate a console window for logging
            AllocConsole();

            // Retrieve the saved theme URI from settings
            string? themeUri = ConfigurationManager.AppSettings[ThemeKey];
            if (string.IsNullOrEmpty(themeUri))
            {
                themeUri = "Resources/ColorsDark.xaml";
            }

            ApplyTheme(themeUri);

            // Create a new service collection that will hold our service registrations
            var services = new ServiceCollection();

            // Call the method that registers all necessary services
            ConfigureServices(services);
            Console.WriteLine(
                $"MQTT Configuration: Host={Broker.Instance.BrokerAddress}; Port={Broker.Instance.BrokerPort}");

            // Build the service provider from the service collection (This is the Dependency Injection container)
            _serviceProvider = services.BuildServiceProvider();

            // Run BrokerMain asynchronously to avoid blocking the main UI thread
            var brokerMain = _serviceProvider.GetRequiredService<IBrokerMain>();
            await brokerMain.StartAsync([]); // Await the task directly


            /*
            //ExecutePythonScript();
            var config = LoadDatabaseConfiguration();
            string scriptName = config.PythonScript?.FileName ?? "python1.py"; // Default to "python1.py" if not specified
            var pythonScriptEngine = _serviceProvider.GetRequiredService<IAlgorithmLayer>();
            try
            {
                string result = pythonScriptEngine.ExecuteScript(scriptName);
                MessageBox.Show($"Python script '{scriptName}' executed successfully.", "Execution Success", MessageBoxButton.OK);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error executing Python script:\n{ex.Message}", "Execution Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            */
        }

        /// <summary>
        /// Configures and registers all the services, managers, and other dependencies.
        /// This is where we register the InfluxDB client, services, managers, and UI components.
        /// </summary>
        /// <param name="services">The service collection where services are registered.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            // Load database configuration from config.json
            var config = AppConfiguration.LoadDatabaseConfiguration();

            // Determine which configuration to use
            bool useLocal = config.UseLocal == true;

            // Set up the appropriate host and API key based on the configuration
            string? host = useLocal ? config.Local?.Host : $"{config.Server?.Host}:{config.Server?.Port}";
            string? apiKey = useLocal ? config.Local?.ApiKey : config.Server?.ApiKey;

            // Configure broker
            var broker = Broker.Instance;
            broker.BrokerAddress = config.Mqtt?.Host ?? "localhost";
            broker.BrokerPort = config.Mqtt?.Port ?? 1883;

            // Check if Host or ApiKey is missing and throw an exception with a detailed message
            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(apiKey))
            {
                MessageBox.Show(
                    "Host or API key is missing or improperly configured. Please check your configuration file.",
                    "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                throw new InvalidOperationException("Host or API key is missing or improperly configured.");
            }

            // Display which database is being used
            string databaseType = useLocal ? "Local Database" : "Server Database";
            //MessageBox.Show($"Using {databaseType} at {host}", "Database Configuration", MessageBoxButton.OK, MessageBoxImage.Information);

            // Ensure the host has the correct URI format
            if (!host.StartsWith("http://") && !host.StartsWith("https://"))
            {
                host = "http://" + host;
            }

            // Register InfluxDBClient with the validated host and API key
            services.AddSingleton<InfluxDBClient>(sp => new InfluxDBClient(host, apiKey));

            // Register InfluxDatabaseService as IDatabaseService
            services.AddSingleton<IDatabaseService>(sp =>
            {
                var influxClient = sp.GetRequiredService<InfluxDBClient>();
                string org = "thu-de"; // Keep your org consistent
                return new InfluxDatabaseService(
                    influxClient,
                    apiKey, // API key retrieved from the configuration
                    host, // baseUrl from configuration
                    org // organization name
                );
            });

            // Register other necessary services
            services.AddTransient<FileUpload>();
            services.AddSingleton<ILocalHost, LocalHostSetup>();
            services.AddSingleton<IBrokerHealthService, BrokerHealth>();
            services.AddSingleton<IManagerPacifiers, ManagerPacifiers>();
            services.AddSingleton<IManagerCampaign, ManagerCampaign>();
            services.AddSingleton<IManagerSensors, ManagerSensors>();
            services.AddSingleton<MainWindow>();
            services.AddSingleton<DeveloperView>();
            services.AddTransient<AlgorithmView>();
            services.AddTransient<AlgoSelection>();
            services.AddSingleton<IBrokerMain, BrokerMain>();

            // UI component registration
            services.AddTransient<PacifierSelectionView>();
            services.AddTransient<Func<string, SettingsView>>(sp => (defaultView) =>
            {
                var localHostService = sp.GetRequiredService<ILocalHost>();
                var brokerHealthService = sp.GetRequiredService<IBrokerHealthService>(); // Add this
                return new SettingsView(localHostService, brokerHealthService, defaultView); // Pass both
            });

            services.AddTransient<CampaignsView>();
        }

        /// <summary>
        /// Applies the passed theme, clears all current resource dictionaries and adds them back
        /// </summary>
        /// <param name="themeUri">The URI of the theme that should be applied, either dark or light</param>
        public void ApplyTheme(string themeUri)
        {
            // Save the selected theme URI to settings
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            config.AppSettings.Settings[ThemeKey].Value = themeUri;
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");

            Application.Current.Properties[ThemeKey] = themeUri;

            // Clear existing resources
            Application.Current.Resources.Clear();

            // Add the new theme resource dictionary
            Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri(themeUri, UriKind.Relative)
            });

            // Add other required resource dictionaries
            Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri("Resources/ScrollBar.xaml", UriKind.Relative)
            });
            Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri("Resources/TextBoxStyle.xaml", UriKind.Relative)
            });
            Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri("Resources/DatePickerStyle.xaml", UriKind.Relative)
            });
            Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri("Resources/ButtonStyle.xaml", UriKind.Relative)
            });
        }


        public void ReloadServices()
        {
            // Re-load database configuration from the updated config.json
            var config = AppConfiguration.LoadDatabaseConfiguration();
            bool useLocal = config.UseLocal == true;
            string? host = useLocal ? config.Local?.Host : $"{config.Server?.Host}:{config.Server?.Port}";
            string? apiKey = useLocal ? config.Local?.ApiKey : config.Server?.ApiKey;

            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(apiKey))
            {
                MessageBox.Show(
                    "Host or API key is missing or improperly configured. Please check your configuration file.",
                    "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Ensure the host has the correct URI format
            if (!host.StartsWith("http://") && !host.StartsWith("https://"))
            {
                host = "http://" + host;
            }

            // Create a new service collection
            var services = new ServiceCollection();

            // Register InfluxDBClient and IDatabaseService with the new configuration
            services.AddSingleton<InfluxDBClient>(sp => new InfluxDBClient(host, apiKey));
            services.AddSingleton<IDatabaseService>(sp =>
            {
                var influxClient = sp.GetRequiredService<InfluxDBClient>();
                string org = "thu-de";
                return new InfluxDatabaseService(
                    influxClient,
                    apiKey,
                    host,
                    org
                );
            });

            // Register other necessary services
            ConfigureServices(services);

            // Rebuild the service provider with updated services
            _serviceProvider = services.BuildServiceProvider();

            MessageBox.Show("Database configuration updated and services reloaded successfully.", "Success",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }


        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            //PythonScriptEngine.Instance.StopExecution();


            // Free the console
            FreeConsole();

            // Kill all locked processes
            KillLockedProcesses("SmartPacifier.UI (WPF)");

            // Dispose services
            if (_serviceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }

            // Release Mutexes
            _appMutex?.ReleaseMutex();
            _brokerMutex?.ReleaseMutex();
        }


        /// <summary>
        /// Kill all child processes started by the application
        /// </summary>
        private void KillLockedProcesses(string processName)
        {
            try
            {
                var processes = System.Diagnostics.Process.GetProcessesByName(processName);
                foreach (var process in processes)
                {
                    try
                    {
                        process.Kill();
                        process.WaitForExit();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to kill process {process.ProcessName}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while killing locked processes: {ex.Message}");
            }
        }


        /// <summary>
        /// Extension method to get the parent process ID
        /// </summary>
        private static int GetParentProcessId(System.Diagnostics.Process process)
        {
            try
            {
                using (var searcher = new System.Management.ManagementObjectSearcher(
                           "SELECT ParentProcessId FROM Win32_Process WHERE ProcessId = " + process.Id))
                {
                    foreach (var obj in searcher.Get())
                    {
                        return Convert.ToInt32(obj["ParentProcessId"]);
                    }
                }
            }
            catch
            {
                // If unable to retrieve the parent process ID, return 0
            }

            return 0;
        }
    }
}