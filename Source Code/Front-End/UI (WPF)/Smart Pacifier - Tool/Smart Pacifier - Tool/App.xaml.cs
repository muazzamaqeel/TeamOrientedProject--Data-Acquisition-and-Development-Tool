using Microsoft.Extensions.DependencyInjection;
using SmartPacifier.Interface.Services;
using SmartPacifier.BackEnd.Database.InfluxDB.Connection;
using SmartPacifier.BackEnd.DatabaseLayer.InfluxDB.Connection;
using SmartPacifier.BackEnd.DatabaseLayer.InfluxDB.Managers;
using Smart_Pacifier___Tool.Temp;
using InfluxDB.Client;
using System;
using System.Windows;
using SmartPacifier.BackEnd.Database.InfluxDB.Managers;
using Smart_Pacifier___Tool.Tabs.DeveloperTab; // Add the DeveloperTab namespace
using Smart_Pacifier___Tool.Tabs.SettingsTab; // Add the SettingsTab namespace for SettingsView

namespace Smart_Pacifier___Tool
{
    public partial class App : Application
    {
        // The IServiceProvider instance that manages the lifetime of services and dependencies
        private IServiceProvider? _serviceProvider;

        // Expose the service provider publicly so other components can access it
        public IServiceProvider ServiceProvider => _serviceProvider!;

        /// <summary>
        /// The entry point of the application.
        /// This method is executed when the application starts.
        /// We use the Microsoft.Extensions.DependencyInjection library to set up Dependency Injection (DI).
        /// </summary>
        /// <param name="e">Startup event arguments.</param>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Create a new service collection that will hold our service registrations
            var services = new ServiceCollection();

            // Call the method that registers all necessary services
            ConfigureServices(services);

            // Build the service provider from the service collection (This is the Dependency Injection container)
            _serviceProvider = services.BuildServiceProvider();

            // Use the service provider to resolve (get) the Test window and show it
            //var testWindow = _serviceProvider.GetRequiredService<Test>();
            //testWindow.Show();
        }

        /// <summary>
        /// Configures and registers all the services, managers, and other dependencies.
        /// This is where we register the InfluxDB client, services, managers, and UI components.
        /// </summary>
        /// <param name="services">The service collection where services are registered.</param>
        private void ConfigureServices(IServiceCollection services)
        {
            // Register ILocalHost with its implementation
            services.AddSingleton<ILocalHost, LocalHostSetup>();

            // Register InfluxDBClient with the URL and token from ILocalHost
            services.AddSingleton<InfluxDBClient>(sp =>
            {
                var localHostService = sp.GetRequiredService<ILocalHost>();
                string apiKey = localHostService.GetApiKey();

                return new InfluxDBClient("http://localhost:8086", apiKey);
            });

            // Register InfluxDatabaseService as IDatabaseService
            services.AddSingleton<IDatabaseService>(sp =>
            {
                var influxClient = sp.GetRequiredService<InfluxDBClient>();
                var localHostService = sp.GetRequiredService<ILocalHost>();
                string apiKey = localHostService.GetApiKey();

                return new InfluxDatabaseService(
                    influxClient,
                    apiKey, // API key retrieved through ILocalHost
                    "http://localhost:8086", // baseUrl
                    "thu-de" // org
                );
            });

            // Register the Manager classes, injecting IDatabaseService where necessary
            services.AddSingleton<IManagerCampaign, ManagerCampaign>();
            services.AddSingleton<IManagerPacifiers, ManagerPacifiers>();
            services.AddSingleton<IManagerSensors, ManagerSensors>();

            // Register UI components
            services.AddSingleton<MainWindow>();
            services.AddSingleton<DeveloperView>();
            services.AddTransient<SettingsView>();
        }
    }
}
