using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Text.Json;
using Newtonsoft.Json;
using System.Reflection;


namespace Smart_Pacifier___Tool
{ 
    public class AppConfiguration
    {
        public bool? UseLocal { get; set; }
        public LocalConfig? Local { get; set; }
        public ServerConfig? Server { get; set; }

        private AppConfiguration LoadDatabaseConfiguration()
        {
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "OutputResources", "config.json");

            if (!File.Exists(configPath))
            {
                MessageBox.Show($"Configuration file not found at: {configPath}", "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                throw new FileNotFoundException("Configuration file not found.", configPath);
            }

            try
            {
                string configJson = File.ReadAllText(configPath);
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
        }

        




    }

    public class LocalConfig
    {
        public string? Host { get; set; }
        public string? ApiKey { get; set; }
    }

    public class ServerConfig
    {
        public string? Host { get; set; }
        public string? Port { get; set; }
        public string? Username { get; set; }
        public string? ApiKey { get; set; }
    }

}
