using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Xml;
using System.Xml.Linq;

namespace PawnManager
{
    public class PawnParser
    {
        public void SetConfig(XElement config)
        {
            ParseTemplatePawn(config);
            ParseSavConfig(config);
        }

        public void ExportPawnToSav(Pawn pawn, XElement savPawnXElement)
        {
            savConfigRootClass.LoadPawnToSav(pawn, savPawnXElement);
        }

        public Pawn LoadPawnFromSav(XElement savPawnXElement)
        {
            Pawn loadPawn = new Pawn(templatePawn);

            savConfigRootClass.LoadSavToPawn(loadPawn, savPawnXElement);

            return loadPawn;
        }
        
        private Pawn templatePawn = null;
        
        private SavConfigClass savConfigRootClass = null;

        private abstract class SavConfigElement
        {
            public string Name { get; set; }

            public abstract void LoadPawnToSav(Pawn pawn, XElement xElement);
            public abstract void LoadSavToPawn(Pawn pawn, XElement xElement);
        }

        private class SavConfigClass : SavConfigElement
        {
            public List<SavConfigElement> Children { get; set; }

            public override void LoadPawnToSav(Pawn pawn, XElement xElement)
            {
                int i = 0;
                foreach (XElement childXElement in xElement.Elements())
                {
                    if (i == Children.Count)
                    {
                        return;
                    }

                    if (childXElement.GetNameAttribute() == Children[i].Name)
                    {
                        Children[i].LoadPawnToSav(pawn, childXElement);
                        ++i;
                    }
                }
            }

            public override void LoadSavToPawn(Pawn pawn, XElement xElement)
            {
                int i = 0;
                foreach (XElement childXElement in xElement.Elements())
                {
                    if (i == Children.Count)
                    {
                        return;
                    }

                    if (childXElement.GetNameAttribute() == Children[i].Name)
                    {
                        Children[i].LoadSavToPawn(pawn, childXElement);
                        ++i;
                    }
                }
            }
        }

        private class SavConfigParameter : SavConfigElement
        {
            public string Key { get; set; }

            public override void LoadPawnToSav(Pawn pawn, XElement xElement)
            {
                PawnParameter pawnParameter = pawn.GetParameter(Key);
                if (pawnParameter != null)
                {
                    pawnParameter.ExportValueToSav(xElement);
                }
            }

            public override void LoadSavToPawn(Pawn pawn, XElement xElement)
            {
                PawnParameter pawnParameter = pawn.GetParameter(Key);
                if (pawnParameter != null)
                {
                    pawnParameter.SetValueFromSav(xElement);
                }
            }
        }

        private class SavConfigParameterArray : SavConfigElement
        {
            public List<string> Keys { get; set; }

            public override void LoadPawnToSav(Pawn pawn, XElement xElement)
            {
                int i = 0;
                foreach (XElement childElement in xElement.Elements())
                {
                    if (Keys[i].Length > 0)
                    {
                        PawnParameter pawnParameter = pawn.GetParameter(Keys[i]);
                        if (pawnParameter != null)
                        {
                            pawnParameter.ExportValueToSav(childElement);
                        }
                    }
                    ++i;
                }
            }

            public override void LoadSavToPawn(Pawn pawn, XElement xElement)
            {
                int i = 0;
                foreach (XElement childElement in xElement.Elements())
                {
                    if (Keys[i].Length > 0)
                    {
                        PawnParameter pawnParameter = pawn.GetParameter(Keys[i]);
                        if (pawnParameter != null)
                        {
                            pawnParameter.SetValueFromSav(childElement);
                        }
                    }
                    ++i;
                }
            }
        }

        #region parsing implementation

        #region common

        private const string ElementNameKey = "key";
        
        private string GetElementKey(XElement xElement)
        {
            XElement keyElement = xElement.Element(ElementNameKey);
            return keyElement == null ? "" : keyElement.Value;
        }

        #endregion
        
        #region edit

        #region element names

        private const string ElementNameEditParseTreeRoot = "edit";
        private const string ElementNameEditParameter = "parameter";
        private const string ElementNameEditParameterContainer = "category";
        private const string ElementNameEditLabel = "label";
        private const string ElementNameEditTypeString = "string";
        private const string ElementNameEditTypeDropDown = "options";
        private const string ElementNameEditTypeDropDownOption = "option";
        private const string ElementNameEditTypeDropDownOptionValue = "value";
        private const string ElementNameEditTypeSlider = "slider";
        private const string ElementNameEditTypeSliderMin = "min";
        private const string ElementNameEditTypeSliderMax = "max";

        #endregion

        private void ParseTemplatePawn(XElement config)
        {
            XElement editTreeXml = config.Element(ElementNameEditParseTreeRoot);
            if (editTreeXml == null)
            {
                throw new XmlException(
                    string.Format("Config file is missing element: {0}", ElementNameEditParseTreeRoot));
            }

            templatePawn = new Pawn();
            templatePawn.Initialize(ParseEditCategory(editTreeXml));
        }

        private PawnCategory ParseEditCategory(XElement xElement)
        {
            ObservableCollection<PawnElement> children = new ObservableCollection<PawnElement>();

            foreach (XElement child in xElement.Elements())
            {
                if (child.Name == ElementNameEditParameterContainer)
                {
                    children.Add(ParseEditCategory(child));
                }
                else if (child.Name == ElementNameEditParameter)
                {
                    children.Add(ParseEditParameter(child));
                }
            }

            return new PawnCategory
            {
                Label = GetEditElementLabel(xElement),
                Children = children
            };
        }

        private PawnParameter ParseEditParameter(XElement xElement)
        {
            PawnParameter editParameter = null;

            foreach (XElement child in xElement.Elements())
            {
                if (child.Name == ElementNameEditTypeDropDown)
                {
                    editParameter = ParseEditElementDropDown(child);
                }
                else if (child.Name == ElementNameEditTypeSlider)
                {
                    editParameter = ParseEditElementSlider(child);
                }
                else if (child.Name == ElementNameEditTypeString)
                {
                    editParameter = new PawnParameterName();
                }
            }

            editParameter.Key = GetElementKey(xElement);
            editParameter.Label = GetEditElementLabel(xElement);

            return editParameter;
        }

        private PawnParameterDropDown ParseEditElementDropDown(XElement xElement)
        {
            PawnParameterDropDown dropDown = new PawnParameterDropDown
            {
                Options = new ObservableCollection<PawnParameterDropDown.Option>()
            };

            foreach (XElement optionElement in xElement.Elements())
            {
                if (optionElement.Name != ElementNameEditTypeDropDownOption)
                {
                    throw new XmlException(string.Format(
                        "{0} is not a valid child of {1}",
                        optionElement.Name,
                        ElementNameEditTypeDropDown));
                }
                PawnParameterDropDown.Option option = new PawnParameterDropDown.Option();
                option.Label = GetEditElementLabel(optionElement);
                try { option.Value = (int)optionElement.Element(ElementNameEditTypeDropDownOptionValue); }
                catch (Exception ex)
                {
                    throw new XmlException(string.Format(
                        "Invalid {0} element in edit config: {1}",
                        ElementNameEditTypeDropDownOption,
                        optionElement.ToString()),
                        ex);
                }
                dropDown.Options.Add(option);
            }

            return dropDown;
        }

        private PawnParameterSlider ParseEditElementSlider(XElement xElement)
        {
            try
            {
                return new PawnParameterSlider
                {
                    Minimum = (int)xElement.Element(ElementNameEditTypeSliderMin),
                    Maximum = (int)xElement.Element(ElementNameEditTypeSliderMax)
                };
            }
            catch (Exception ex)
            {
                throw new XmlException(string.Format(
                    "Invalid {0} element in config: {0}",
                    ElementNameEditTypeSlider,
                    xElement.ToString()),
                    ex);
            }
        }

        private string GetEditElementLabel(XElement xElement)
        {
            XElement labelElement = xElement.Element(ElementNameEditLabel);
            return labelElement == null ? "" : labelElement.Value;
        }

        #endregion
        
        #region sav

        #region element names

        private const string ElementNameSavParseTreeRoot = "sav";
        private const string ElementNameSavParameter = "data";
        private const string ElementNameSavParameterArray = "array";
        private const string ElementNameSavParameterContainer = "class";
        private const string ElementNameSavElementName = "name";

        #endregion

        private void ParseSavConfig(XElement config)
        {
            XElement savTreeXml = config.Element(ElementNameSavParseTreeRoot);
            if (savTreeXml == null)
            {
                throw new XmlException("Config file is missing sav parse tree.");
            }

            savConfigRootClass = ParseSavClassElement(savTreeXml);
        }
        
        private SavConfigClass ParseSavClassElement(XElement xElement)
        {
            List<SavConfigElement> children = new List<SavConfigElement>();

            foreach (XElement child in xElement.Elements())
            {
                SavConfigElement parsedElement = null;

                if (child.Name == ElementNameSavParameterContainer)
                {
                    parsedElement = ParseSavClassElement(child);
                }
                else if (child.Name == ElementNameSavParameter)
                {
                    parsedElement = ParseSavDataElement(child);
                }
                else if (child.Name == ElementNameSavParameterArray)
                {
                    parsedElement = ParseSavArrayElement(child);
                }

                if (parsedElement != null)
                {
                    children.Add(parsedElement);
                }
            }

            return new SavConfigClass
            {
                Name = GetSavElementName(xElement),
                Children = children
            };
        }

        private SavConfigParameter ParseSavDataElement(XElement xElement)
        {
            return new SavConfigParameter
            {
                Name = GetSavElementName(xElement),
                Key = GetElementKey(xElement)
            };
        }

        private SavConfigParameterArray ParseSavArrayElement(XElement xElement)
        {
            SavConfigParameterArray ret = new SavConfigParameterArray()
            {
                Name = GetSavElementName(xElement),
                Keys = new List<string>()
            };
            foreach (XElement keyElement in xElement.Elements(ElementNameKey))
            {
                ret.Keys.Add(keyElement.Value);
            }
            return ret;
        }

        private string GetSavElementName(XElement xElement)
        {
            XElement nameElement = xElement.Element(ElementNameSavElementName);
            return nameElement == null ? "" : nameElement.Value;
        }

        #endregion
        
        #endregion
    }
}
