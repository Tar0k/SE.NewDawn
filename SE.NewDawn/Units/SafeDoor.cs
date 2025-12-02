using System;
using Sandbox.ModAPI.Ingame;

namespace IngameScript
{
    internal class SafeDoor: IDisposable
    {
        private readonly SafetySystem _safetySystem;
        private readonly IMyDoor _door;
        private int _openDoorTimer;
        private DoorStatus _prevDoorStatus;
        public event Action<IMyDoor> DoorStatusChanged;
        
        public SafeDoor(IMyDoor door, SafetySystem safetySystem, ILogger logger)
        {
            _door = door;
            _prevDoorStatus = _door.Status;
            _safetySystem = safetySystem;
            _safetySystem.UpdateDoors += Update;
            Update();
        }

        private void Update()
        {
            switch (_door.Status)
            {
                case DoorStatus.Open:
                    _openDoorTimer += 1;
                    if (_prevDoorStatus != DoorStatus.Open)
                    {
                        DoorStatusChanged?.Invoke(_door);
                    }

                    break;
                case DoorStatus.Closed:
                    _openDoorTimer = 0;
                    if (_prevDoorStatus != DoorStatus.Closed)
                    {
                        DoorStatusChanged?.Invoke(_door);
                    }

                    break;
                case DoorStatus.Opening:
                case DoorStatus.Closing:
                default:
                    break;
            }
            _prevDoorStatus = _door.Status;

            if (_openDoorTimer <= 1) return;
            _openDoorTimer = 0;
            _door.CloseDoor();
        }

        public void Dispose()
        {
            _safetySystem.UpdateDoors -= Update;
        }
    }
}