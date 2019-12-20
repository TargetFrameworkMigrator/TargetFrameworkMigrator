﻿// Copyright (c) 2013 Pavel Samokha
using System;
using System.Collections;
using System.Collections.Generic;
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

                    foreach (var projectModel in wrapperBindingList)
                    {
                        projectModel.PropertyChanged += ProjectModel_PropertyChanged;
                    }
                }
                catch (InvalidOperationException)
                {
                    Invoke(new EventHandler(delegate
                    {
                        dataGridView1.DataSource = wrapperBindingList;
                        dataGridView1.Refresh();
                        
                        foreach (var projectModel in wrapperBindingList)
                        {
                            projectModel.PropertyChanged += ProjectModel_PropertyChanged;
                        }
                    }));
                }
            }
            get
            {
                SortableBindingList<ProjectModel> wrapperBindingList = null;
                try
                {
                    wrapperBindingList = (SortableBindingList<ProjectModel>)dataGridView1.DataSource;
                }
                catch (InvalidOperationException)
                {
                    Invoke(new EventHandler(delegate
                    {
                        wrapperBindingList = (SortableBindingList<ProjectModel>)dataGridView1.DataSource;
                    }));
                }
                return wrapperBindingList.WrappedList;
            }
        }

        private void ProjectModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
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
    }
}
