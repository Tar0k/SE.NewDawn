namespace IngameScript
{
    public interface ILogger
    {
        /// <summary>
        /// Записывает в журнал тревогу
        /// </summary>
        /// <param name="message">Сообщение о тревоге</param>
        /// <returns>Результат выполнения</returns>
        bool WriteText(AlarmMessage message);
        
        /// <summary>
        /// Записывает в журнал тревогу
        /// </summary>
        /// <param name="alarmCode">Код тревоги</param>
        /// <param name="text">Текст сообщения</param>
        /// <param name="system">Система источник</param>
        /// <param name="isActive">Активна на текущий момент</param>
        /// <returns>Результат выполнения</returns>
        bool WriteText(AlarmCodes alarmCode, string text,  BaseSystem system, bool? isActive);
    }
}