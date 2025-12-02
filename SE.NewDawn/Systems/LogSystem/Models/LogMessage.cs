using System;
using System.Text;

namespace IngameScript
{
    public class LogMessage
    {
        public AlarmCodes AlarmCode { get; set; }
        public string System { get; set; }
        public string Message { get; set; }
        public DateTime? OccurrenceTime { get; set; }
        public DateTime?  EndTime { get; set; }
        
        public override string ToString()
        {
            var str = new StringBuilder();
            str.Append(System);
            str.Append(' ');
            str.Append(Message);
            if (OccurrenceTime != null)
            {
                str.Append(' ');
                str.Append(OccurrenceTime.Value.ToString("HH:mm:ss"));
            }

            if (EndTime == null) return str.ToString();
            str.Append(' ');
            str.Append(EndTime.Value.ToString("HH:mm:ss"));
            return str.ToString();
        }
    }
}