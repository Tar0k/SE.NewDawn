using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.ModAPI.Ingame;


namespace IngameScript
{
    public class SafetySystem : BaseSystem, IDisposable
    {
        private readonly CoreSystem _coreSystem;
        private readonly ILogger _logger;
        private readonly List<IMyLargeTurretBase> _turrets;
        private readonly List<SafeDoor> _safeDoors = new List<SafeDoor>();
        private bool _firstRun = true;
        private bool _enemyDetected;
        public event Action<AlarmMessage> EnemyDetected;
        public event Action UpdateDoors;
        
        public SafetySystem(Program program, CoreSystem coreSystem, ILogger logger) : base(logger)
        {
            SystemName = "Система безопасности";
            _coreSystem = coreSystem;
            _coreSystem.UpdateSystems += Update;
            
            _logger = logger;
            var blocks = new List<IMyTerminalBlock>();
            program.GridTerminalSystem.GetBlocks(blocks);
            var doors = blocks.OfType<IMyDoor>()
                .Where(b => b.CustomData.Contains(RefCustomData)).ToList();

            foreach (var safeDoor in doors.Select(door => new SafeDoor(door, this, _logger)))
            {
                safeDoor.DoorStatusChanged += LogDoorStatusChanges;
                _safeDoors.Add(safeDoor);
            }
            
            _turrets = blocks.OfType<IMyLargeTurretBase>().ToList();
        }
        
        /// <summary>
        /// Проверяет есть ли у турелей цели.
        /// </summary>
        /// <returns>Найдена цель.</returns>
        private bool CheckTurrets() => _turrets.Any(turret => turret.IsAimed || turret.HasTarget);
        
        
        public override void Update()
        {
            if (_firstRun)
                CheckFirstRun();
            
            CheckAvailableTurrets();
            CheckAvailableSafeDoors();
            
            if (CheckTurrets())
            {
                if (!_enemyDetected)
                {
                    var alarmMessage = new AlarmMessage
                    {
                        AlarmCode = AlarmCodes.EnemyDetected,
                        Message = "Обнаружен противник",
                        System = this,
                        Type = MessageType.Error,
                        IsActive = true
                    };
                    EnemyDetected?.Invoke(alarmMessage);
                    _logger.WriteText(alarmMessage);
                    _enemyDetected = true;
                }
            }
            else
            {
                if (_enemyDetected)
                {
                    var alarmMessage = new AlarmMessage
                    {
                        AlarmCode = AlarmCodes.EnemyDetected,
                        Message = "Обнаружен противник",
                        System = this,
                        Type = MessageType.Error,
                        IsActive = false
                    };
                    EnemyDetected?.Invoke(alarmMessage);
                    _logger.WriteText(alarmMessage);
                
                    _enemyDetected = false;
                }
            }
            UpdateDoors?.Invoke();
        }
        
        private void CheckFirstRun()
        {
            if (_firstRun)
            {
                if (_turrets.Count > 0)
                {
                    _logger?.WriteText(new AlarmMessage
                    {
                        AlarmCode = AlarmCodes.StartupInfo,
                        Message = $"Инициализировано {_turrets.Count} турелей",
                        System = this,
                        Type = MessageType.Info,
                        IsActive = true
                    });
                }

                if (_safeDoors.Count > 0)
                {
                    _logger?.WriteText(new AlarmMessage
                    {
                        AlarmCode = AlarmCodes.StartupInfo,
                        Message = $"Инициализировано {_safeDoors.Count} безопасных дверей",
                        System = this,
                        Type = MessageType.Info,
                        IsActive = true
                    });
                }
            }  
            _firstRun = false;
        }

        private void CheckAvailableTurrets()
        {
            if (_turrets.Count <= 0)
            {
                _logger?.WriteText(new AlarmMessage
                {
                    AlarmCode = AlarmCodes.InitCount,
                    Message = "Не найдены турели",
                    System = this,
                    Type = MessageType.Warning,
                    IsActive = true
                });
            }
        }
        
        private void CheckAvailableSafeDoors()
        {
            if (_safeDoors.Count <= 0)
            {
                _logger?.WriteText(new AlarmMessage
                {
                    AlarmCode = AlarmCodes.InitCount,
                    Message = "Не найдены безопасные двери",
                    System = this,
                    Type = MessageType.Warning,
                    IsActive = true
                });
            }
        }

        private void LogDoorStatusChanges(IMyDoor door)
        {
            switch (door.Status)
            {
                case DoorStatus.Open:
                    _logger.WriteText(new AlarmMessage
                    {
                        AlarmCode = AlarmCodes.OnOffInfo,
                        Message = $"Открыта дверь {door.DisplayNameText}",
                        System = this,
                        Type = MessageType.Info,
                        IsActive = true
                    });
                    break;
                case DoorStatus.Closed:
                    _logger.WriteText(new AlarmMessage
                    {
                        AlarmCode = AlarmCodes.OnOffInfo,
                        Message = $"Закрыта дверь {door.DisplayNameText}",
                        System = this,
                        Type = MessageType.Info,
                        IsActive = true
                    });
                    break;
                case DoorStatus.Opening:
                case DoorStatus.Closing:
                default:
                    break;
            }
        }

        public void Dispose()
        {
            _coreSystem.UpdateSystems -= Update;
            foreach (var safeDoor in _safeDoors)
            {
                safeDoor.DoorStatusChanged -= LogDoorStatusChanges;
            }
        }
    }
}