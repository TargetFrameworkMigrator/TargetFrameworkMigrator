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

namespace VSChangeTargetFrameworkExtension
{
  public class Migrator
  {
    private readonly DTE applicationObject;
    private ProjectsUpdateList projectsUpdateList;
    private List<FrameworkModel> frameworkModels;

    private object syncRoot = new object();

    public Migrator(DTE applicationObject)
    {
      this.applicationObject = applicationObject;

      frameworkModels = new List<FrameworkModel>();

      var folderPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

      var frameworks = new XmlDocument();
      frameworks.Load(Path.Combine(folderPath, "Frameworks.xml"));
      foreach (XmlNode node in frameworks.DocumentElement.ChildNodes)
        frameworkModels.Add(new FrameworkModel { Id = uint.Parse(node.Attributes["Id"].Value), Name = node.Attributes["Name"].Value });
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

        if (projectsUpdateList != null && projectsUpdateList.Visible)
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

      projectModels = projectModels
                                  .Where(pm => pm.HasFramework)
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

    private static ProjectModel MapProject(Project p)
    {
        var projectModel = new ProjectModel
            {
                Name = p.Name,
                DteProject = p,
            };
        if (p.Properties == null) return projectModel;


        try
        {
            // check if not applicable for current project
            if (p.Properties.Item("TargetFramework") == null ||
                p.Properties.Item("TargetFrameworkMoniker") == null) return projectModel;
        }
        catch (ArgumentException e)
        {
            Debug.WriteLine("ArgumentException on " + projectModel + e);
            return projectModel;
        }

        try
        {
            var frameworkModel = new FrameworkModel
            {
                Id = (uint)p.Properties.Item("TargetFramework").Value,
                Name = (string)p.Properties.Item("TargetFrameworkMoniker").Value
            };
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
                projectModel.DteProject.Properties.Item("TargetFrameworkMoniker").Value = frameworkModel.Name;

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