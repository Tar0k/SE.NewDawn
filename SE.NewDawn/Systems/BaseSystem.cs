using System;
using System.Collections.Generic;
using System.Linq;

namespace IngameScript
{
    public abstract class BaseSystem
    {
        protected Dictionary<string, Action<List<string>>> AvailableCommands;
        protected ILogger Logger;
        
        protected BaseSystem()
        {
            AvailableCommands = new Dictionary<string, Action<List<string>>>();
            SystemName = GetType().Name;
            RefCustomData = GetType().Name;
        }

        protected BaseSystem(ILogger logger) : this()
        {
            Logger = logger;
        }
        
        /// <summary>
        /// Название системы для отображения в UI
        /// </summary>
        public string SystemName { get; set; }
        
        /// <summary>
        /// Ссылочное название системы в CustomData для определения принадлежности
        /// </summary>
        public string RefCustomData { get; set; }
        
        /// <summary>
        /// Текущий статус системы
        /// </summary>
        public virtual SystemStates SystemState { get; protected set; } = SystemStates.Active;

        /// <summary>
        /// Метод обработки команды от других систем.
        /// </summary>
        /// <param name="command">Входящая команда</param>
        /// <returns>Результат выполнения команды</returns>
        public virtual bool ExecuteCommand(string command)
        {
            // Проверки полученной команды на формат
            var cmd = command.Split(' ');
            if (cmd.Length < 2)
            {
                Logger?.WriteText(new AlarmMessage
                {
                    AlarmCode = AlarmCodes.CommandInfo,
                    Message = $"Получена команда в неверном формате: \"{command}\"",
                    System = this,
                    Type = MessageType.Warning,
                    IsActive = true
                });
                return false;
            }

            if (cmd[0] != RefCustomData)
            {
                Logger?.WriteText(new AlarmMessage
                {
                    AlarmCode = AlarmCodes.CommandInfo,
                    Message = $"Получена команда от другой системы: \"{command}\"",
                    System = this,
                    Type = MessageType.Warning,
                    IsActive = true
                });
                return false;
            }
            
            // Исполнение команды
            Action<List<string>> availableCommand;
            if (AvailableCommands.TryGetValue(cmd[1], out availableCommand))
            {
                if (cmd.Length > 2)
                {
                    var arguments = cmd.Skip(2).ToList();
                    availableCommand.Invoke(arguments);
                }
                else
                {
                    availableCommand.Invoke(new List<string>());
                }
                
                Logger?.WriteText(new AlarmMessage
                {
                    AlarmCode = AlarmCodes.CommandInfo,
                    Message = $"Выполнена команда: \"{command}\"",
                    System = this,
                    Type = MessageType.Info,
                    IsActive = true
                });
                return true;
            }
            Logger?.WriteText(new AlarmMessage
            {
                AlarmCode = AlarmCodes.CommandInfo,
                Message = $"Введена неизвестная команда {command}",
                System = this,
                Type = MessageType.Warning,
                IsActive = true
            });
            return false;
        }

        /// <summary>
        /// Получение доступных комманд от системы
        /// </summary>
        /// <returns>Список доступных комманд</returns>
        public virtual IEnumerable<string> GetCommands()
        {
            return AvailableCommands.Keys.Select(k => $"{SystemName} {k}");
        }
        
        // Метод обновления данных в системе.
        /// <summary>
        /// Метод обновления системы.
        /// Должен вызываться в каждом цикле для обновления данных
        /// </summary>
        public abstract void Update();

        public override string ToString()
        {
            return GetType().Name;
        }
    }
}