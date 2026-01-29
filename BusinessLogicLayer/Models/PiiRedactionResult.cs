using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessLogicLayer.Models
{
    public class PiiRedactionResult
    {
        public string PreprocessedText { get; set; } = string.Empty;
        public string RedactedText { get; set; } = string.Empty;
        public bool SensitiveDataDetected { get; set; }
        public IReadOnlyList<DetectedPiiItem> DetectedTypes { get; set; } = [];
        public IReadOnlyList<DetectedPiiItem> IpDetectedItems { get; set; } = [];
    }
    public class DetectedPiiItem
    {
        public string Type { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

}
