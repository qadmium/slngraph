using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace slngraph
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string YumlBaseUri = "http://yuml.me/diagram/nofunky;dir:LR;scale:80/class/";

        public MainWindow()
        {
            InitializeComponent();
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog {DefaultExt = ".sln", Filter = "Solution files (.sln)|*.sln"};

            if (dlg.ShowDialog(this) != true)
            {
                return;
            }

            try
            {
                CurrentSolution = new Solution(dlg.FileName);
            }
            catch (Exception exception)
            {
                StatusLabel.Content = exception.Message;
                MessageBox.Show(this, exception.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            ProjectsBox.ItemsSource = CurrentSolution.Projects;
        }
         
        public Solution CurrentSolution { get; set; }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                IsEnabled = false;

                if (CurrentSolution == null)
                {
                    throw new ArgumentException("Choose solution first!");
                }

                var client = new HttpClient();
                var outputPath = Path.Combine(Path.GetDirectoryName(CurrentSolution.SolutionPath), CurrentSolution.Name + ".png");

                using (var outputStream = File.Create(outputPath))
                {
                    StatusLabel.Content = "Downloading result...";

                    var requestParams = new Dictionary<string, string>
                        {
                            { "dsl_text", CurrentSolution.ToYuml(ImplicitCheck.IsChecked == true) }
                        };
                    var request = new FormUrlEncodedContent(requestParams);

                    var imageNameResponse = await client.PostAsync(YumlBaseUri, request);
                    imageNameResponse.EnsureSuccessStatusCode();

                    var imageRequest = YumlBaseUri + await imageNameResponse.Content.ReadAsStringAsync();
                    var response = await client.GetAsync(imageRequest);
                    response.EnsureSuccessStatusCode();

                    await response.Content.CopyToAsync(outputStream);
                }

                Process.Start(outputPath);
                
                StatusLabel.Content = "";
            }
            catch (Exception exception)
            {
                StatusLabel.Content = exception.Message;
                MessageBox.Show(this, exception.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsEnabled = true;
            }
        }

        private void SelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentSolution == null)
            {
                return;
            }

            var hasSelection = CurrentSolution.Projects.Any(project => project.UseInGraphBuilding);

            foreach (var project in CurrentSolution.Projects)
            {
                project.UseInGraphBuilding = !hasSelection;
            }

            ProjectsBox.ItemsSource = null;
            ProjectsBox.ItemsSource = CurrentSolution.Projects;
        }
    }
}
