using Microsoft.Extensions.DependencyInjection;
using SmartPacifier.Interface.Services;
using SmartPacifier.BackEnd.Database.InfluxDB.Connection;
using SmartPacifier.BackEnd.DatabaseLayer.InfluxDB.Managers;
using Smart_Pacifier___Tool.Temp;
using InfluxDB.Client;
using System.Windows;
using SmartPacifier.BackEnd.Database.InfluxDB.Managers;

namespace Smart_Pacifier___Tool
{
    public partial class App : Application
    {
        private IServiceProvider? _serviceProvider;

        /// <summary>
        /// The entry point of the application.
        /// We are using the Microsoft.Extensions.DependencyInjection library to manage the dependency injection.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            var services = new ServiceCollection();
            ConfigureServices(services);

            _serviceProvider = services.BuildServiceProvider();
            var testWindow = _serviceProvider.GetRequiredService<Test>();
            testWindow.Show();
        }

        /// <summary>
        /// The method that registers all the services and managers that are needed for the application.
        /// </summary>
        /// <param name="services"></param>
        private void ConfigureServices(IServiceCollection services)
        {
            // Register InfluxDBClient as a singleton to reuse the same instance across the app
            services.AddSingleton(sp =>
                new InfluxDBClient("http://localhost:8086", "k-U_edQtQNhAFOwwjclwGCfh3seVBR6S64aKBPh46ZoDjW_ZI9DtWUAPa81IlMIKyMs8mjvMT58Tl33tCOm4hQ=="));

            // Register InfluxDatabaseService and inject InfluxDBClient
            services.AddSingleton<IDatabaseService, InfluxDatabaseService>();

            // Register the managers
            services.AddSingleton<IManagerCampaign, ManagerCampaign>();
            services.AddSingleton<IManagerPacifiers, ManagerPacifiers>();
            services.AddSingleton<IManagerSensors, ManagerSensors>();

            // Register the Test window
            services.AddSingleton<Test>();

            // Register MainWindow or other UI components if needed
            services.AddSingleton<MainWindow>();
        }
    }
}
