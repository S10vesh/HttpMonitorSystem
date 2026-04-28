using System;

namespace HttpMonitorSystem.Models
{
    public class StoredMessage
    {
        public int Id { get; set; }
        public string Message { get; set; }
        public DateTime ReceivedAt { get; set; }
    }
}