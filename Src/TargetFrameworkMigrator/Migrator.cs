// Copyright (c) 2013 Pavel Samokha

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell.Interop;

namespace VHQLabs.TargetFrameworkMigrator
{
  public class Migrator
  {
    private readonly DTE applicationObject;
    private readonly IVsFrameworkMultiTargeting frameworkMultiTargeting;
    private ProjectsUpdateList projectsUpdateList;
    private List<FrameworkModel> frameworkModels;

    private object syncRoot = new object();

    public Migrator(DTE applicationObject, IVsFrameworkMultiTargeting frameworkMultiTargeting)
    {
      this.applicationObject = applicationObject;
      this.frameworkMultiTargeting = frameworkMultiTargeting;

      frameworkMultiTargeting.GetSupportedFrameworks(out Array prgSupportedFrameworks);

      frameworkModels = prgSupportedFrameworks.Cast<string>()
                                              .Select(GetFrameworkModel)
                                              .ToList();
    }

    private FrameworkModel GetFrameworkModel(string moniker)
    {
      frameworkMultiTargeting.GetDisplayNameForTargetFx(moniker, out string displayName);
      return new FrameworkModel { DisplayName = displayName, Moniker = moniker };
    }

    private bool isSolutionLoaded = true;
    private SynchronizationContext synchronizationContext;

    public void Show()
    {
      lock (syncRoot)
      {
        synchronizationContext = SynchronizationContext.Current;

        projectsUpdateList = new ProjectsUpdateList();

        projectsUpdateList.UpdateFired += Update;
        projectsUpdateList.ReloadFired += ReloadProjects;

        projectsUpdateList.Frameworks = frameworkModels;

        projectsUpdateList.State = "Waiting all projects are loaded...";

        if (applicationObject.Solution == null)
        {
          projectsUpdateList.State = "No solution";
        }
        else
        {
          if (isSolutionLoaded)
            ReloadProjects();
        }

        projectsUpdateList.StartPosition = FormStartPosition.CenterScreen;
        projectsUpdateList.TopMost = true;
        projectsUpdateList.ShowDialog();
      }
    }

    public void OnBeforeSolutionLoaded()
    {
      lock (syncRoot)
      {
        if (projectsUpdateList != null)
          projectsUpdateList.State = "Waiting all projects are loaded...";

        isSolutionLoaded = false;
      }
    }

    public void OnAfterSolutionLoaded()
    {
      lock (syncRoot)
      {
        isSolutionLoaded = true;

        if (projectsUpdateList?.Visible == true)
          ReloadProjects();
      }
    }

    private void ReloadProjects()
    {
      var projectModels = LoadProjects();

      projectsUpdateList.State = projectModels.Count == 0 ? "No .Net projects" : String.Empty;

      projectsUpdateList.Projects = projectModels;
    }

    private List<ProjectModel> LoadProjects()
    {
      Projects projects = applicationObject.Solution.Projects;

      if (projects.Count == 0)
      {
        return new List<ProjectModel>();
      }

      var projectModels = MapProjects(projects.OfType<Project>());

      projectModels = projectModels.Where(pm => pm.HasFramework)
                                   .ToList();
      return projectModels;
    }

    private List<ProjectModel> MapProjects(IEnumerable<Project> projects)
    {
      List<ProjectModel> projectModels = new List<ProjectModel>();
      foreach (Project p in projects)
      {
        if (p == null)
          continue;

        if (p.Kind == ProjectKinds.vsProjectKindSolutionFolder)
        {
          var projectItems = p.ProjectItems.OfType<ProjectItem>();
          var subProjects = projectItems.Select(pi => pi.SubProject);
          projectModels.AddRange(MapProjects(subProjects));
        }
        else
        {
          var projectModel = MapProject(p);
          projectModels.Add(projectModel);
        }
      }
      return projectModels;
    }

    private ProjectModel MapProject(Project p)
    {
      var projectModel = new ProjectModel
      {
        Name = p.Name,
        DteProject = p,
      };
      if (p.Properties == null) return projectModel;

      // not applicable for current project
      var targetFrameworkMoniker = p.Properties
        .Cast<EnvDTE.Property>()
        .FirstOrDefault(prop => string.Compare(prop.Name, "TargetFrameworkMoniker") == 0);

      if (targetFrameworkMoniker?.Value == null)
        return projectModel;

      try
      {
        var frameworkModel = GetFrameworkModel((string)targetFrameworkMoniker.Value);
        projectModel.Framework = frameworkModel;
      }
      catch (ArgumentException e) //possible when project still loading
      {
        Debug.WriteLine("ArgumentException on " + projectModel + e);
      }
      catch (InvalidCastException e) //for some projects with wrong types
      {
        Debug.WriteLine("InvalidCastException on " + projectModel + e);
      }
      return projectModel;
    }

    async void Update()
    {
      FrameworkModel frameworkModel = projectsUpdateList.SelectedFramework;

      projectsUpdateList.State = "Updating...";

      await UpdateFrameworks(frameworkModel);

      projectsUpdateList.Projects = LoadProjects();

      projectsUpdateList.State = "Done";
    }

    private Task UpdateFrameworks(FrameworkModel frameworkModel)
    {
      return Task.Run(() =>
          {
            var enumerable = projectsUpdateList.Projects.Where(p => p.IsSelected);

            foreach (var projectModel in enumerable)
            {
              try
              {
                projectModel.DteProject.Properties.Item("TargetFrameworkMoniker").Value = frameworkModel.Moniker;

                synchronizationContext.Post(o =>
                          {
                            var pm = (ProjectModel)o;
                            projectsUpdateList.State = string.Format("Updating... {0} done", pm.Name);
                          }, projectModel);
              }
              catch (COMException e) //possible "project unavailable" for unknown reasons
              {
                Debug.WriteLine("COMException on " + projectModel.Name + e);
              }
            }
          });
    }
  }
}
