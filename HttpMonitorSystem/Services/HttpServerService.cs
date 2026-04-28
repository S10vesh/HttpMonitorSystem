using HttpMonitorSystem.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HttpMonitorSystem.Services
{
    public class HttpServerService
    {
        private HttpListener _listener;
        private readonly ConcurrentBag<RequestLog> _logs = new();
        private readonly List<StoredMessage> _messages = new();
        private int _requestCounter = 0;
        private DateTime _startTime;
        private bool _isRunning = false;

        public event Action<RequestLog> OnRequestLogged;

        public void Start(int port)
        {
            if (_isRunning) return;

            _startTime = DateTime.Now;
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://+:{port}/");
            _listener.Start();
            _isRunning = true;

            Task.Run(Listen);
        }

        private async Task Listen()
        {
            while (_isRunning && _listener.IsListening)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    _ = Task.Run(() => ProcessRequest(context));
                }
                catch (HttpListenerException)
                {
                    // Сервер остановлен
                    break;
                }
            }
        }

        private async Task ProcessRequest(HttpListenerContext context)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var log = new RequestLog
            {
                Timestamp = DateTime.Now,
                Method = context.Request.HttpMethod,
                Url = context.Request.Url.ToString(),
                Headers = string.Join("; ", context.Request.Headers.AllKeys.Select(k => $"{k}:{context.Request.Headers[k]}"))
            };

            string body = null;
            if (context.Request.HasEntityBody)
            {
                using var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding);
                body = await reader.ReadToEndAsync();
                log.Body = body;
            }

            int statusCode = 200;
            string responseString = "";

            try
            {
                if (context.Request.HttpMethod == "GET" && context.Request.Url.AbsolutePath == "/")
                {
                    var uptime = DateTime.Now - _startTime;
                    var stats = new
                    {
                        uptime_seconds = uptime.TotalSeconds,
                        total_requests = _requestCounter,
                        messages_count = _messages.Count,
                        get_count = GetGetCount(),
                        post_count = GetPostCount()
                    };
                    responseString = JsonSerializer.Serialize(stats);
                }
                else if (context.Request.HttpMethod == "POST" && context.Request.Url.AbsolutePath == "/message")
                {
                    var json = JsonDocument.Parse(body);
                    var msgText = json.RootElement.GetProperty("message").GetString();
                    var newMsg = new StoredMessage
                    {
                        Id = _messages.Count + 1,
                        Message = msgText,
                        ReceivedAt = DateTime.Now
                    };
                    _messages.Add(newMsg);
                    responseString = JsonSerializer.Serialize(new { id = newMsg.Id });
                }
                else
                {
                    statusCode = 404;
                    responseString = "{\"error\":\"Not found\"}";
                }
            }
            catch (Exception ex)
            {
                statusCode = 400;
                responseString = $"{{\"error\":\"Bad request: {ex.Message}\"}}";
            }

            sw.Stop();
            log.StatusCode = statusCode;
            log.ProcessingTimeMs = sw.Elapsed.TotalMilliseconds;

            _logs.Add(log);
            Interlocked.Increment(ref _requestCounter);
            OnRequestLogged?.Invoke(log);

            context.Response.StatusCode = statusCode;
            var buffer = Encoding.UTF8.GetBytes(responseString);
            context.Response.ContentType = "application/json";
            await context.Response.OutputStream.WriteAsync(buffer);
            context.Response.Close();
        }

        public void Stop()
        {
            _isRunning = false;
            _listener?.Stop();
            _listener?.Close();
        }

        public List<RequestLog> GetLogs() => _logs.ToList();
        public int GetRequestCount() => _requestCounter;
        public int GetGetCount() => _logs.Count(l => l.Method == "GET");
        public int GetPostCount() => _logs.Count(l => l.Method == "POST");
        public double GetAvgProcessingTime() => _logs.Any() ? _logs.Average(l => l.ProcessingTimeMs) : 0;
    }
}