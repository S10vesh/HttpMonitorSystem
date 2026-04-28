using System;

namespace HttpMonitorSystem.Models
{
    public class RequestLog
    {
        public DateTime Timestamp { get; set; }
        public string Method { get; set; }
        public string Url { get; set; }
        public string Headers { get; set; }
        public string Body { get; set; }
        public int StatusCode { get; set; }
        public double ProcessingTimeMs { get; set; }
    }
}