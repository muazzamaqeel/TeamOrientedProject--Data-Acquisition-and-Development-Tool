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

namespace Smart_Pacifier___Tool
{
    public partial class App : Application
    {
        // The IServiceProvider instance that manages the lifetime of services and dependencies
        private IServiceProvider? _serviceProvider;

        // Mutex instances to prevent multiple instances of the application and the broker
        private static Mutex? appMutex;
        private static Mutex? brokerMutex;

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
            appMutex = new Mutex(true, "SmartPacifierMainAppMutex", out isAppNewInstance);
            if (!isAppNewInstance)
            {
                MessageBox.Show("The application is already running.");
                Shutdown();
                return;
            }

            // Ensure only one instance of the MQTT client
            bool isBrokerNewInstance;
            brokerMutex = new Mutex(true, "SmartPacifierBrokerMutex", out isBrokerNewInstance);

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

            // Build the service provider from the service collection (This is the Dependency Injection container)
            _serviceProvider = services.BuildServiceProvider();

            // Run BrokerMain asynchronously to avoid blocking the main UI thread
            var brokerMain = _serviceProvider.GetRequiredService<IBrokerMain>();
            await brokerMain.StartAsync(Array.Empty<string>()); // Await the task directly



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
            var config = LoadDatabaseConfiguration();

            // Determine which configuration to use
            bool useLocal = config.UseLocal == true;

            // Set up the appropriate host and API key based on the configuration
            string? host = useLocal ? config.Local?.Host : $"{config.Server?.Host}:{config.Server?.Port}";
            string? apiKey = useLocal ? config.Local?.ApiKey : config.Server?.ApiKey;

            // Check if Host or ApiKey is missing and throw an exception with a detailed message
            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(apiKey))
            {
                MessageBox.Show("Host or API key is missing or improperly configured. Please check your configuration file.", "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    host,   // baseUrl from configuration
                    org     // organization name
                );
            });

            //Register PythonScriptEngine as IAlgorithmLayer
            // Correct registration using the class constructor
            //services.AddSingleton<IAlgorithmLayer, PythonScriptEngine>();


            // Register other necessary services
            services.AddSingleton<ILocalHost, LocalHostSetup>();
            services.AddSingleton<IManagerPacifiers, ManagerPacifiers>();
            services.AddSingleton<IManagerCampaign, ManagerCampaign>();
            services.AddSingleton<IManagerSensors, ManagerSensors>();
            services.AddSingleton<MainWindow>();
            services.AddSingleton<DeveloperView>();
            services.AddTransient<AlgorithmView>();
            services.AddSingleton<IBrokerMain, BrokerMain>();

            // UI component registration
            services.AddTransient<PacifierSelectionView>();
            services.AddTransient<Func<string, SettingsView>>(sp => (defaultView) =>
            {
                var localHostService = sp.GetRequiredService<ILocalHost>();
                return new SettingsView(localHostService, defaultView);
            });
            services.AddTransient<CampaignsView>();
        }


        /// <summary>
        /// Loads the database configuration from the config.json file.
        /// This method deserializes the JSON file to populate the AppConfiguration object.
        /// </summary>
        /// <returns>Returns the AppConfiguration object with loaded settings.</returns>
        public AppConfiguration LoadDatabaseConfiguration()
        {
            // Navigate up from the bin directory to the project root
            string ?projectDirectory = AppDomain.CurrentDomain.BaseDirectory;
            for (int i = 0; i < 4; i++)  // Adjust as needed to reach the project root
            {
                projectDirectory = Directory.GetParent(projectDirectory)?.FullName;
            }

            // Define the path to the original config.json in the project structure
            string configPath = Path.Combine(projectDirectory, "Resources", "OutputResources", "config.json");

            if (!File.Exists(configPath))
            {
                MessageBox.Show($"Configuration file not found at: {configPath}", "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                throw new FileNotFoundException("Configuration file not found.", configPath);
            }

            try
            {
                var configJson = File.ReadAllText(configPath);
                var config = JsonConvert.DeserializeObject<AppConfiguration>(configJson);

                if (config == null)
                {
                    MessageBox.Show("Failed to parse configuration file.", "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    throw new InvalidOperationException("Failed to parse configuration file.");
                }

                return config;
            }
            catch (Newtonsoft.Json.JsonException ex)
            {
                MessageBox.Show($"Error parsing configuration file: {ex.Message}", "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An unexpected error occurred: {ex.Message}", "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
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
            var config = LoadDatabaseConfiguration();
            bool useLocal = config.UseLocal == true;
            string? host = useLocal ? config.Local?.Host : $"{config.Server?.Host}:{config.Server?.Port}";
            string? apiKey = useLocal ? config.Local?.ApiKey : config.Server?.ApiKey;

            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(apiKey))
            {
                MessageBox.Show("Host or API key is missing or improperly configured. Please check your configuration file.", "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

            MessageBox.Show("Database configuration updated and services reloaded successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }




        /// <summary>
        /// Cleanup when the application exits
        /// </summary>
        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            // Free the console when the application exits
            FreeConsole();
        }
    }
}
