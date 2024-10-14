﻿using System;
using System.Collections.Generic;
using System.Windows;
using Interface_NETInterop;  // Reference to interface

namespace Smart_Pacifier___Tool.Temp
{
    public partial class Test : Window
    {
        private readonly IDatabaseService _databaseService;

        public Test(IServiceFactory serviceFactory)
        {
            InitializeComponent();

            // Use the factory to create the database service
            _databaseService = serviceFactory.CreateDatabaseService();
        }

        private void OnWriteDataButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var fields = new Dictionary<string, object>
                {
                    { "temperature", 37.2 },
                    { "humidity", 55 }
                };

                var tags = new Dictionary<string, string>
                {
                    { "sensor", "pacifier1" }
                };

                _databaseService.WriteData("environment", fields, tags);
                MessageBox.Show("Data written to SmartPacifier-Bucket1.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error writing data: {ex.Message}");
            }
        }

        private void OnReadDataButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                string fluxQuery = @"
                    from(bucket: ""SmartPacifier-Bucket1"")
                    |> range(start: -1h)
                    |> filter(fn: (r) => r._measurement == ""environment"")
                ";

                var results = _databaseService.ReadData(fluxQuery);
                ResultsTextBox.Text = string.Join(Environment.NewLine, results);
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show($"Error reading data: {ex.Message}");
            }
        }
    }
}
