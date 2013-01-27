using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using EnvDTE;
using VSChangeTargetFrameworkExtension.Annotations;

namespace VSChangeTargetFrameworkExtension
{
    public partial class ProjectsUpdateList : Form
    {
        public ProjectsUpdateList()
        {
            InitializeComponent();
            dataGridView1.AutoGenerateColumns = false;
            comboBox1.DropDownStyle = ComboBoxStyle.DropDownList;
        }

        public List<FrameworkModel> Frameworks { set { comboBox1.DataSource = value; } }
        public List<ProjectModel> Projects
        {
            set { dataGridView1.DataSource = value; }
            get { return (List<ProjectModel>) dataGridView1.DataSource; }
        }

        public FrameworkModel SelectedFramework
        {
            get { return (FrameworkModel) comboBox1.SelectedItem; }
        }

        public string State { set { label1.Text = value; } }

        public event Action UpdateFired;

        private async void button3_Click(object sender, EventArgs e)
        {
            var onUpdate = UpdateFired;
            if(onUpdate != null)
                onUpdate.Invoke();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            foreach (var projectModel in Projects)
            {
                projectModel.IsSelected = true;
            }
            dataGridView1.Refresh();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            foreach (var projectModel in Projects)
            {
                projectModel.IsSelected = false;
            }
            dataGridView1.Refresh();
        }
    }

    public class ProjectModel:INotifyPropertyChanged
    {
        private bool isSelected;
        public bool IsSelected
        {
            get { return isSelected; }
            set
            {
                isSelected = value;
                OnPropertyChanged();
            }
        }

        public string Name { get; set; }
        public FrameworkModel Framework { get; set; }

        public bool HasFramework
        {
            get { return Framework != null; }
        }

        public Project DteProject { get; set; }

        public override string ToString()
        {
            return string.Format("{0}", Name);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class FrameworkModel
    {
        public string Name { get; set; }
        public uint Id { get; set; }

        public override string ToString()
        {
            return string.Format("{0}", Name);
        }
    }
}
