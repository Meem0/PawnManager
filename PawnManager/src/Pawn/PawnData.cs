using System.Collections.Generic;

namespace PawnManager
{
    public class PawnParameter
    {
        public object Value { get; set; }
        public bool FormatAsFloat { get; set; } = false;
    }

    public class PawnData
    {
        public Dictionary<string, PawnParameter> ParameterDict { get; set; }
         = new Dictionary<string, PawnParameter>();

        public PawnParameter GetParameter(string key)
        {
            PawnParameter ret = null;
            ParameterDict.TryGetValue(key, out ret);
            return ret;
        }

        public PawnParameter GetOrAddParameter(string key)
        {
            PawnParameter ret = null;
            if (!ParameterDict.TryGetValue(key, out ret))
            {
                ret = new PawnParameter();
                ParameterDict.Add(key, ret);
            }
            return ret;
        }
    }
}
