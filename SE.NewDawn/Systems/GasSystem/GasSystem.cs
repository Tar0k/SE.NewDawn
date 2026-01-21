using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.ModAPI.Ingame;

namespace IngameScript
{
    /// <summary>
    /// Система контроля водорода
    /// </summary>
    public class GasSystem : BaseSystem
    {
        private static readonly string[] HydrogenTanksDefinitions = {
            "MyObjectBuilder_OxygenTank/LargeHydrogenTank",
            "MyObjectBuilder_OxygenTank/LargeHydrogenTankSmall",
            "MyObjectBuilder_OxygenTank/LargeHydrogenTankIndustrial",
            "MyObjectBuilder_OxygenTank/SmallHydrogenTank",
            "MyObjectBuilder_OxygenTank/SmallHydrogenTankLab",
            "MyObjectBuilder_OxygenTank/SmallHydrogenTankSmall"
        };

        private static readonly string[] OxygenTankDefinitions =
        {
            "MyObjectBuilder_OxygenTank/LargeBlockOxygenTankLab",
            "MyObjectBuilder_OxygenTank/OxygenTankSmall",
            "MyObjectBuilder_OxygenTank/SmallOxygenTankSmall"
        };

        private readonly CoreSystem _coreSystem;
        private readonly ILogger _logger;
        
        private readonly List<IMyGasTank> _hydrogenTanks;
        private readonly List<IMyGasTank> _oxygenTanks;
        
        private bool _firstRun = true;
        private double _warningLevel;
        private double _alarmLevel;
        private readonly List<IMyTextPanel> _controlPanels;
        private bool _stockPile;
        private bool _autoRefillBottles;
        
        public GasSystemSettings Settings { get; set; } = new GasSystemSettings();

        public event Action<decimal> HydrogenWarningLevelTriggered;
        public event Action<decimal> HydrogenAlarmLevelTriggered;

        public event Action<decimal> OxygenWarningLevelTriggered;
        public event Action<decimal> OxygenAlarmLevelTriggered;

        public GasSystem(Program program, CoreSystem core, ILogger logger, GasSystemSettings gasSystemSettings = null) : base(logger)
        {
            SystemName = "Система контроля водорода";

            if (gasSystemSettings != null)
                Settings = gasSystemSettings;
            
            var tanks = new List<IMyGasTank>();
            program.GridTerminalSystem.GetBlocksOfType(tanks);
            tanks = tanks.Where(t => t.IsSameConstructAs(program.Me)).ToList();
            
            _hydrogenTanks = tanks
                .Where(t => HydrogenTanksDefinitions.Contains(t.DefinitionDisplayNameText))
                .ToList();

            _oxygenTanks = tanks
                .Where(t => OxygenTankDefinitions.Contains(t.DefinitionDisplayNameText))
                .ToList();
            
            _coreSystem = core;
            _coreSystem.UpdateSystems += Update;
            _logger = logger;
            
            var panels = new List<IMyTextPanel>();
            program.GridTerminalSystem.GetBlocksOfType(panels);
            _controlPanels = panels
                .Where(p => p.IsSameConstructAs(program.Me) && p.CustomData.Contains(RefCustomData))
                .ToList();
            
            CheckFirstRun();
            CheckAvailableHydrogenTanks();
            StockPile = false;
            AutoRefillBottles = false;
        }
        
        public override void Update()
        {
            switch (SystemState)
            {
                case SystemStates.Active:
                    if (HydrogenLevel < Settings.HydrogenAlarmLevel)
                    {
                        HydrogenAlarmLevelTriggered?.Invoke(CurrentHydrogenCapacity);
                        SystemState = SystemStates.Alarm;
                    }
                    if (HydrogenLevel < Settings.HydrogenWarningLevel)
                    {
                        HydrogenWarningLevelTriggered?.Invoke(CurrentHydrogenCapacity);
                        SystemState = SystemStates.Warning;
                    }
                    break;
                case SystemStates.Warning:
                    if (HydrogenLevel < Settings.HydrogenAlarmLevel)
                    {
                        HydrogenAlarmLevelTriggered?.Invoke(CurrentHydrogenCapacity);
                        SystemState = SystemStates.Alarm;
                    }
                    if (HydrogenLevel > Settings.HydrogenWarningLevel)
                    {
                        SystemState = SystemStates.Active;
                    }
                    break;
                case SystemStates.Alarm:
                    if (HydrogenLevel > Settings.HydrogenAlarmLevel)
                    {
                        if (HydrogenLevel < Settings.HydrogenWarningLevel)
                        {
                            HydrogenWarningLevelTriggered?.Invoke(CurrentHydrogenCapacity);
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
        public decimal MaxHydrogenCapacity => (decimal)_hydrogenTanks.Select(t => t.Capacity).Sum();
        
        /// <summary>
        /// Фактический водород, в литрах
        /// </summary>
        public decimal CurrentHydrogenCapacity => (decimal)_hydrogenTanks.Select(CalcTankCurrentCapacity).Sum();

        /// <summary>
        /// Текущая заполненность, в процентах
        /// </summary>
        public double HydrogenLevel
        {
            get
            {
                if (MaxHydrogenCapacity == 0)
                    return 0;
                return (double)(CurrentHydrogenCapacity / MaxHydrogenCapacity);
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
        /// Вкл/Выкл заполнение системы
        /// </summary>
        public bool StockPile
        {
            get
            {
                return _stockPile;
            } 
            set
            {
                foreach (var hydrogenTank in _hydrogenTanks)
                {
                    hydrogenTank.Stockpile = value;
                }
                _stockPile = value;
            }
        }

        /// <summary>
        /// Вкл/Выкл автозаполнение ситстемы
        /// </summary>
        public bool AutoRefillBottles
        {
            get
            {
                return _autoRefillBottles;
            }
            set
            {
                foreach (var hydrogenTank in _hydrogenTanks)
                {
                    hydrogenTank.AutoRefillBottles = value;
                }
                _autoRefillBottles = value;
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
                str.AppendLine($"Общий: {Math.Round(HydrogenLevel * 100, 2)}%,  {CurrentHydrogenCapacity} / {MaxHydrogenCapacity}");
                str.AppendLine("----------------");
                foreach (var hydrogenTank in _hydrogenTanks)
                {
                    var currentCapacity = CalcTankCurrentCapacity(hydrogenTank);
                    var level = CalcTankLevel(hydrogenTank);
                    str.AppendLine($"{hydrogenTank.CustomName}: {Math.Round(level * 100, 2)}%, {currentCapacity} / {hydrogenTank.Capacity}");
                }
                str.AppendLine("----------------");
                controlPanel.WriteText(str.ToString());
            }
        }

        /// <summary>
        /// Рассчитать фактический объем газа в баке, литры
        /// </summary>
        /// <param name="tank">Резервуар для расчета</param>
        /// <returns>Фактический объем, литры</returns>
        private static double CalcTankCurrentCapacity(IMyGasTank tank)
        {
            return tank.Capacity * tank.FilledRatio;
        }

        /// <summary>
        /// Рассчитать уровень в баке, в процентах
        /// </summary>
        /// <param name="tank">Резервуар для расчета</param>
        /// <returns>Уровень в баке, в процентах</returns>
        private static double CalcTankLevel(IMyGasTank tank)
        {
            if (tank.Capacity == 0)
                return 0;
            return CalcTankCurrentCapacity(tank) / tank.Capacity;
        }
    }
}