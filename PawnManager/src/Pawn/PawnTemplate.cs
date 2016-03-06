using System;
using System.Collections.Generic;

namespace PawnManager
{
    public abstract class PawnTemplateElement
    {
        public string Label { get; set; } = "";

        public abstract IEnumerable<PawnTemplateElement> Descendants();
    }
    
    public class PawnTemplateCategory : PawnTemplateElement
    {
        public List<PawnTemplateElement> Children { get; set; }
         = new List<PawnTemplateElement>();
        
        public override IEnumerable<PawnTemplateElement> Descendants()
        {
            yield return this;
            foreach (PawnTemplateElement child in Children)
            {
                foreach (PawnTemplateElement child2 in child.Descendants())
                {
                    yield return child2;
                }
            }
        }
    }

    public abstract class PawnTemplateParameter : PawnTemplateElement
    {
        public string Key { get; set; }

        public abstract PawnTreeParameter CreateTreeParameter();

        public override IEnumerable<PawnTemplateElement> Descendants()
        {
            yield return this;
        }
    }
    
    public class PawnTemplateParameterName : PawnTemplateParameter
    {
        private const int maxNameLength = 25;
        public int MaxNameLength { get { return maxNameLength; } }

        public override PawnTreeParameter CreateTreeParameter()
        {
            return new PawnTreeParameterName { Template = this };
        }
    }
    
    public class PawnTemplateParameterDropDown : PawnTemplateParameter
    {
        public class Option
        {
            public string Label { get; set; } = "";
            public int Value { get; set; }
        }

        public bool AllowCustom { get; set; } = false;

        public List<Option> Options { get; } = new List<Option>();
        private Dictionary<int, int> optionValuesToIndex
          = new Dictionary<int, int>();

        public void AddOption(Option option)
        {
            optionValuesToIndex.Add(option.Value, Options.Count);
            Options.Add(option);
        }

        public int GetOptionIndexFromValue(int optionValue)
        {
            int index;
            if (!optionValuesToIndex.TryGetValue(optionValue, out index))
            {
                index = -1;
            }
            return index;
        }

        public override PawnTreeParameter CreateTreeParameter()
        {
            return new PawnTreeParameterDropDown { Template = this };
        }
    }
    
    public class PawnTemplateParameterSlider : PawnTemplateParameter
    {
        public Converter ValueConverter { get; set; }
        public int Minimum { get; set; }
        public int Maximum { get; set; }
        public int UIMinimum
        {
            get
            {
                return Math.Min(ValueConverter.ConvertValueToUI(Minimum),
                                ValueConverter.ConvertValueToUI(Maximum));
            }
        }
        public int UIMaximum
        {
            get
            {
                return Math.Max(ValueConverter.ConvertValueToUI(Minimum),
                                ValueConverter.ConvertValueToUI(Maximum));
            }
        }

        public override PawnTreeParameter CreateTreeParameter()
        {
            return new PawnTreeParameterSlider { Template = this };
        }

        public class Converter
        {
            public virtual int ConvertUIToValue(int uiValue) { return uiValue; }
            public virtual int ConvertValueToUI(int value) { return value; }
        }

        public class ConverterPlusOne : Converter
        {
            public override int ConvertUIToValue(int uiValue)
            {
                return uiValue - 1;
            }

            public override int ConvertValueToUI(int value)
            {
                return value + 1;
            }
        }

        public class ConverterXOffset : Converter
        {
            public override int ConvertUIToValue(int uiValue)
            {
                return 5 - uiValue;
            }

            public override int ConvertValueToUI(int value)
            {
                return 5 - value;
            }
        }

        public static Converter CreateConverterFromKey(string key)
        {
            switch (key)
            {
                case "plusOne": return new ConverterPlusOne();
                case "xOffset": return new ConverterXOffset();
                default: return new Converter();
            }
        }
    }
}
