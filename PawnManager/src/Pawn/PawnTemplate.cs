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
            int index = -1;
            optionValuesToIndex.TryGetValue(optionValue, out index);
            return index;
        }

        public override PawnTreeParameter CreateTreeParameter()
        {
            return new PawnTreeParameterDropDown { Template = this };
        }
    }
    
    public class PawnTemplateParameterSlider : PawnTemplateParameter
    {
        public int Minimum { get; set; }
        public int Maximum { get; set; }

        public override PawnTreeParameter CreateTreeParameter()
        {
            return new PawnTreeParameterSlider { Template = this };
        }
    }
}
