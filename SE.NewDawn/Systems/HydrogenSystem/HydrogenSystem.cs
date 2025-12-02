using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.ModAPI.Ingame;

namespace IngameScript
{
    public class HydrogenSystem : BaseSystem
    {
        private readonly CoreSystem _coreSystem;
        private readonly ILogger _logger;
        
        private readonly List<IMyGasTank> _hydrogenTanks = new List<IMyGasTank>();
        private bool _firstRun = true;
        private double _warningLevel = 0.2;
        private double _alarmLevel = 0.1;
        private readonly List<IMyTextPanel> _controlPanels;

        public event Action<decimal> WarningLevelTriggered;
        public event Action<decimal> AlarmLevelTriggered;

        public HydrogenSystem(Program program, CoreSystem core, ILogger logger, double warningLevel = 0.2, double alarmLevel = 0.1) : base(logger)
        {
            SystemName = "Система контроля водорода";
            program.GridTerminalSystem.GetBlocksOfType(_hydrogenTanks);
            _coreSystem = core;
            _coreSystem.UpdateSystems += Update;
            _logger = logger;
            
            var panels = new List<IMyTextPanel>();
            program.GridTerminalSystem.GetBlocksOfType(panels);
            _controlPanels = panels.Where(p => p.CustomData.Contains(RefCustomData)).ToList();
            
            WarningLevel = warningLevel;
            AlarmLevel = alarmLevel;
            
            CheckFirstRun();
            CheckAvailableHydrogenTanks();
        }
        
        public override void Update()
        {
            switch (SystemState)
            {
                case SystemStates.Active:
                    if (Level < _alarmLevel)
                    {
                        AlarmLevelTriggered?.Invoke(CurrentCapacity);
                        SystemState = SystemStates.Alarm;
                    }
                    if (Level < _warningLevel)
                    {
                        WarningLevelTriggered?.Invoke(CurrentCapacity);
                        SystemState = SystemStates.Warning;
                    }
                    break;
                case SystemStates.Warning:
                    if (Level < _alarmLevel)
                    {
                        AlarmLevelTriggered?.Invoke(CurrentCapacity);
                        SystemState = SystemStates.Alarm;
                    }
                    if (Level > _warningLevel)
                    {
                        SystemState = SystemStates.Active;
                    }
                    break;
                case SystemStates.Alarm:
                    if (Level > _alarmLevel)
                    {
                        if (Level < _warningLevel)
                        {
                            WarningLevelTriggered?.Invoke(CurrentCapacity);
                            SystemState = SystemStates.Warning;
                        }
                        else
                        {
                            SystemState = SystemStates.Active;
                        }
                    }
                    break;
                case SystemStates.Inactive:
                case SystemStates.Unknown:
                default:
                    break;
            }
            UpdatePanels();
        }

        /// <summary>
        /// Максимальная емкость, в литрах
        /// </summary>
        public decimal MaxCapacity => (decimal)_hydrogenTanks.Select(t => t.Capacity).Sum();
        
        /// <summary>
        /// Фактический водород, в литрах
        /// </summary>
        public decimal CurrentCapacity => (decimal)_hydrogenTanks.Select(CalcTankCurrentCapacity).Sum();

        /// <summary>
        /// Текущая заполненность, в процентах
        /// </summary>
        public double Level
        {
            get
            {
                if (MaxCapacity == 0)
                    return 0;
                return (double)(CurrentCapacity / MaxCapacity);
            }
        }

        /// <summary>
        /// Уставка на предупреждение по уровню
        /// </summary>
        public double WarningLevel
        {
            get
            {
                return _warningLevel;
            } 
            set
            {
                if (value < 0 || value > 1)
                {
                    _logger?.WriteText(new AlarmMessage
                    {
                        AlarmCode = AlarmCodes.CommandInfo,
                        Message = "Заданный уровень предупреждения вне диапазона от 0 до 1",
                        System = this,
                        Type = MessageType.Warning,
                        IsActive = false
                    });
                    return;
                }

                if (value <= _alarmLevel)
                {
                    _logger?.WriteText(new AlarmMessage
                    {
                        AlarmCode = AlarmCodes.CommandInfo,
                        Message = "Уровень предупреждения не может быть ниже аварийного значения уровня",
                        System = this,
                        Type = MessageType.Warning,
                        IsActive = false
                    });
                    return;
                }
                _warningLevel = value;
            }
        }

        /// <summary>
        /// Уставка на тревогу по уровню
        /// </summary>
        public double AlarmLevel
        {
            get 
            {
                return _alarmLevel;
            }
            set 
            {
                if (value < 0 || value > 1)
                {
                    _logger?.WriteText(new AlarmMessage
                    {
                        AlarmCode = AlarmCodes.CommandInfo,
                        Message = "Заданный уровень предупреждения вне диапазона от 0 до 1",
                        System = this,
                        Type = MessageType.Warning,
                        IsActive = false
                        
                    });
                    return;
                }
                
                if (value >= _warningLevel)
                {
                    _logger?.WriteText(new AlarmMessage
                    {
                        AlarmCode = AlarmCodes.CommandInfo,
                        Message = "Уровень тревоги не может быть выше предупредительного значения уровня",
                        System = this,
                        Type = MessageType.Warning,
                        IsActive = false
                    });
                    return;
                }
                _alarmLevel = value;
            }
        }
        
        /// <summary>
        /// Проверка при первом запуске
        /// </summary>
        private void CheckFirstRun()
        {
            if (_firstRun && _hydrogenTanks.Count > 0)
            {
                _logger?.WriteText(new AlarmMessage
                {
                    AlarmCode = AlarmCodes.StartupInfo,
                    Message = $"Инициализировано {_hydrogenTanks.Count} баков с водородом",
                    System = this,
                    Type = MessageType.Info,
                    IsActive = true
                });
            }
            _firstRun = false;
        }

        /// <summary>
        ///  Проверка доступных баков с водородом
        /// </summary>
        private void CheckAvailableHydrogenTanks()
        {
            if (_hydrogenTanks.Count <= 0)
            {
                _logger?.WriteText(new AlarmMessage
                {
                    AlarmCode = AlarmCodes.InitCount,
                    Message = "Не найдены баки с водором",
                    System = this,
                    Type = MessageType.Warning,
                    IsActive = true
                });
            }
        }

        /// <summary>
        /// Обновление информации на контрольных панелях
        /// </summary>
        private void UpdatePanels()
        {
            foreach (var controlPanel in _controlPanels)
            {
                var str = new StringBuilder();
                str.AppendLine($"{SystemName}");
                str.AppendLine("----------------");
                str.AppendLine($"Общий: {Level * 100}%,  {CurrentCapacity} / {MaxCapacity}");
                str.AppendLine("----------------");
                foreach (var hydrogenTank in _hydrogenTanks)
                {
                    var currentCapacity = CalcTankCurrentCapacity(hydrogenTank);
                    var level = CalcTankLevel(hydrogenTank);
                    str.AppendLine($"{hydrogenTank.CustomName}: {level * 100}%, {currentCapacity} / {hydrogenTank.Capacity}");
                }
                controlPanel.WriteText(str.ToString());
            }
        }

        private static double CalcTankCurrentCapacity(IMyGasTank tank)
        {
            return tank.Capacity * tank.FilledRatio;
        }

        private static double CalcTankLevel(IMyGasTank tank)
        {
            if (tank.Capacity == 0)
                return 0;
            return CalcTankCurrentCapacity(tank) / tank.Capacity;
        }
    }
}