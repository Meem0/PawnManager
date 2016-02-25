using PawnManager.TreeList;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections;

namespace PawnManager
{
    [Serializable()]
    public class Pawn : INotifyPropertyChanged, ITreeModel
    {
        public Pawn() { }
        public Pawn(Pawn other)
        {
            Initialize(new PawnCategory(other.root));
        }

        public void Initialize(PawnCategory rootCategory)
        {
            root = rootCategory;

            parameterDict = new Dictionary<string, PawnParameter>();
            foreach (PawnParameter parameter in root.DescendantParameters())
            {
                if (nameParameter == null && parameter is PawnParameterName)
                {
                    nameParameter = (PawnParameterName)parameter;
                    NotifyPropertyChanged("Name");
                }
                parameterDict.Add(parameter.Key, parameter);
            }
            NotifyPropertyChanged("Root");
        }

        public PawnParameter GetParameter(string key)
        {
            PawnParameter ret = null;
            parameterDict.TryGetValue(key, out ret);
            return ret;
        }
        
        public string Name
        {
            get
            {
                if (nameParameter != null)
                {
                    return nameParameter.Value as string;
                }
                return null;
            }
        }

        private PawnCategory root;
        public PawnCategory Root
        {
            get { return root; }
        }

        private Dictionary<string, PawnParameter> parameterDict;

        private PawnParameterName nameParameter = null;

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public IEnumerable GetChildren(object parent)
        {
            if (parent == null)
            {
                return Root.Children;
            }
            else
            {
                PawnCategory parentCategory = parent as PawnCategory;
                if (parentCategory != null)
                {
                    return parentCategory.Children;
                }
                else return null;
            }
        }

        public bool HasChildren(object parent)
        {
            PawnCategory parentCategory = parent as PawnCategory;
            return parentCategory != null && parentCategory.Children.Count > 0;
        }
    }
}
