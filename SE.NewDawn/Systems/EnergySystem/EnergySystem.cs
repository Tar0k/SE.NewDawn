using System.Collections.Generic;
using System.Linq;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;

namespace IngameScript
{
    public class EnergySystem : BaseSystem
    {
        private readonly CoreSystem _coreSystem;
        private readonly ILogger _logger;
        
        private readonly List<IMyTerminalBlock> _blocks = new List<IMyTerminalBlock>();
        
        private readonly List<IMyBatteryBlock> _batteries = new List<IMyBatteryBlock>();
        private readonly List<IMyWindTurbine> _windTurbines = new List<IMyWindTurbine>();
        private readonly List<IMySolarPanel> _solarPanels = new List<IMySolarPanel>();
        private readonly List<IMyTerminalBlock> _hydrogenEngines;
        private readonly List<IMyTextPanel> _controlPanels;

        private BatteriesChargeMode _chargeMode = BatteriesChargeMode.Auto;
        
        public EnergySystem(Program program, CoreSystem core, ILogger logger) : base(logger)
        {
            SystemName = "Энергосистема";
            _coreSystem = core;
            _coreSystem.UpdateSystems += Update;
            _logger = logger;

            program.GridTerminalSystem.GetBlocksOfType(_batteries);
            _batteries = _batteries.Where(b => b.IsSameConstructAs(program.Me)).ToList();
            program.GridTerminalSystem.GetBlocksOfType(_windTurbines);
            _windTurbines = _windTurbines.Where(b => b.IsSameConstructAs(program.Me)).ToList();
            program.GridTerminalSystem.GetBlocksOfType(_solarPanels);
            _solarPanels =  _solarPanels.Where(b => b.IsSameConstructAs(program.Me)).ToList();
            
            program.GridTerminalSystem.GetBlocks(_blocks);
            _hydrogenEngines = _blocks
                .Where(b =>
                    b.IsSameConstructAs(program.Me)
                    && (
                        b.DefinitionDisplayNameText == "MyObjectBuilder_HydrogenEngine/LargeHydrogenEngine" 
                        || b.DefinitionDisplayNameText == "MyObjectBuilder_HydrogenEngine/SmallHydrogenEngine"
                        )
                    )
                .ToList();

            var panels = new List<IMyTextPanel>();
            program.GridTerminalSystem.GetBlocksOfType(panels);
            _controlPanels = panels
                .Where(p => p.IsSameConstructAs(program.Me) && p.CustomData.Contains(RefCustomData))
                .ToList();
        }
        
        public override void Update()
        {
            CheckChargeMode();
        }
        
        /// <summary>
        /// Режим зарядки батарей
        /// </summary>
        public BatteriesChargeMode ChargeMode
        {
            get
            {
                return _chargeMode;
            }
            set
            {
                if (_chargeMode == value)
                    _logger?.WriteText(new AlarmMessage
                    {
                        AlarmCode = AlarmCodes.CommandInfo,
                        Message = $"Режим {value} уже установлен",
                        System = this,
                        Type = MessageType.Warning,
                        IsActive = false
                    });
                switch (value)
                {
                    case BatteriesChargeMode.Auto:
                        foreach (var battery in _batteries)
                        {
                            battery.ChargeMode = Sandbox.ModAPI.Ingame.ChargeMode.Auto;
                        }
                        break;
                    case BatteriesChargeMode.Discharge:
                        foreach (var battery in _batteries)
                        {
                            battery.ChargeMode = Sandbox.ModAPI.Ingame.ChargeMode.Discharge;
                        }
                        break;
                    case BatteriesChargeMode.Recharge:
                        foreach (var battery in _batteries)
                        {
                            battery.ChargeMode = Sandbox.ModAPI.Ingame.ChargeMode.Recharge;
                        }
                        break;
                    case BatteriesChargeMode.Mixed:
                    default:
                        _logger?.WriteText(new AlarmMessage
                        {
                            AlarmCode = AlarmCodes.CommandInfo,
                            Message = $"Нельзя установить {value}",
                            System = this,
                            Type = MessageType.Warning,
                            IsActive = false
                        });
                        return;
                }
                _chargeMode = value;
            }
        }

        /// <summary>
        /// Проверка режима зарядки батарей
        /// </summary>
        /// <returns>Режим зарядки батарей</returns>
        private BatteriesChargeMode CheckChargeMode()
        {
            if (_batteries.All(battery => battery.ChargeMode == Sandbox.ModAPI.Ingame.ChargeMode.Auto))
            {
                ChargeMode = BatteriesChargeMode.Auto;
                return ChargeMode;
            }
            
            if (_batteries.All(battery => battery.ChargeMode == Sandbox.ModAPI.Ingame.ChargeMode.Recharge))
            {
                ChargeMode = BatteriesChargeMode.Recharge;
                return ChargeMode;
            }

            if (_batteries.All(battery => battery.ChargeMode == Sandbox.ModAPI.Ingame.ChargeMode.Discharge))
            {
                ChargeMode = BatteriesChargeMode.Discharge;
                return ChargeMode;
            }
            
            ChargeMode = BatteriesChargeMode.Mixed;
            return ChargeMode;
        }

        #region Ветряные турбины
        
        /// <summary>
        /// Вычисление текущей выходной мощности от ветряных турбин
        /// </summary>
        /// <returns>Текущая выходная мощность от ветряных турбин</returns>
        private decimal CalcCurrentWindTurbinesOutput() => _windTurbines.Sum(windTurbine => (decimal)windTurbine.CurrentOutput);

        /// <summary>
        /// Вычисление фактической максимальной выходной мощности от ветряных турбин
        /// С учетом коэффициента места установки
        /// </summary>
        /// <returns>Фактическая максимальная мощность от ветряных турбин</returns>
        private static decimal CalcActualMaxWindTurbinesOutput()
        {
            // TODO: Рассчитать фактическую максимальную мощность (с учетом места установки)
            return 0;
        }
        
        /// <summary>
        /// Вычисление процента использования ветряных турбин
        /// </summary>
        /// <returns>Процент использования ветряных турбин</returns>
        private decimal CalcWindTurbinesOutputRatio()
        {
            if (CalcActualMaxWindTurbinesOutput() == 0)
                return 0;
            return CalcCurrentWindTurbinesOutput() / CalcActualMaxWindTurbinesOutput();
        }

        #endregion
        
      
        #region Солнечные панели

        /// <summary>
        /// Вычисление текущей выходной мощности от солнечных панелей
        /// </summary>
        /// <returns>Текущая выходная мощность от солнечных панелей</returns>
        private decimal CalcCurrentSolarPanelOutput() => _solarPanels.Sum(solarPanel => (decimal)solarPanel.CurrentOutput);
        
        /// <summary>
        /// Вычисление фактической максимальной выходной мощности от солнечных панелей
        /// С учетом коэффициента места установки
        /// </summary>
        /// <returns>Фактическая максимальная мощность от солнечных панелей</returns>
        private static decimal CalcActualMaxSolarPanelsOutput()
        {
            // TODO: Рассчитать фактическую максимальную мощность (с учетом места установки)
            return 0;
        }

        /// <summary>
        /// Вычисление процента использования солнечных панелей
        /// </summary>
        /// <returns>Процент использования солнечных панелей</returns>
        private decimal CalcSolarPanelsOutputRatio()
        {
            if (CalcActualMaxSolarPanelsOutput() == 0)
                return 0;
            return CalcCurrentSolarPanelOutput() / CalcActualMaxSolarPanelsOutput();
        }

        #endregion

      
        
        // Батареи

        #region Батареи

        /// <summary>
        /// Вычисление выходной мощности от батарей
        /// </summary>
        /// <returns>Выходная мощность от батарей</returns>
        private decimal CalcBatteriesOutput() => _batteries.Sum(battery => (decimal)battery.CurrentOutput);
        
        /// <summary>
        /// Вычисление максимальной возможной выходной мощности
        /// </summary>
        /// <returns>Максимальная возможная выходная мощность</returns>
        private decimal CalcBatteriesMaxOutput() => _batteries.Sum(battery => (decimal)battery.MaxOutput);
        
        /// <summary>
        /// Вычисление отношение текущей мощности к выходной
        /// </summary>
        /// <returns>Отношение текущей мощности к выходной</returns>
        private decimal CalcBatteriesOutputRatio() => CalcBatteriesOutput() / CalcBatteriesMaxOutput();

        /// <summary>
        /// Вычислить текущий заряд батарей
        /// </summary>
        /// <returns>Текущий заряд батарей, в MW</returns>
        private decimal CalcBatteriesCurrentCharge() => _batteries.Sum(battery => (decimal)battery.CurrentStoredPower);

        /// <summary>
        /// Вычислить максимальный возможный заряд батарей
        /// </summary>
        /// <returns>Максимальный возможный заряд батарей</returns>
        private decimal CalcBatteriesMaxCharge() => _batteries.Max(battery => (decimal)battery.MaxStoredPower);

        /// <summary>
        /// Вычислить текущий заряд батарей, в процентах
        /// </summary>
        /// <returns>Текущий заряд батарей, в процентах</returns>
        private decimal CalcBatteriesChargeLevel() => CalcBatteriesCurrentCharge() / CalcBatteriesMaxCharge();
        
        /// <summary>
        /// Вычислить входную мощность на зарядку батарей, в MW
        /// </summary>
        /// <returns>Входная мощность на зарядку батарей, в MW</returns>
        private decimal CalcBatteriesInput() => _batteries.Sum(battery => (decimal)battery.CurrentInput);
        
        /// <summary>
        /// Вычислить максимально возможную мощность на зарядку батарей
        /// </summary>
        /// <returns>Максимально возможная мощность на зарядку батарей</returns>
        private decimal CalcBatteriesMaxInput() => _batteries.Sum(battery => (decimal)battery.MaxInput);
        
        /// <summary>
        /// Вычислить входную мощность на зарядку батарей, в процентах
        /// </summary>
        /// <returns>Входная мощность на зарядку батарей, в процентах</returns>
        private decimal CalcBatteriesInputLevel() => CalcBatteriesCurrentCharge() / CalcBatteriesMaxCharge();

        #endregion
        
        #region Водородные движки

        /// <summary>
        /// Вычисление выходной мощности от водородных движков
        /// </summary>
        /// <returns></returns>
        private static decimal CalcHydrogenEngineOutput()
        {
            //TODO: Рассчитать как получить правильное значение
            return 0;
        }

        #endregion
        
        #region Общие данные

        /// <summary>
        /// Вычисление общее выходная мощность энергосистемы
        /// </summary>
        /// <returns>Общая выходная мощность энергосистемы</returns>
        private decimal CalcTotalEnergyOutput()
        {
            decimal total = 0;
            total += CalcCurrentWindTurbinesOutput();
            total += CalcCurrentSolarPanelOutput();
            total += CalcHydrogenEngineOutput();
            total += CalcBatteriesOutput();
            return total;
        }

        #endregion
    }
}