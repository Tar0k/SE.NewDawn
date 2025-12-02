namespace IngameScript
{
    public class AlarmMessage : SystemAlarm
    {
        public AlarmMessage() {}
        
        public AlarmMessage(SystemAlarm alarm, bool isActive)
        {
            AlarmCode = alarm.AlarmCode;
            Message = alarm.Message;
            System = alarm.System;
            Type = alarm.Type;
            IsActive = isActive;
        }
        
        public bool IsActive { get; set; }
    }
}