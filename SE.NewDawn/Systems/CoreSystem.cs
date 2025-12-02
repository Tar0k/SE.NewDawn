using System;
using System.Collections.Generic;
using System.Linq;

namespace IngameScript
{
    public class CoreSystem : BaseSystem, IDisposable
    {
        public event Action UpdateSystems;
        public event Action<AlarmMessage> AlarmTriggered;

        private readonly LogSystem _logSystem;
        private readonly LightSystem _lightSystem;
        private readonly SoundSystem _soundSystem;
        private readonly SafetySystem _safetySystem;
        private readonly IEnumerable<BaseSystem> _systems;

        public CoreSystem(Program program)
        {
            SystemName = "Центральная система";
            
            _logSystem = new LogSystem(program, this);
            _logSystem.SystemAlarmTriggered += OnSystemAlarmTriggered;
            Logger = _logSystem;
            
            _lightSystem = new LightSystem(program, this, _logSystem);
            _soundSystem = new SoundSystem(program, this, _logSystem);
            _safetySystem = new SafetySystem(program, this, _logSystem);
            _safetySystem.EnemyDetected += OnSystemAlarmTriggered;
            
            _systems = new List<BaseSystem>
            {
                _logSystem, _lightSystem,  _soundSystem, _safetySystem
            };
        }
        
        public override SystemStates SystemState {
            get
            {
                if (_systems.Any(s => s.SystemState == SystemStates.Alarm))
                    return SystemStates.Alarm;
                if (_systems.Any(s => s.SystemState == SystemStates.Warning))
                    return SystemStates.Warning;
                if (_systems.All(s => s.SystemState == SystemStates.Active))
                    return SystemStates.Active;
                return SystemStates.Unknown;

            }
            protected set { }
        }

        public override void Update()
        {
            UpdateSystems?.Invoke();
        }

        public override bool ExecuteCommand(string command)
        {
            // Проверки полученной команды на формат
            var cmd = command.Split(' ');
            
            if (!_systems.Select(s => s.RefCustomData).Contains(cmd[0]))
            {
                Logger?.WriteText(new AlarmMessage
                {
                    AlarmCode = AlarmCodes.CommandInfo,
                    Message = $"Получена команда для неизвестной системы: \"{command}\"",
                    System = this,
                    Type = MessageType.Warning,
                    IsActive = true
                });
                return false;
            }
            
            // Вызываем выполнение команды для конкретной системы
            var system = _systems.First(s => s.RefCustomData == cmd[0]);
            var result = system.ExecuteCommand(command);
            return result;
        }

        private void OnSystemAlarmTriggered(AlarmMessage message)
        {
            if (message.Type == MessageType.Error && message.IsActive)
            {
                AlarmTriggered?.Invoke(message);
            }
        }

        public void Dispose()
        {
            _logSystem.SystemAlarmTriggered -= OnSystemAlarmTriggered;
            _safetySystem.EnemyDetected -= OnSystemAlarmTriggered;
        }
    }
}