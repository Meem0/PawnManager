using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Linq;

namespace PawnManager
{
    [Serializable()]
    public abstract class PawnElement : INotifyPropertyChanged
    {
        protected PawnElement() { }
        protected PawnElement(PawnElement other)
        {
            Label = string.Copy(other.Label);
        }

        public abstract PawnElement Copy();

        public abstract IEnumerable<PawnParameter> DescendantParameters();

        private string label = "";

        public string Label
        {
            get { return label; }
            set { label = value; }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    [Serializable()]
    public class PawnCategory : PawnElement, IEnumerable, IEnumerable<PawnElement>
    {
        public PawnCategory() { }
        public PawnCategory(PawnCategory other) : base(other)
        {
            Children = new ObservableCollection<PawnElement>();
            foreach (PawnElement element in other.Children)
            {
                Children.Add(element.Copy());
            }
            NotifyPropertyChanged("Children");
        }

        public override PawnElement Copy()
        {
            return new PawnCategory(this);
        }

        public override IEnumerable<PawnParameter> DescendantParameters()
        {
            foreach (PawnElement child in Children)
            {
                foreach (PawnParameter descendantParameter in child.DescendantParameters())
                {
                    yield return descendantParameter;
                }
            }
        }

        private ObservableCollection<PawnElement> children;
        public ObservableCollection<PawnElement> Children
        {
            get { return children; }
            set
            {
                children = value;
                NotifyPropertyChanged();
            }
        }

        public IEnumerator GetEnumerator()
        {
            return Children.GetEnumerator();
        }

        IEnumerator<PawnElement> IEnumerable<PawnElement>.GetEnumerator()
        {
            return Children.GetEnumerator();
        }
    }

    [Serializable()]
    public abstract class PawnParameter : PawnElement
    {
        public PawnParameter() { }
        public PawnParameter(PawnParameter other) : base(other)
        {
            Key = string.Copy(other.Key);
        }

        public abstract void ExportValueToSav(XElement xElement);
        public abstract void SetValueFromSav(XElement xElement);

        public override IEnumerable<PawnParameter> DescendantParameters()
        {
            yield return this;
        }

        private string key = "";
        public string Key
        {
            get { return key; }
            set { key = value; }
        }

        private object value = null;
        public object Value
        {
            get { return value; }
            set
            {
                this.value = ApplyValueConstraints(value);
                NotifyPropertyChanged();
            }
        }
        
        protected virtual object ApplyValueConstraints(object value)
        {
            return value;
        }
    }
    
    public class PawnParameterName : PawnParameter
    {
        public PawnParameterName()
        {
            Value = "";
        }
        public PawnParameterName(PawnParameterName other) : base(other)
        {
            Value = string.Copy((string)other.Value);
        }

        public override PawnElement Copy()
        {
            return new PawnParameterName(this);
        }

        private const int maxNameLength = 25;
        public int MaxNameLength { get { return maxNameLength; } }

        protected override object ApplyValueConstraints(object value)
        {
            string str = value as string;
            if (str == null)
            {
                return "";
            }
            return str.Substring(0, Math.Min(str.Length, MaxNameLength));
        }

        public override void ExportValueToSav(XElement xElement)
        {
            string name = (string)Value;
            int letterIndex = 0;
            foreach (XElement letterElement in xElement.Elements())
            {
                XAttribute letterAttribute = letterElement.GetValueAttribute();
                if (letterIndex < name.Length)
                {
                    letterAttribute.Value = ((int)name[letterIndex]).ToString();
                    ++letterIndex;
                }
                else if (letterAttribute.Value == "0")
                {
                    break;
                }
                else
                {
                    letterAttribute.Value = "0";
                }
            }
        }

        public override void SetValueFromSav(XElement xElement)
        {
            StringBuilder sb = new StringBuilder(MaxNameLength);
            try
            {
                foreach (XElement letterElement in xElement.Elements())
                {
                    int value = letterElement.GetParsedValueAttribute();
                    if (value == 0)
                    {
                        break;
                    }
                    sb.Append((char)value);
                }
                Value = sb.ToString();
            }
            catch (Exception ex)
            {
                throw new System.Xml.XmlException(".sav file contained an invalid Pawn name.", ex);
            }
        }
    }

    [Serializable()]
    public class PawnParameterDropDown : PawnParameter
    {
        public PawnParameterDropDown()
        {
            Value = 0;
        }
        public PawnParameterDropDown(PawnParameterDropDown other) : base(other)
        {
            Value = other.Value;
            // no need for deep copy since the options for a given drop down are the same for all Pawns
            Options = other.Options;
        }

        public override PawnElement Copy()
        {
            return new PawnParameterDropDown(this);
        }

        protected override object ApplyValueConstraints(object value)
        {
            if (!(value is int))
            {
                return 0;
            }
            return Extensions.Clamp((int)value, 0, Options.Count - 1);
        }

        public override void ExportValueToSav(XElement xElement)
        {
            int index = (int)Value;
            if (index < 0 || Options.Count <= index)
            {
                throw new Exception(string.Format(
                    "Parameter {0} has invalid value: {1}",
                    Label,
                    index));
            }
            xElement.GetValueAttribute().Value = Options[index].ToString();
        }

        public override void SetValueFromSav(XElement xElement)
        {
            // linear search for the Option with the value from the XML
            // start a bit before the index equal to the value, to optimize
            // cases where the Options have values {1, 2, 3, ... }
            int optionValue = xElement.GetParsedValueAttribute();
            int searchIndex = Math.Max(0, optionValue - 2);
            int numSearched = 0;
            while (numSearched < Options.Count)
            {
                if (Options[searchIndex].Value == optionValue)
                {
                    Value = searchIndex;
                    return;
                }
                searchIndex = (searchIndex + 1) % Options.Count;
                ++numSearched;
            }

            throw new System.Xml.XmlException(string.Format(
                "Parameter {0} does not have an option with value: {1}",
                Label,
                optionValue));
        }

        [Serializable()]
        public class Option : INotifyPropertyChanged
        {
            private string label;
            public string Label
            {
                get { return label; }
                set
                {
                    label = value;
                    NotifyPropertyChanged();
                }
            }

            private int value;
            public int Value
            {
                get { return value; }
                set
                {
                    this.value = value;
                    NotifyPropertyChanged();
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;
            private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                }
            }
        }

        private ObservableCollection<Option> options = new ObservableCollection<Option>();
        public ObservableCollection<Option> Options
        {
            get { return options; }
            set
            {
                options = value;
                NotifyPropertyChanged();
            }
        }
    }

    [Serializable()]
    public class PawnParameterSlider : PawnParameter
    {
        public PawnParameterSlider()
        {
            Value = 0;
        }
        public PawnParameterSlider(PawnParameterSlider other) : base(other)
        {
            Value = other.Value;
            Minimum = other.Minimum;
            Maximum = other.Maximum;
        }

        public override PawnElement Copy()
        {
            return new PawnParameterSlider(this);
        }

        protected override object ApplyValueConstraints(object value)
        {
            int num = 0;

            if (value is int)
            {
                num = (int)value;
            }
            else
            {
                string numStr = value as string;
                if (numStr == null || !int.TryParse(numStr, out num))
                {
                    return Minimum;
                }
            }

            return Extensions.Clamp(num, Minimum, Maximum);
        }

        public override void ExportValueToSav(XElement xElement)
        {
            xElement.GetValueAttribute().Value = ((int)Value).ToString();
        }

        public override void SetValueFromSav(XElement xElement)
        {
            Value = xElement.GetParsedValueAttribute();
        }

        private int minimum;
        public int Minimum
        {
            get { return minimum; }
            set { minimum = value; }
        }

        private int maximum;
        public int Maximum
        {
            get { return maximum; }
            set { maximum = value; }
        }
    }
}
