using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Xml.Linq;

namespace PawnManager
{
    public abstract class PawnParameter
    {
        /// <summary>
        /// The user-facing label that describes the parameter
        /// </summary>
        public string Label { get; set; } = "Label";
        
        /// <summary>
        /// The internal key used by the config file and Pawn files
        /// </summary>
        public string Key { get; set; } = "";
    }

    public class PawnParameterClass : PawnParameter
    {
        private ObservableCollection<PawnParameter> children;
        public ObservableCollection<PawnParameter> Children
        {
            get { return children; }
            set { children = (ObservableCollection<PawnParameter>)value; }
        }
    }

    public class PawnParameterName : PawnParameter
    {
        private string name = "";
        /// <summary>
        /// The Pawn's name
        /// </summary>
        public string Name
        {
            get { return name; }
            set { this.name = (string)value; }
        }
        
        private const int maxNameLength = 25;
        public int MaxNameLength
        {
            get { return maxNameLength; }
        }
    }

    public class PawnParameterRange : PawnParameter
    {
        private int value = 0;
        public int Value
        {
            get { return value; }
            set { this.value = (int)value; }
        }

        public int Minimum { get; set; }
        public int Maximum { get; set; }
    }

    public class Pawn : IPawn
    {
        public string Name { get; }

        public PawnParameterClass RootClass { get; set; }

        public XElement EditClass { get; set; }
    }
}
