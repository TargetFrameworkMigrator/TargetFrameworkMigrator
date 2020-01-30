using System.ComponentModel;
using System.Runtime.CompilerServices;
using EnvDTE;

namespace VHQLabs.TargetFrameworkMigrator
{
    public class ProjectModel : INotifyPropertyChanged
    {
        private bool isSelected;
        private string name;

        public bool IsSelected
        {
            get
            {
                return isSelected;
            }
            set
            {
                isSelected = value;
                OnPropertyChanged();
            }
        }

        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                name = value;
                OnPropertyChanged();
            }
        }

        public FrameworkModel Framework
        {
            get; set;
        }

        public bool HasFramework
        {
            get
            {
                return Framework != null;
            }
        }

        public Project DteProject
        {
            get; set;
        }

        public override string ToString()
        {
            return string.Format("{0}", Name);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}