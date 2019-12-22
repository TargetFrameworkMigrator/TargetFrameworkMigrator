// Copyright (c) 2013 Pavel Samokha
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VSChangeTargetFrameworkExtension
{
    public partial class ProjectsUpdateList : Form
    {
        public event Action UpdateFired;
        public event Action ReloadFired;
        public event Action SelectedFired;

        public ProjectsUpdateList()
        {
            InitializeComponent();
            dataGridView1.AutoGenerateColumns = false;
            comboBox1.DropDownStyle = ComboBoxStyle.DropDownList;
            dataGridView1.CellMouseUp += DataGridView1_OnCellMouseUp;
            dataGridView1.RowEnter += DataGridView1_RowEnter;
            dataGridView1.RowLeave += DataGridView1_RowLeave;
            EnableButtons();
        }

        private void EnableButtons()
        {
            button1.Enabled = EnabledButton;
            button2.Enabled = EnabledButton;
            button3.Enabled = EnabledButton;
        }

        public bool EnabledButton
        {
            get => Projects?.Count(p => p.IsSelected) > 0;
        }


        public List<FrameworkModel> Frameworks
        {
            set
            {
                comboBox1.DataSource = value;
            }
        }


        public List<ProjectModel> Projects
        {
            set
            {
                var wrapperBindingList = new SortableBindingList<ProjectModel>(value);
                try
                {
                    dataGridView1.DataSource = wrapperBindingList;
                    dataGridView1.Refresh();

                    SetPropertyChanged(wrapperBindingList);
                }
                catch (InvalidOperationException)
                {
                    Invoke(new EventHandler(delegate
                    {
                        dataGridView1.DataSource = wrapperBindingList;
                        dataGridView1.Refresh();

                        SetPropertyChanged(wrapperBindingList);
                    }));
                }
            }
            get
            {
                SortableBindingList<ProjectModel> wrapperBindingList = null;
                try
                {
                    wrapperBindingList = (SortableBindingList<ProjectModel>)dataGridView1.DataSource;
                    SetPropertyChanged(wrapperBindingList);
                }
                catch (InvalidOperationException)
                {
                    Invoke(new EventHandler(delegate
                    {
                        wrapperBindingList = (SortableBindingList<ProjectModel>)dataGridView1.DataSource;
                        SetPropertyChanged(wrapperBindingList);
                    }));
                }
                return wrapperBindingList?.WrappedList;
            }
        }

        private void SetPropertyChanged(SortableBindingList<ProjectModel> wrapperBindingList)
        {
            if (wrapperBindingList != null)
            {
                foreach (var projectModel in wrapperBindingList)
                {
                    projectModel.PropertyChanged += ProjectModel_PropertyChanged;
                }
            }
        }

        private void ProjectModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            EnableButtons();
            SelectedFired?.Invoke();
        }

        public FrameworkModel SelectedFramework
        {
            get
            {
                FrameworkModel model = null;
                Invoke(new EventHandler(delegate
                {
                    model = (FrameworkModel)comboBox1.SelectedItem;
                }));
                return model;
            }
        }

        public string State
        {
            set
            {
                try
                {
                    label1.Text = value;
                }
                catch (InvalidOperationException)
                {
                    Invoke(new EventHandler(delegate
                    {
                        label1.Text = value;
                    }));
                }
            }
        }

        private async void button3_Click(object sender, EventArgs e)
        {
            var onUpdate = UpdateFired;
            if (onUpdate != null)
                await Task.Run(() =>
                {
                    onUpdate.Invoke();
                });
        }

        private void button1_Click(object sender, EventArgs e)
        {
            foreach (var projectModel in Projects)
            {
                projectModel.IsSelected = true;
            }
            dataGridView1.Refresh();
            SelectedFired?.Invoke();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            foreach (var projectModel in Projects)
            {
                projectModel.IsSelected = false;
            }
            dataGridView1.Refresh();
            SelectedFired?.Invoke();
        }

        private void reloadButton_Click(object sender, EventArgs e)
        {
            ReloadFired?.Invoke();
        }

        private void DataGridView1_OnCellMouseUp(object sender, DataGridViewCellMouseEventArgs e)
        {
            // Click on checkbox is done editing
            if (e.ColumnIndex == Update.Index && e.RowIndex != -1)
            {
                dataGridView1.EndEdit();
            }
        }

        private int? rowIndex;

        private void DataGridView1_RowEnter(object sender, DataGridViewCellEventArgs e)
        {
            rowIndex = e.RowIndex;
        }


        private void DataGridView1_RowLeave(object sender, DataGridViewCellEventArgs e)
        {
            rowIndex = null;
        }

        private void DataGridView1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space)
            {
                //handle space in the correct row, and also end editing
                if (rowIndex.HasValue)
                {
                    //toggle
                    var projectModel = Projects[rowIndex.Value];
                    projectModel.IsSelected = !projectModel.IsSelected;
                }
                dataGridView1.EndEdit();

            }
        }
    }
}
