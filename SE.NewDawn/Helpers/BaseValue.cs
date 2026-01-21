using System;
using System.Globalization;

namespace IngameScript
{
    public class BaseValue
    {
        public decimal Value { get; set; }
        public Units Units { get; set; } = Units.Kilogram;

        public override string ToString()
        {
            var absValue = Math.Abs(Value);
            
            switch (Units)
            {
                case Units.Liter:
                    return absValue % 1000 > 0
                        ? $"{Math.Round(absValue / 1000, 3)} кубов"
                        : $"{Math.Round(absValue, 3)} литров";
                case Units.Kilogram:
                    return absValue % 1000 > 0 ? $"{Math.Round(absValue / 1000, 3)} тонн" : $"{Math.Round(absValue)} килограмм";
                default:
                    return Math.Round(Value, 2).ToString(CultureInfo.InvariantCulture);
            }
        }

        public static BaseValue operator +(BaseValue a, BaseValue b)
        {
            if (a.Units != b.Units)
                return new BaseValue();
            return new BaseValue {Value = a.Value + b.Value};
        }

        public static BaseValue operator -(BaseValue a, BaseValue b)
        {
            if (a.Units != b.Units)
                return new BaseValue();
            return new BaseValue {Value = a.Value - b.Value};
        }
        
        public static BaseValue operator *(BaseValue a, BaseValue b)
        {
            if (a.Units != b.Units)
                return new BaseValue();
            return new BaseValue {Value = a.Value * b.Value};
        }
        
        public static BaseValue operator /(BaseValue a, BaseValue b)
        {
            if (a.Units != b.Units)
                return new BaseValue();
            return new BaseValue {Value = a.Value / b.Value};
        }
        
    }
}