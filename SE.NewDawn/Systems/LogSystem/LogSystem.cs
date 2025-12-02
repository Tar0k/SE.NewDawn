using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.ModAPI.Ingame;
using VRage.Game.GUI.TextPanel;

namespace IngameScript
{
    public sealed class LogSystem : BaseSystem, ILogger, IDisposable
    {
        private readonly CoreSystem _coreSystem;
        
        private readonly List<IMyTextPanel> _logPanels;
        private readonly Queue<LogMessage> _logMessages = new Queue<LogMessage>();
        private readonly int _maxMessages;
        private readonly List<SystemAlarm> _alarms = new List<SystemAlarm>();
        private bool _firstRun = true;
        
        public event Action<AlarmMessage> SystemAlarmTriggered;
        
        
        public LogSystem(Program program, CoreSystem core, int maxMessages = 20)
        {
            SystemName = "Журнал сообщений";
            _maxMessages = maxMessages;
            var panels = new List<IMyTextPanel>();
            program.GridTerminalSystem.GetBlocksOfType(panels);
            _logPanels = panels.Where(p => p.CustomData.Contains(RefCustomData)).ToList();
            ConfigurePanels(_logPanels);
            _coreSystem = core;
            _coreSystem.UpdateSystems += Update;
        }

        public bool WriteText(AlarmMessage message)
        {
            return SystemState == SystemStates.Active && WriteText(message.AlarmCode, message.Message, message.System, message.IsActive);
        }

        public bool WriteText(AlarmCodes alarmCode, string text,  BaseSystem system, bool? isActive)
        {
            if (SystemState != SystemStates.Active) return false;
            if (isActive.HasValue)
            {
                if (isActive == true)
                {
                    _logMessages.Enqueue(new LogMessage
                    {
                        AlarmCode = alarmCode,
                        Message = text,
                        System = system.ToString(),
                        OccurrenceTime = DateTime.Now,
                        EndTime = null
                    });
                }
                else
                {
                    var loggedMessage =
                        _logMessages.FirstOrDefault(m => m.Message == text && m.System == system.ToString());
                    if (loggedMessage != null) loggedMessage.EndTime = DateTime.Now;
                }
            }
            else
            {
                _logMessages.Enqueue(new LogMessage
                {
                    Message = text,
                    System = system.ToString(),
                    OccurrenceTime = DateTime.Now,
                });
            }
            
            if (_logMessages.Count > _maxMessages) _logMessages.Dequeue();
            return true;
        }
        
        public override void Update()
        {
            if (SystemState != SystemStates.Active) return;
            
            CheckSystemState();
            foreach (var textPanel in _logPanels)
            {
                var str = new StringBuilder();
                str.AppendLine("ЖУРНАЛ СООБЩЕНИЙ");
                str.AppendLine("----------------");
                str.AppendLine(string.Join(Environment.NewLine, _logMessages.Reverse()));
                textPanel.WriteText(str.ToString());
            }
        }

        // Проверяет состояние системы и информирует подписчиков об изменении
        private void CheckSystemState()
        {
            // Сохраняем тревоги с предыдущего цикла (до обработки)
            var previousAlarms = new List<SystemAlarm>(_alarms);
            
            // Проверка при первом запуске
            if (_firstRun) CheckFirstRun();
            // Проверка и обновление списка текущих тревог
            CheckAvailablePanels();
        }

        private void CheckFirstRun()
        {
            if (_firstRun && _logPanels.Count > 0)
            {
                SystemAlarmTriggered?.Invoke(new AlarmMessage
                {
                    AlarmCode = AlarmCodes.StartupInfo,
                    Message = $"Инициализировано {_logPanels.Count} панелей",
                    System = this,
                    Type = MessageType.Warning,
                    IsActive = true
                });
            }
            _firstRun = false;
        }

        private void CheckAvailablePanels()
        {
            if (_logPanels.Count <= 0 && !_alarms.Select(a => a.AlarmCode).Contains(AlarmCodes.InitCount))
                _alarms.Add(new SystemAlarm
                {
                    Message = $"Не найдено панелей с системой \"{RefCustomData}\" в CustomData",
                    AlarmCode = AlarmCodes.InitCount,
                    System = this,
                    Type = MessageType.Warning
                });
            else if (_alarms.Select(a => a.AlarmCode).Contains(AlarmCodes.InitCount))
                _alarms.RemoveAll(a => a.AlarmCode == AlarmCodes.InitCount);
        }
        
        // Настройка параметров панелей
        private static void ConfigurePanels(List<IMyTextPanel> logPanels)
        {
            foreach (var logPanel in logPanels)
            {
                logPanel.ContentType = ContentType.TEXT_AND_IMAGE;
                logPanel.FontSize = 0.6f;
                logPanel.TextPadding = 3;
            }
        }

        public void Dispose()
        {
            _coreSystem.UpdateSystems -= Update;
        }
    }
}