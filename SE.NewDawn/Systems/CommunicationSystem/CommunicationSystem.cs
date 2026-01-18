using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Sandbox.ModAPI.Ingame;

namespace IngameScript
{
    public class CommunicationSystem : BaseSystem
    {
        private readonly CoreSystem _coreSystem;
        private readonly ILogger _logger;
        
        private readonly List<IMyRadioAntenna> _radioAntennas = new List<IMyRadioAntenna>();
        private readonly List<IMyLaserAntenna> _laserAntennas = new List<IMyLaserAntenna>();
        private readonly IMyIntergridCommunicationSystem _igc;
        private readonly IMyUnicastListener _unicastListener;
        private readonly IMyBroadcastListener _broadcastListener;
        private readonly string _broadcastTag;
        private readonly string _unicastTag;

        public CommunicationSystem(Program program, CoreSystem core, ILogger logger) : base(logger)
        {
            SystemName = "Связь";
            _coreSystem = core;
            _coreSystem.UpdateSystems += Update;
            _logger = logger;
            _igc = program.IGC;
            _unicastListener = _igc.UnicastListener;

            _broadcastTag = $"Broadcast_{program.Me.CubeGrid.DisplayName}";
            _unicastTag = $"Unicast_{program.Me.CubeGrid.DisplayName}";
            _broadcastListener = _igc.RegisterBroadcastListener(_broadcastTag);
            _broadcastListener.SetMessageCallback(_broadcastTag);
            
            program.GridTerminalSystem.GetBlocksOfType(_radioAntennas);
            _radioAntennas = _radioAntennas.Where(b => b.IsSameConstructAs(program.Me)).ToList();
            program.GridTerminalSystem.GetBlocksOfType(_laserAntennas);
            _laserAntennas = _laserAntennas.Where(b => b.IsSameConstructAs(program.Me)).ToList();
            
            AvailableCommands = new Dictionary<string, Action<List<string>>>
            {
                { "SendUnicastMessage", _ =>
                {
                    SendUnicastMessage(0, "TEST unicast message");
                }},
                { "SendBroadcastMessage", _ =>
                {
                    SendBroadcastMessage("TEST broadcast message");
                }}
            };
        }
        
        public override void Update()
        {
            while (_unicastListener.HasPendingMessage)
            {
               ResolveUnicastMessage();
            }

            while (_broadcastListener.HasPendingMessage)
            {
                ResolveBroadcastMessage();
            }
        }

        private void ResolveUnicastMessage()
        {
            var message = _unicastListener.AcceptMessage();
            ResolveMessage(message);
        }

        private void ResolveBroadcastMessage()
        {
            var message = _broadcastListener.AcceptMessage();
            ResolveMessage(message);
        }

        private void ResolveMessage(MyIGCMessage message)
        {
            var data = message.Data as string;
            if (data != null)
            {
                _logger.WriteText(AlarmCodes.CommandInfo, $"{message.Source.ToString()}: {message}", this, false);
            }
            else
            {
                _logger.WriteText(AlarmCodes.SystemWarning, $"Received unknown message type: {message.Data.GetType()}", this, false);
            }
        }
        
        public void SendBroadcastMessage(string message, string tag = null)
        {
            if (tag == null)
                tag = _broadcastTag;
            _igc.SendBroadcastMessage(message, tag);
        }
        
        public void SendUnicastMessage(long targetId, string message, string tag = null)
        {
            if (tag == null)
                tag = _unicastTag;
            _igc.SendUnicastMessage(targetId, message, tag);
        }
    }
}