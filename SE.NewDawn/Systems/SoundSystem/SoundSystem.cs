using System;
using System.Collections.Generic;
using SpaceEngineers.Game.ModAPI.Ingame;

namespace IngameScript
{
    internal class SoundSystem : BaseSystem, IDisposable
    {
        private readonly CoreSystem _coreSystem;
        private readonly ILogger _logger;
        private bool _firstRun = true;
        private bool _soundOn = true;
        private SoundStates _soundState;
        
        private readonly List<IMySoundBlock> _soundBlocks = new List<IMySoundBlock>();

        private SoundStates SoundState
        {
            get 
            {
                return _soundState;
            }
            set
            {
                switch (value)
                {
                    
                    case SoundStates.Alarm:
                        AlarmOn();
                        break;
                    case SoundStates.RandomMusic:
                    case SoundStates.InfoMessage:
                        break;
                    case SoundStates.Default:
                        Default();
                        break;
                    case SoundStates.Stop:
                        Stop();
                        break;
                    case SoundStates.Off:
                    case SoundStates.Mixed:
                    case SoundStates.On:
                        TurnOn();
                        break;
                    default:
                        TurnOff();
                        break;
                }
            }
        }

        public SoundSystem(Program program, CoreSystem core, ILogger logger) : base(logger)
        {
            SystemName = "Звуковая система";
            program.GridTerminalSystem.GetBlocksOfType(_soundBlocks);
            _coreSystem = core;
            _coreSystem.UpdateSystems += Update;
            _coreSystem.AlarmTriggered += SwitchAlarm;
            _logger = logger;
            AvailableCommands = new Dictionary<string, Action>
            {
                { "TurnOff", () =>
                    {
                        SoundState = SoundStates.Off;
                    }
                },
                { "TurnOn", () =>
                    {
                        SoundState = SoundStates.On;
                    }
                },
                {
                    "AlarmOn", () =>
                    {
                        SoundState = SoundStates.Alarm;
                    }
                },
                { "AlarmOff", AlarmOff },
                {
                    "Default", () =>
                    {
                        SoundState = SoundStates.Default;
                    }
                },
                {
                    "Stop", () =>
                    {
                        SoundState = SoundStates.Stop;
                    }
                },
                { "SwitchAlarm", SwitchAlarm }
            };
            program.GridTerminalSystem.GetBlocksOfType(_soundBlocks);
            Default();
        }

        private void TurnOn()
        {
            foreach (var soundBlock in  _soundBlocks)
            {
                soundBlock.Enabled = soundBlock.Enabled == false;
            }
            _soundOn = true;
            SoundState = SoundStates.On;
        }

        private void TurnOff()
        {
            foreach (var soundBlock in  _soundBlocks)
            {
                soundBlock.Enabled = soundBlock.Enabled != true;
            }
            _soundOn = false;
            _soundState = SoundStates.Off;
            
            _logger?.WriteText(new AlarmMessage
            {
                AlarmCode = AlarmCodes.OnOffInfo,
                Message = "Звук выключен",
                System = this,
                Type = MessageType.Info,
                IsActive = true
            });
        }

        private void Default()
        {
            foreach (var soundBlock in _soundBlocks)
            {
                soundBlock.SelectedSound = null;
            }
            
            _logger?.WriteText(new AlarmMessage
            {
                AlarmCode = AlarmCodes.OnOffInfo,
                Message = "Звук сброшен",
                System = this,
                Type = MessageType.Info,
                IsActive = true
            });
        }
        
        // Переключить тревогу без условий
        private void SwitchAlarm()
        {
            if (!_soundOn) return;
            if (SoundState == SoundStates.Alarm ) AlarmOff();
            else AlarmOn();
        }

        // Переключить тревогу по сообщению
        private void SwitchAlarm(AlarmMessage alarm)
        {
            if (!_soundOn) return;
            
            if (alarm.IsActive)
            {
                AlarmOn();
            }
            else
            {
                AlarmOff();
            }
        }


        private void AlarmOn()
        {
            if (!_soundOn) return;
            foreach (var soundBlock in _soundBlocks)
            {
                soundBlock.SelectedSound = "SoundBlockAlert1";
                soundBlock.Play();
            }
            _soundState = SoundStates.Alarm;
            
            _logger?.WriteText(new AlarmMessage
            {
                AlarmCode = AlarmCodes.OnOffInfo,
                Message = "Включена тревога",
                System = this,
                Type = MessageType.Info,
                IsActive = true
            });
        }

        private void AlarmOff()
        {
            Stop();
            _logger?.WriteText(new AlarmMessage
            {
                AlarmCode = AlarmCodes.OnOffInfo,
                Message = "Тревога выключена",
                System = this,
                Type = MessageType.Info,
                IsActive = true
            });
        }

        private void Stop()
        {
            foreach (var soundBlock in _soundBlocks)
            {
                soundBlock.Stop();
                soundBlock.SelectedSound = null;
            }
            _soundState = SoundStates.Stop;
        }

        public override void Update()
        {
            if (_firstRun)
                CheckFirstRun();
            CheckAvailableLights();

        }
        
        private void CheckFirstRun()
        {
            if (_firstRun && _soundBlocks.Count > 0)
            {
                _logger?.WriteText(new AlarmMessage
                {
                    AlarmCode = AlarmCodes.StartupInfo,
                    Message = $"Инициализировано {_soundBlocks.Count} источников света",
                    System = this,
                    Type = MessageType.Info,
                    IsActive = true
                });
            }
            _firstRun = false;
        }

        private void CheckAvailableLights()
        {
            if (_soundBlocks.Count <= 0)
            {
                _logger?.WriteText(new AlarmMessage
                {
                    AlarmCode = AlarmCodes.InitCount,
                    Message = "Не найдено динамиков",
                    System = this,
                    Type = MessageType.Warning,
                    IsActive = true
                });
            }
        }

        public void Dispose()
        {
            _coreSystem.UpdateSystems -= Update;
            _coreSystem.AlarmTriggered -= SwitchAlarm;
        }
    }
}