using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    public partial class Program : MyGridProgram
    {
        private readonly List<IMyTerminalBlock> _blocks = new List<IMyTerminalBlock>();
        private readonly IMyTextSurface _programBlockDisplay;
        
        private readonly IMyMotorBase _leftFrontRotor;
        private readonly IMyMotorBase _rightFrontRotor;
        private readonly IMyMotorBase _leftRearRotor;
        private readonly IMyMotorBase _rightRearRotor;
        
        private readonly IMyShipConnector _frontConnector;
        private readonly IMyShipConnector _backConnector;

        private const float NominalSpeed = 10f;
        private float _currentSpeed = 0.0f;

        public Program()
        {
            _programBlockDisplay = Me.GetSurface(0);
            GridTerminalSystem.GetBlocksOfType(_blocks, rotor => rotor.IsSameConstructAs(Me));
            var rotors = _blocks.OfType<IMyMotorBase>().ToList();
            var connectors = _blocks.OfType<IMyShipConnector>().ToList();
            
            var text = new StringBuilder();

            _leftFrontRotor = rotors.FirstOrDefault(r => string.Equals(r.CustomData, "LeftFront", StringComparison.CurrentCultureIgnoreCase));
            if (_leftFrontRotor == null)
                text.AppendLine("Cannot find left front rotor");
            
            _rightFrontRotor = rotors.FirstOrDefault(r => string.Equals(r.CustomData, "RightFront", StringComparison.CurrentCultureIgnoreCase));
            if (_rightFrontRotor == null)
                text.AppendLine("Cannot find right front rotor");
            
            _leftRearRotor = rotors.FirstOrDefault(r => string.Equals(r.CustomData, "LeftRear", StringComparison.CurrentCultureIgnoreCase));
            if (_leftRearRotor == null)
                text.AppendLine("Cannot find left rear rotor");
            
            _rightRearRotor = rotors.FirstOrDefault(r => string.Equals(r.CustomData, "RightRear", StringComparison.CurrentCultureIgnoreCase));
            if (_rightRearRotor == null)
                text.AppendLine("Cannot find right rear rotor");
            
            if (_leftFrontRotor != null && _rightFrontRotor != null &&  _leftRearRotor != null && _rightRearRotor != null)
            {
                text.AppendLine("Rotors initialised");
            }
            
            _frontConnector = connectors.FirstOrDefault(c => c.CustomData == "FrontConnector");
            if (_frontConnector == null)
                text.AppendLine("Cannot find FrontConnector");
            
            _backConnector = connectors.FirstOrDefault(c => c.CustomData == "BackConnector");
            if (_backConnector == null)
                text.AppendLine("Cannot find BackConnector");
            
            _programBlockDisplay.WriteText(text.ToString());
        }

        public void Save()
        {
        }

        public void Main(string argument, UpdateType updateSource)
        {
            switch (updateSource)
            {
                case UpdateType.None:
                    break;
                case UpdateType.Terminal:
                    switch (argument)
                    {
                        case "FrontMove":
                            Echo("Front move");
                            _programBlockDisplay.WriteText("Front move");
                            FrontMove();
                            break;
                        case "BackMove":
                            Echo("Back move");
                            _programBlockDisplay.WriteText("Back move");
                            BackMove();
                            break;
                        case "ResetSpeed":
                            Echo("Reset speed");
                            _programBlockDisplay.WriteText("Reset speed");
                            ResetSpeed();
                            break;
                    }
                    break;
                case UpdateType.Trigger:
                    switch (argument)
                    {
                        case "FrontMove":
                            Echo("Front move");
                            _programBlockDisplay.WriteText("Front move");
                            FrontMove();
                            break;
                        case "BackMove":
                            Echo("Back move");
                            _programBlockDisplay.WriteText("Back move");
                            BackMove();
                            break;
                        case "ResetSpeed":
                            Echo("Reset speed");
                            _programBlockDisplay.WriteText("Reset speed");
                            ResetSpeed();
                            break;
                    }
                    break;
                case UpdateType.Mod:
                    break;
                case UpdateType.Script:
                    switch (argument)
                    {
                        case "FrontMove":
                            Echo("Front move");
                            _programBlockDisplay.WriteText("Front move");
                            FrontMove();
                            break;
                        case "BackMove":
                            Echo("Back move");
                            _programBlockDisplay.WriteText("Back move");
                            BackMove();
                            break;
                        case "ResetSpeed":
                            Echo("Reset speed");
                            _programBlockDisplay.WriteText("Reset speed");
                            ResetSpeed();
                            break;
                    }
                    break;
                case UpdateType.Update1:
                    break;
                case UpdateType.Update10:
                    break;
                case UpdateType.Update100:
                    if (_frontConnector.Status == MyShipConnectorStatus.Connectable)
                    {
                        
                    }
                    break;
                case UpdateType.Once:
                    break;
                case UpdateType.IGC:
                    break;
                default:
                    _programBlockDisplay.WriteText("Unknown update type");
                    break;
            }
        }

        private void FrontMove()
        {
            _leftFrontRotor.SetValue("Velocity", NominalSpeed);
            _leftRearRotor.SetValue("Velocity", NominalSpeed);
            
            _rightFrontRotor.SetValue("Velocity", -NominalSpeed);
            _rightRearRotor.SetValue("Velocity", -NominalSpeed);
        }

        private void BackMove()
        {
            _leftFrontRotor.SetValue("Velocity", -NominalSpeed);
            _leftRearRotor.SetValue("Velocity", -NominalSpeed);
            
            _rightFrontRotor.SetValue("Velocity", NominalSpeed);
            _rightRearRotor.SetValue("Velocity", NominalSpeed);
        }
        
        private void ResetSpeed()
        {
            _leftFrontRotor.SetValue("Velocity", 0.0f);
            _leftRearRotor.SetValue("Velocity", 0.0f);
            
            _rightFrontRotor.SetValue("Velocity", 0.0f);
            _rightRearRotor.SetValue("Velocity", 0.0f);
        }
        
    }
}