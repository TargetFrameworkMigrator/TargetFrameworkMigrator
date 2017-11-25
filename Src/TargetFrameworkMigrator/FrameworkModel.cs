// Copyright (c) 2013 Pavel Samokha
using System;

namespace VHQLabs.TargetFrameworkMigrator
{
  public class FrameworkModel : IComparable
  {
    public string Moniker { get; set; }

    public string DisplayName { get; set; }

    public override string ToString() => DisplayName;

    // support comparison so we can sort by properties of this type
    public int CompareTo(object obj)
    {
      return StringComparer.Ordinal.Compare(this.Moniker, ((FrameworkModel)obj).Moniker);
    }
  }
}
