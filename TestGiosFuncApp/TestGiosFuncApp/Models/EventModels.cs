using System;
using System.Collections.Generic;
using System.Text;

namespace TestGiosFuncApp.Models
{
    public class GiosDataEvent
    {
        public string StorageFileName { get; set; }

        public string PlotName { get; set; }
    }

    public class CustomEvent
    {
        /// <summary>
        /// Gets the unique identifier for the event.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets the publisher defined path to the event subject.
        /// </summary>
        public string Subject { get; set; }

        /// <summary>
        /// Gets the registered event type for this event source.
        /// </summary>
        public string EventType { get; }

        /// <summary>
        /// Gets the time the event is generated based on the provider's UTC time.
        /// </summary>
        public string EventTime { get; }

        public GiosDataEvent Data { get; set; }
        /// <summary>
        /// Constructor.
        /// </summary>
        public CustomEvent()
        {
            Id = Guid.NewGuid().ToString();
            EventType = "GiosDataEvent";
            EventTime = DateTime.UtcNow.ToString("o");
        }
    }
}
