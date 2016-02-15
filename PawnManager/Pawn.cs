using System;
using System.Collections;
using System.Collections.Generic;

namespace PawnManager
{
    public abstract class PawnParameter
    {
        /// <summary>
        /// The user-facing label that describes the parameter
        /// </summary>
        public string Label { get; set; } = "Label";

        /// <summary>
        /// The value that the user can edit
        /// </summary>
        public abstract object Value { get; }

        /// <summary>
        /// The internal key used by the config file and Pawn files
        /// </summary>
        public string Key { get; set; } = "";
    }

    public class PawnParameterName : PawnParameter
    {
        public override object Value { get { return Name; } }

        /// <summary>
        /// The Pawn's name
        /// </summary>
        public string Name { get; set; }

    }

    public class Pawn : IPawn
    {
        public string Name { get; }

        private List<PawnParameter> parameters;
    }
}
