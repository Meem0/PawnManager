using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PawnManager
{
    public abstract class PawnTreeElement : INotifyPropertyChanged
    {
        public abstract string Label { get; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    public class PawnTreeCategory : PawnTreeElement
    {
        public override string Label
        {
            get
            {
                return Template.Label;
            }
        }

        public ObservableCollection<PawnTreeElement> Children { get; set; }
         = new ObservableCollection<PawnTreeElement>();

        public PawnTemplateCategory Template { get; set; }
    }

    public abstract class PawnTreeParameter : PawnTreeElement
    {
        /// <summary>
        /// A reference to one of the values in the Pawn data dictionary
        /// </summary>
        public PawnParameter @PawnParameter { get; set; }

        /// <summary>
        /// The value bound to the UI tree
        /// </summary>
        public object Value
        {
            get { return GetValueForUI(); }
            set
            {
                SetValueFromUI(value);
                NotifyPropertyChanged();
            }
        }

        protected virtual object GetValueForUI()
        {
            return PawnParameter.Value;
        }

        protected virtual void SetValueFromUI(object value)
        {
            PawnParameter.Value = value;
        }
    }

    public class PawnTreeParameterDropDown : PawnTreeParameter
    {
        public PawnTemplateParameterDropDown Template { get; set; }

        public override string Label
        {
            get
            {
                return Template.Label;
            }
        }

        public int ParameterValue
        {
            get { return (int)PawnParameter.Value; }
            set
            {
                if (Template.AllowCustom || Template.GetOptionIndexFromValue(value) >= 0)
                {
                    PawnParameter.Value = value;
                    NotifyPropertyChanged("Value");
                }
            }
        }

        protected override object GetValueForUI()
        {
            int index = Template.GetOptionIndexFromValue((int)PawnParameter.Value);
            return index < 0 ? 0 : index;
        }

        protected override void SetValueFromUI(object value)
        {
            if (Template.Options.Count == 0)
            {
                return;
            }

            if (value is int)
            {
                int index = (int)value;

                if (Template.AllowCustom && index == 0)
                {
                    return;
                }

                index = Extensions.Clamp(index, 0, Template.Options.Count - 1);

                PawnParameter.Value = Template.Options[index].Value;
                NotifyPropertyChanged("ParameterValue");
            }
        }
    }
    
    public class PawnTreeParameterName : PawnTreeParameter
    {
        public PawnTemplateParameterName Template { get; set; }

        public override string Label
        {
            get
            {
                return Template.Label;
            }
        }

        protected override void SetValueFromUI(object value)
        {
            string str = value as string;
            if (str == null)
            {
                PawnParameter.Value = "";
            }
            else
            {
                PawnParameter.Value = str.Substring(0, Math.Min(str.Length, Template.MaxNameLength));
            }
        }
    }

    public class PawnTreeParameterSlider : PawnTreeParameter
    {
        public PawnTemplateParameterSlider Template { get; set; }

        public override string Label
        {
            get
            {
                return Template.Label;
            }
        }
        
        protected override void SetValueFromUI(object value)
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
                    num = Template.Minimum;
                }
            }

            PawnParameter.Value = Extensions.Clamp(num, Template.Minimum, Template.Maximum);
        }
    }
}
