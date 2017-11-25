// Copyright (c) 2013 Pavel Samokha
using System;
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
      get => isSelected;
      set
      {
        isSelected = value;
        OnPropertyChanged();
      }
    }

    public string Name
    {
      get => name;
      set
      {
        name = value;
        OnPropertyChanged();
      }
    }

    public FrameworkModel Framework { get; set; }

    public bool HasFramework => Framework != null;

    public Project DteProject { get; set; }

    public override string ToString() => Name;

    public event PropertyChangedEventHandler PropertyChanged;

    [NotifyPropertyChangedInvocator]
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }
}
