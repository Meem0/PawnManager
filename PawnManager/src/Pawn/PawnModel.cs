using PawnManager.TreeList;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections;

namespace PawnManager
{
    public class PawnModel : ITreeModel, INotifyPropertyChanged
    {
        public PawnModel(PawnTemplateCategory templatePawnRoot)
        {
            this.templatePawnRoot = templatePawnRoot;
        }
        
        private PawnTemplateCategory templatePawnRoot;
        private PawnTreeCategory loadedPawnTreeRoot;
        private PawnData loadedPawnData;
        public PawnData LoadedPawn
        {
            get { return loadedPawnData; }
            set
            {
                SetLoadedPawn(value);
                NotifyPropertyChanged();
                NotifyPropertyChanged("IsPawnLoaded");
                NotifyPropertyChanged("Name");
            }
        }

        private void SetLoadedPawn(PawnData pawnData)
        {
            loadedPawnData = pawnData;
            nameParameter = null;
            loadedPawnTreeRoot = CreatePawnTreeCategory(templatePawnRoot);
        }

        private PawnTreeCategory CreatePawnTreeCategory(PawnTemplateCategory templateCategory)
        {
            PawnTreeCategory ret = new PawnTreeCategory { Template = templateCategory };
            foreach (PawnTemplateElement childTemplateElement in templateCategory.Children)
            {
                PawnTreeElement childTreeElement = null;

                PawnTemplateParameter childTemplateParameter = childTemplateElement as PawnTemplateParameter;
                if (childTemplateParameter != null)
                {
                    childTreeElement = CreatePawnTreeParameter(childTemplateParameter);
                }
                else
                {
                    PawnTemplateCategory childTemplateCategory = childTemplateElement as PawnTemplateCategory;
                    childTreeElement = CreatePawnTreeCategory(childTemplateCategory);
                }
                
                ret.Children.Add(childTreeElement);
            }
            return ret;
        }

        private PawnTreeParameter CreatePawnTreeParameter(PawnTemplateParameter templateParameter)
        {
            PawnTreeParameter ret = templateParameter.CreateTreeParameter();
            ret.@PawnParameter = loadedPawnData.GetOrAddParameter(templateParameter.Key);

            if (nameParameter == null)
            {
                nameParameter = ret as PawnTreeParameterName;
            }

            return ret;
        }

        public bool IsPawnLoaded
        {
            get { return LoadedPawn != null; }
        }

        private PawnTreeParameterName nameParameter = null;
        public string Name
        {
            get
            {
                if (nameParameter != null)
                {
                    return nameParameter.Name;
                }
                return "";
            }
        }

        public IEnumerable GetChildren(object parent)
        {
            PawnTreeCategory parentCategory = parent == null ? loadedPawnTreeRoot : parent as PawnTreeCategory;
            if (parentCategory != null)
            {
                return parentCategory.Children;
            }
            return null;
        }

        public bool HasChildren(object parent)
        {
            PawnTreeCategory parentCategory = parent as PawnTreeCategory;
            return parentCategory != null && parentCategory.Children.Count > 0;
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
}
