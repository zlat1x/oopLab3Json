using System;

namespace Lab3JsonMaui.Models
{
    public class ParliamentEvent
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string FullName { get; set; } = string.Empty;

        public string Faculty { get; set; } = string.Empty;

        public string Department { get; set; } = string.Empty;

        public string Speciality { get; set; } = string.Empty;

        public string EventType { get; set; } = string.Empty;

        public string TimeFrame { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;
    }
}