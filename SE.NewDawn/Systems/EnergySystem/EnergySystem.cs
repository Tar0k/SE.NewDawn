using System.Collections.Generic;
using System.Linq;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;

namespace IngameScript.EnergySystem
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
                    b.DefinitionDisplayNameText == "MyObjectBuilder_HydrogenEngine/LargeHydrogenEngine" 
                    || b.DefinitionDisplayNameText == "MyObjectBuilder_HydrogenEngine/SmallHydrogenEngine")
                .ToList();
        }
        
        
        public override void Update()
        {
            
        }

        public decimal CalcCurrentWindTurbinesOutput()
        {
            return _windTurbines.Sum(windTurbine => (decimal)windTurbine.CurrentOutput);
        }

        public decimal CalcCurrentSolarPanelOutput()
        {
            return _solarPanels.Sum(solarPanel => (decimal)solarPanel.CurrentOutput);
        }
    }
}