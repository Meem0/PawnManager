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
        public virtual PawnParameter @PawnParameter { get; set; }
    }

    public class PawnTreeParameterDropDown : PawnTreeParameter
    {
        public PawnTemplateParameterDropDown Template { get; set; }

        public override PawnParameter PawnParameter
        {
            get
            {
                return base.PawnParameter;
            }
            set
            {
                value.Value = value.Value.ToInt();
                base.PawnParameter = value;
            }
        }

        public override string Label
        {
            get
            {
                return Template.Label;
            }
        }

        /// <summary>
        /// Gets or sets the index of the currently selected drop-down option.
        /// For use by the UI.
        /// </summary>
        public int DropDownIndex
        {
            get
            {
                int index = Template.GetOptionIndexFromValue(PawnParameter.Value.ToInt());
                return index < 0 ? 0 : index;
            }
            set
            {
                // When allowing custom values, the Option with index 0 is "Custom".
                // We don't want that to be selectable.
                if (Template.AllowCustom && value == 0)
                {
                    return;
                }

                value = Extensions.Clamp(value, 0, Template.Options.Count - 1);

                PawnParameter.Value = Template.Options[value].Value;

                NotifyPropertyChanged();
                NotifyPropertyChanged("TextBoxValue");
            }
        }

        /// <summary>
        /// The value displayed in the textbox.
        /// This is the actual value used by the .sav file,
        /// so that users can research values that don't have drop-down options yet.
        /// </summary>
        public string TextBoxValue
        {
            get { return PawnParameter.Value.ToInt().ToString(); }
            set
            {
                int num;
                if (!int.TryParse(value, out num))
                {
                    return;
                }

                if (!Template.AllowCustom)
                {
                    num = Extensions.Clamp(num, 0, Template.Options.Count - 1);
                }
                PawnParameter.Value = num;

                NotifyPropertyChanged();
                NotifyPropertyChanged("DropDownIndex");
            }
        }
    }

    public class PawnTreeParameterHex : PawnTreeParameter
    {
        public PawnTemplateParameterHex Template { get; set; }

        public override string Label
        {
            get
            {
                return Template.Label;
            }
        }

        public string HexValue
        {
            get
            {
                uint num = 0;
                try
                {
                    num = Convert.ToUInt32(PawnParameter.Value);
                }
                catch { }
                return num.ToString("X8");
            }
            set
            {
                try
                {
                    PawnParameter.Value = uint.Parse(value, System.Globalization.NumberStyles.HexNumber);
                }
                catch { }
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
        
        public string Name
        {
            get { return PawnParameter.Value as string; }
            set
            {
                if (value == null)
                {
                    value = "";
                }
                PawnParameter.Value = value.Substring(0, Math.Min(value.Length, Template.MaxNameLength));
                NotifyPropertyChanged();
            }
        }
    }

    public class PawnTreeParameterSlider : PawnTreeParameter
    {
        public PawnTemplateParameterSlider Template { get; set; }

        public override PawnParameter PawnParameter
        {
            get
            {
                return base.PawnParameter;
            }
            set
            {
                value.Value = value.Value.ToInt();
                base.PawnParameter = value;
            }
        }

        public override string Label
        {
            get
            {
                return Template.Label;
            }
        }

        public int Value
        {
            get
            {
                return Template.ValueConverter.ConvertValueToUI(PawnParameter.Value.ToInt(Template.Minimum));
            }
            set
            {
                value = Template.ValueConverter.ConvertUIToValue(value);
                PawnParameter.Value = Extensions.Clamp(value, Template.Minimum, Template.Maximum);

                NotifyPropertyChanged();
                NotifyPropertyChanged("TextBoxValue");
            }
        }

        public string TextBoxValue
        {
            get { return Value.ToString(); }
            set
            {
                int num;
                if (!int.TryParse(value, out num))
                {
                    return;
                }
                Value = num;
            }
        }
    }
}
