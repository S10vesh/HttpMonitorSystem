using System;
using System.IO;
using System.Windows;
using HttpMonitorSystem.Services;
using HttpMonitorSystem.Models;

namespace HttpMonitorSystem
{
    public partial class MainWindow : Window
    {
        private HttpServerService? _server;
        private readonly HttpClientService _client;

        public MainWindow()
        {
            InitializeComponent();
            _client = new HttpClientService();
        }

        private void StartServer_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(PortBox.Text, out int port))
            {
                MessageBox.Show("Введите корректный номер порта", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                _server = new HttpServerService();
                _server.OnRequestLogged += OnNewLog;
                _server.Start(port);

                StartServerBtn.IsEnabled = false;
                StopServerBtn.IsEnabled = true;

                MessageBox.Show($"Сервер запущен на порту {port}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка запуска сервера: {ex.Message}\n\nВозможно, нужно запустить Visual Studio от имени администратора", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnNewLog(RequestLog log)
        {
            Dispatcher.Invoke(() =>
            {
                LogsList.Items.Add(new
                {
                    log.Timestamp,
                    log.Method,
                    log.Url,
                    log.StatusCode,
                    log.ProcessingTimeMs
                });

                if (LogsList.Items.Count > 0)
                    LogsList.ScrollIntoView(LogsList.Items[LogsList.Items.Count - 1]);

                UpdateStats();
            });
        }

        private void UpdateStats()
        {
            if (_server == null) return;

            StatsText.Text = $"GET: {_server.GetGetCount()} | POST: {_server.GetPostCount()}\n" +
                            $"Среднее время: {_server.GetAvgProcessingTime():F2} мс\n" +
                            $"Всего: {_server.GetRequestCount()}";
        }

        private void StopServer_Click(object sender, RoutedEventArgs e)
        {
            _server?.Stop();
            StartServerBtn.IsEnabled = true;
            StopServerBtn.IsEnabled = false;

            MessageBox.Show("Сервер остановлен", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void SendRequest_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ClientUrl.Text))
            {
                MessageBox.Show("Введите URL", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SendRequestBtn.IsEnabled = false;
            ResponseBox.Text = "Отправка запроса...";

            string url = ClientUrl.Text;
            string method = GetMethod.IsChecked == true ? "GET" : "POST";
            string? body = PostMethod.IsChecked == true ? JsonBody.Text : null;

            string response = await _client.SendRequestAsync(url, method, body);
            ResponseBox.Text = response;

            SendRequestBtn.IsEnabled = true;
        }

        private void SaveLogs_Click(object sender, RoutedEventArgs e)
        {
            if (_server == null || _server.GetLogs().Count == 0)
            {
                MessageBox.Show("Нет логов для сохранения", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var logs = _server.GetLogs();
                string fileName = $"logs_{DateTime.Now:yyyyMMdd_HHmmss}.txt";

                using (var writer = new StreamWriter(fileName))
                {
                    writer.WriteLine("=== HTTP MONITOR SYSTEM LOGS ===");
                    writer.WriteLine($"Дата экспорта: {DateTime.Now}");
                    writer.WriteLine(new string('-', 80));

                    foreach (var log in logs)
                    {
                        writer.WriteLine($"{log.Timestamp:yyyy-MM-dd HH:mm:ss} | {log.Method} | {log.Url} | Статус: {log.StatusCode} | {log.ProcessingTimeMs:F2}ms");
                    }
                }

                MessageBox.Show($"Логи сохранены в файл: {fileName}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}