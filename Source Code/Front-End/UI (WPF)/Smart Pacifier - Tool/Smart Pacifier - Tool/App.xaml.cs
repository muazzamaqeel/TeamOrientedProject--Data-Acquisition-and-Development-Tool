using Microsoft.Extensions.DependencyInjection;
using SmartPacifier.Interface.Services;
using SmartPacifier.BackEnd.Database.InfluxDB.Connection;
using SmartPacifier.BackEnd.DatabaseLayer.InfluxDB.Managers;
using Smart_Pacifier___Tool.Temp;
using InfluxDB.Client;
using System.Windows;
using SmartPacifier.BackEnd.Database.InfluxDB.Managers;
using Smart_Pacifier___Tool.Tabs.DeveloperTab; // Add the DeveloperTab namespace

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

            // Build the service provider from the service collection (This is the DI container)
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
            // Register InfluxDBClient as a singleton.
            services.AddSingleton(sp =>
                new InfluxDBClient("http://localhost:8086", "k-U_edQtQNhAFOwwjclwGCfh3seVBR6S64aKBPh46ZoDjW_ZI9DtWUAPa81IlMIKyMs8mjvMT58Tl33tCOm4hQ=="));

            // Register the InfluxDatabaseService as a singleton.
            services.AddSingleton<IDatabaseService, InfluxDatabaseService>();

            // Register campaign manager as a singleton.
            services.AddSingleton<IManagerCampaign, ManagerCampaign>();

            // Register pacifier manager as a singleton.
            services.AddSingleton<IManagerPacifiers, ManagerPacifiers>();

            // Register sensor manager as a singleton.
            services.AddSingleton<IManagerSensors, ManagerSensors>();

            // Register the Test window as a singleton.
            services.AddSingleton<Test>();

            // Register MainWindow or any other UI components you may need as singletons.
            services.AddSingleton<MainWindow>();

            // Register DeveloperView as a singleton
            services.AddSingleton<DeveloperView>();
        }
    }
}
