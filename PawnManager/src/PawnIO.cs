using System;
using System.IO;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Xml;
using System.Text;

namespace PawnManager
{
    /// <summary>
    /// The three Pawns that follow the player
    /// </summary>
    public enum SavSlot
    {
        MainPawn,
        Pawn1,
        Pawn2
    }
    
    /// <summary>
    /// Contains all the logic that involves reading and writing Pawns to and from files
    /// and requires knowledge of the internals of Pawns.
    /// </summary>
    public static class PawnIO
    {
        /// <summary>
        /// Initializes the object using the given config.
        /// Throws an exception if parsing the config fails.
        /// </summary>
        /// <param name="config">
        /// Contains the instructions for parsing the .sav file,
        /// as well as information on how to display Pawn parameters to the user,
        /// such as labels, slider ranges, and drop-down options.
        /// </param>
        /// <returns>
        /// The root of the parsed Pawn template
        /// </returns>
        public static PawnTemplateCategory ParseConfig(XElement config)
        {
            PawnTemplateCategory template = ParsePawnTemplate(config);
            ParseSavConfig(config);
            return template;
        }

        #region Pawn file IO
        
        #region element names

        const string ElementNamePawnFileVersion = "version";
        const string ElementNamePawnFileRoot = "root";
        const string ElementNamePawnFileParameter = "parameter";
        const string ElementNamePawnFileKey = "key";
        const string ElementNamePawnFileValue = "value";

        #endregion

        /// <summary>
        /// Load a Pawn from a Pawn file
        /// </summary>
        /// <param name="pawnFilePath">The path to the Pawn file</param>
        /// <returns>The loaded Pawn</returns>
        public static PawnData LoadPawn(string filename)
        {
            PawnData loadPawn = null;
            try
            {
                XElement pawnFile = XElement.Load(filename);

                XAttribute versionAttribute = pawnFile.Attribute(ElementNamePawnFileVersion);
                if (versionAttribute != null && versionAttribute.Value == "1")
                {
                    loadPawn = LoadPawnVersion1(pawnFile);
                }
                else
                {
                    loadPawn = new PawnData();
                    foreach (XElement child in pawnFile.Elements())
                    {
                        loadPawn.ParameterDict.Add(
                            child.Element(ElementNamePawnFileKey).Value,
                            new PawnParameter
                            {
                                Value = child.Element(ElementNamePawnFileValue).Value
                            }
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                throw new XmlException(string.Format(
                    "{0} is not a valid Pawn file.",
                    filename),
                    ex);
            }

            return loadPawn;
        }

        private static PawnData LoadPawnVersion1(XElement pawnFile)
        {
            SavConfigClass appearanceConfig = null;
            foreach (SavConfigElement childElement in savConfigRootClass.Children)
            {
                SavConfigClass childClass = childElement as SavConfigClass;
                if (childClass != null && childClass.Name == "mEdit")
                {
                    appearanceConfig = childClass;
                    break;
                }
            }
            if (appearanceConfig == null)
            {
                return null;
            }
            PawnData loadPawn = new PawnData();
            appearanceConfig.LoadSavToPawn(loadPawn, pawnFile);
            return loadPawn;
        }

        /// <summary>
        /// Save a Pawn to a Pawn file
        /// </summary>
        /// <param name="pawn">The Pawn to save</param>
        public static void SavePawn(PawnData pawn, string filename)
        {
            XElement pawnFile = new XElement(ElementNamePawnFileRoot);
            pawnFile.SetAttributeValue(ElementNamePawnFileVersion, 2);
            foreach (KeyValuePair<string, PawnParameter> kvp in pawn.ParameterDict)
            {
                pawnFile.Add(
                    new XElement(ElementNamePawnFileParameter,
                        new XElement(ElementNamePawnFileKey, kvp.Key),
                        new XElement(ElementNamePawnFileValue, kvp.Value.Value)
                    )
                );
            }
            pawnFile.Save(filename);
        }

        #endregion

        #region sav IO

        /// <summary>
        /// The Pawn's name has special behaviour for importing and exporting to the .sav file
        /// </summary>
        private const string SpecialKeyName = "name";

        #region sav config classes

        private abstract class SavConfigElement
        {
            public string Name { get; set; }

            public abstract void LoadPawnToSav(PawnData pawn, XElement xElement);
            public abstract void LoadSavToPawn(PawnData pawn, XElement xElement);

            protected static void LoadParameterToSav(PawnParameter parameter, XElement xElement)
            {
                try
                {
                    xElement.GetValueAttribute().Value = (parameter.Value.ToInt()).ToString();
                }
                catch (Exception ex)
                {
                    throw new XmlException(string.Format(
                        "Error exporting parameter to .sav file element: {0}.",
                        xElement.ToString()),
                        ex);
                }
            }
        }

        private class SavConfigClass : SavConfigElement
        {
            public List<SavConfigElement> Children { get; set; }

            public override void LoadPawnToSav(PawnData pawn, XElement xElement)
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

            public override void LoadSavToPawn(PawnData pawn, XElement xElement)
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
            private string key;
            public string Key
            {
                get { return key; }
                set
                {
                    key = value;
                    if (key == SpecialKeyName)
                    {
                        isName = true;
                    }
                }
            }

            private bool isName = false;

            public override void LoadPawnToSav(PawnData pawn, XElement xElement)
            {
                if (isName)
                {
                    LoadPawnNameToSav(pawn, xElement);
                    return;
                }

                PawnParameter pawnParameter = pawn.GetParameter(Key);
                if (pawnParameter != null)
                {
                    LoadParameterToSav(pawnParameter, xElement);
                }
            }

            public override void LoadSavToPawn(PawnData pawn, XElement xElement)
            {
                if (isName)
                {
                    LoadSavNameToPawn(pawn, xElement);
                    return;
                }

                PawnParameter pawnParameter = pawn.GetOrAddParameter(Key);
                pawnParameter.Value = xElement.GetParsedValueAttribute();
            }

            private void LoadPawnNameToSav(PawnData pawn, XElement xElement)
            {
                string name = pawn.GetParameter(Key).Value as string;
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

            private void LoadSavNameToPawn(PawnData pawn, XElement xElement)
            {
                StringBuilder sb = new StringBuilder();
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
                    pawn.GetOrAddParameter(Key).Value = sb.ToString();
                }
                catch (Exception ex)
                {
                    throw new XmlException(".sav file contained an invalid Pawn name.", ex);
                }
            }
        }

        private class SavConfigParameterArray : SavConfigElement
        {
            public List<string> Keys { get; set; }

            public override void LoadPawnToSav(PawnData pawn, XElement xElement)
            {
                int i = 0;
                foreach (XElement childElement in xElement.Elements())
                {
                    if (Keys[i].Length > 0)
                    {
                        PawnParameter pawnParameter = pawn.GetParameter(Keys[i]);
                        if (pawnParameter != null)
                        {
                            LoadParameterToSav(pawnParameter, childElement);
                        }
                    }
                    ++i;
                }
            }

            public override void LoadSavToPawn(PawnData pawn, XElement xElement)
            {
                int i = 0;
                foreach (XElement childElement in xElement.Elements())
                {
                    if (Keys[i].Length > 0)
                    {
                        PawnParameter pawnParameter = pawn.GetOrAddParameter(Keys[i]);
                        if (pawnParameter != null)
                        {
                            pawnParameter.Value = childElement.GetParsedValueAttribute();
                        }
                    }
                    ++i;
                }
            }
        }

        #endregion

        private static SavConfigClass savConfigRootClass = null;

        /// <summary>
        /// Load a Pawn from the .sav file
        /// </summary>
        /// <param name="savSlot">The Pawn to load</param>
        /// <param name="savRoot">The .sav file</param>
        /// <returns>The loaded Pawn, or null if no Pawn was loaded</returns>
        public static PawnData LoadPawnSav(SavSlot savSlot, XElement savRoot)
        {
            XElement savPawnXElement = SavGetPawn(savRoot, savSlot);
            PawnData loadPawn = new PawnData();
            savConfigRootClass.LoadSavToPawn(loadPawn, savPawnXElement);
            return loadPawn;
        }

        /// <summary>
        /// Save a Pawn to the .sav file
        /// </summary>
        /// <param name="pawn">The Pawn to save</param>
        /// <param name="savSlot">The Pawn to save to</param>
        /// <param name="savRoot">The loaded .sav file</param>
        public static void SavePawnSav(PawnData pawn, SavSlot savSlot, XElement savRoot)
        {
            XElement savPawnXElement = SavGetPawn(savRoot, savSlot);
            savConfigRootClass.LoadPawnToSav(pawn, savPawnXElement);
        }

        private static XElement SavGetPawn(XElement savRoot, SavSlot savSlot)
        {
            XElement pawnClass = null;

            try
            {
                XElement pawnArray = savRoot.GetChildByName("mPlayerDataManual");
                pawnArray = pawnArray.GetChildByName("mPlCmcEditAndParam");
                pawnArray = pawnArray.GetChildByName("mCmc");

                int index = (int)savSlot;
                int currentIndex = 0;
                foreach (XElement child in pawnArray.Elements())
                {
                    if (currentIndex++ == index)
                    {
                        pawnClass = child;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Invalid save file.", ex);
            }

            return pawnClass;
        }

        #endregion

        #region config parsing
        
        private const string ElementNameKey = "key";
        
        private static string GetElementKey(XElement xElement)
        {
            XElement keyElement = xElement.Element(ElementNameKey);
            return keyElement == null ? "" : keyElement.Value;
        }

        #region Pawn

        #region element names

        private const string ElementNameOptionsDict = "optionsDict";
        private const string ElementNameEditParseTreeRoot = "edit";
        private const string ElementNameEditParameter = "parameter";
        private const string ElementNameEditParameterContainer = "category";
        private const string ElementNameEditLabel = "label";
        private const string ElementNameEditTypeString = "string";
        private const string ElementNameEditTypeDropDown = "options";
        private const string ElementNameEditTypeDropDownOption = "option";
        private const string ElementNameEditTypeDropDownOptionValue = "value";
        private const string ElementNameEditTypeDropDownDisableCustom = "disableCustom";
        private const string ElementNameEditTypeSlider = "slider";
        private const string ElementNameEditTypeSliderConverter = "converter";
        private const string ElementNameEditTypeSliderMin = "min";
        private const string ElementNameEditTypeSliderMax = "max";

        #endregion

        private static Dictionary<string, List<PawnTemplateParameterDropDown.Option>> OptionsDict
                 = new Dictionary<string, List<PawnTemplateParameterDropDown.Option>>();

        private static PawnTemplateCategory ParsePawnTemplate(XElement config)
        {
            XElement optionsDictXml = config.Element(ElementNameOptionsDict);
            if (optionsDictXml != null)
            {
                ParseOptionsDict(optionsDictXml);
            }

            XElement editTreeXml = config.Element(ElementNameEditParseTreeRoot);
            if (editTreeXml == null)
            {
                throw new XmlException(string.Format(
                    "Config file is missing element: {0}",
                    ElementNameEditParseTreeRoot));
            }
            
            return ParseEditCategory(editTreeXml);
        }

        private static PawnTemplateCategory ParseEditCategory(XElement xElement)
        {
            List<PawnTemplateElement> children = new List<PawnTemplateElement>();

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

            return new PawnTemplateCategory
            {
                Label = GetEditElementLabel(xElement),
                Children = children
            };
        }

        private static PawnTemplateParameter ParseEditParameter(XElement xElement)
        {
            PawnTemplateParameter editParameter = null;

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
                    editParameter = new PawnTemplateParameterName();
                }
            }

            editParameter.Key = GetElementKey(xElement);
            editParameter.Label = GetEditElementLabel(xElement);

            return editParameter;
        }

        private static PawnTemplateParameterDropDown ParseEditElementDropDown(XElement xElement)
        {
            PawnTemplateParameterDropDown dropDown = new PawnTemplateParameterDropDown();

            if (xElement.Parent.Element(ElementNameEditTypeDropDownDisableCustom) == null)
            {
                dropDown.AddOption(new PawnTemplateParameterDropDown.Option { Label = "Custom", Value = -2 });
                dropDown.AllowCustom = true;
            }
            
            XElement keyElement = xElement.Element(ElementNameKey);
            if (keyElement != null)
            {
                try
                {
                    List<PawnTemplateParameterDropDown.Option> optionsList = OptionsDict[keyElement.Value];
                    foreach (PawnTemplateParameterDropDown.Option option in optionsList)
                    {
                        dropDown.AddOption(option);
                    }
                }
                catch (Exception ex)
                {
                    throw new XmlException(string.Format(
                        "Invalid option key in config file: {0}.",
                        keyElement.Value),
                        ex);
                }
            }
            else
            {
                foreach (XElement optionElement in xElement.Elements())
                {
                    if (optionElement.Name != ElementNameEditTypeDropDownOption)
                    {
                        throw new XmlException(string.Format(
                            "{0} is not a valid child of {1}",
                            optionElement.Name,
                            ElementNameEditTypeDropDown));
                    }
                    PawnTemplateParameterDropDown.Option option = new PawnTemplateParameterDropDown.Option();
                    option.Label = GetEditElementLabel(optionElement);
                    try { option.Value = optionElement.Element(ElementNameEditTypeDropDownOptionValue).Value.ToInt(); }
                    catch (Exception ex)
                    {
                        throw new XmlException(string.Format(
                            "Invalid {0} element in edit config: {1}",
                            ElementNameEditTypeDropDownOption,
                            optionElement.ToString()),
                            ex);
                    }
                    dropDown.AddOption(option);
                }
            }

            return dropDown;
        }

        private static PawnTemplateParameterSlider ParseEditElementSlider(XElement xElement)
        {
            try
            {
                XElement converterElement = xElement.Element(ElementNameEditTypeSliderConverter);
                return new PawnTemplateParameterSlider
                {
                    Minimum = xElement.Element(ElementNameEditTypeSliderMin).Value.ToInt(),
                    Maximum = xElement.Element(ElementNameEditTypeSliderMax).Value.ToInt(),
                    ValueConverter = PawnTemplateParameterSlider.CreateConverterFromKey(converterElement == null ? "" : converterElement.Value)
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

        private static string GetEditElementLabel(XElement xElement)
        {
            XElement labelElement = xElement.Element(ElementNameEditLabel);
            return labelElement == null ? "" : labelElement.Value;
        }

        private static void ParseOptionsDict(XElement xElement)
        {
            try
            {
                foreach (XElement optionListElement in xElement.Elements(ElementNameEditTypeDropDown))
                {
                    List<PawnTemplateParameterDropDown.Option> optionsList
                        = new List<PawnTemplateParameterDropDown.Option>();
                    foreach (XElement optionElement in optionListElement.Elements(ElementNameEditTypeDropDownOption))
                    {
                        optionsList.Add(new PawnTemplateParameterDropDown.Option
                        {
                            Label = optionElement.Element(ElementNameEditLabel).Value,
                            Value = int.Parse(optionElement.Element(ElementNamePawnFileValue).Value)
                        });
                    }

                    OptionsDict.Add(optionListElement.Element(ElementNameKey).Value, optionsList);
                }
            }
            catch (Exception ex)
            {
                throw new XmlException("Config file contains invalid options dictionary.", ex);
            }
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

        private static void ParseSavConfig(XElement config)
        {
            XElement savTreeXml = config.Element(ElementNameSavParseTreeRoot);
            if (savTreeXml == null)
            {
                throw new XmlException("Config file is missing sav parse tree.");
            }

            savConfigRootClass = ParseSavClassElement(savTreeXml);
        }
        
        private static SavConfigClass ParseSavClassElement(XElement xElement)
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

        private static SavConfigParameter ParseSavDataElement(XElement xElement)
        {
            return new SavConfigParameter
            {
                Name = GetSavElementName(xElement),
                Key = GetElementKey(xElement)
            };
        }

        private static SavConfigParameterArray ParseSavArrayElement(XElement xElement)
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

        private static string GetSavElementName(XElement xElement)
        {
            XElement nameElement = xElement.Element(ElementNameSavElementName);
            return nameElement == null ? "" : nameElement.Value;
        }

        #endregion
        
        #endregion
    }
}
