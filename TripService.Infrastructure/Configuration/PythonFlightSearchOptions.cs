using System;
using System.Collections.Generic;
using System.Text;

namespace TripService.Infrastructure.Configuration
{
    public sealed class PythonFlightSearchOptions
    {
        public const string SectionName = "PythonFlightSearch";

        public string BaseUrl { get; set; } = string.Empty;

        public string ApiKey { get; set; } = string.Empty;

        public int TimeoutSeconds { get; set; } = 30;
    }
}
