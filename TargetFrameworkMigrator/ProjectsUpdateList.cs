// Copyright (c) 2013 Pavel Samokha
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using EnvDTE;
using VSChangeTargetFrameworkExtension.Annotations;

namespace VSChangeTargetFrameworkExtension
{
  public partial class ProjectsUpdateList : Form
  {
    public event Action UpdateFired;
    public event Action ReloadFired;

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
        }
        catch (InvalidOperationException)
        {
          Invoke(new EventHandler(delegate
          {
            dataGridView1.DataSource = wrapperBindingList;
            dataGridView1.Refresh();
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
    }

    private void button2_Click(object sender, EventArgs e)
    {
      foreach (var projectModel in Projects)
      {
        projectModel.IsSelected = false;
      }
      dataGridView1.Refresh();
    }

    private void reloadButton_Click(object sender, EventArgs e)
    {
      var onReloadFired = ReloadFired;
      if (onReloadFired != null)
        onReloadFired.Invoke();
    }
  }

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

  public class FrameworkModel : IComparable
  {
    public string Name
    {
      get; set;
    }
    public uint Id
    {
      get; set;
    }

    public override string ToString()
    {
      return string.Format("{0}", Name);
    }

    // support comparison so we can sort by properties of this type
    public int CompareTo(object obj)
    {
      return StringComparer.Ordinal.Compare(this.Name, ((FrameworkModel)obj).Name);
    }
  }
}
