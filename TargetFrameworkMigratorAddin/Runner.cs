using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using EnvDTE;
using EnvDTE80;

namespace VSChangeTargetFrameworkExtension
{
    public class Runner
    {
        private readonly DTE2 applicationObject;
        private ProjectsUpdateList projectsUpdateList;
        private List<FrameworkModel> frameworkModels;

        public Runner(DTE2 applicationObject)
        {
            this.applicationObject = applicationObject;

            frameworkModels = new List<FrameworkModel>();
            frameworkModels.Add(new FrameworkModel() { Id = 262149, Name = ".NETFramework,Version=v4.5" });
            frameworkModels.Add(new FrameworkModel() { Id = 262144, Name = ".NETFramework,Version=v4.0" });
            frameworkModels.Add(new FrameworkModel() { Id = 262144, Name = ".NETFramework,Version=v4.0,Profile=Client" });
            frameworkModels.Add(new FrameworkModel() { Id = 196613, Name = ".NETFramework,Version=v3.5" });
            frameworkModels.Add(new FrameworkModel() { Id = 196613, Name = ".NETFramework,Version=v3.5,Profile=Client" });
            frameworkModels.Add(new FrameworkModel() { Id = 196608, Name = ".NETFramework,Version=v3.0" });
            frameworkModels.Add(new FrameworkModel() { Id = 131072, Name = ".NETFramework,Version=v2.0" });
        }

        public void Run()
        {
            Debug.WriteLine("Run!");

            if (applicationObject.Solution == null)
            {
                MessageBox.Show("No solution");
                return;
            }

            projectsUpdateList = new ProjectsUpdateList();

            var projectModels = LoadProjects();

            if (projectModels.Count == 0)
            {
                MessageBox.Show("No .Net projects");
                return;
            }

            projectsUpdateList.Projects = projectModels;

            
            projectsUpdateList.Frameworks = frameworkModels;

            projectsUpdateList.UpdateFired += projectsUpdateList_UpdateFired;

            projectsUpdateList.ShowDialog();
        }

        private List<ProjectModel> LoadProjects()
        {
            Projects projects = applicationObject.Solution.Projects;

            if (projects.Count == 0)
            {
                MessageBox.Show("No projects");
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
            var projectModel = new ProjectModel()
                {
                    Name = p.Name,
                    DteProject = p,
                };
            if (p.Properties != null)
            {
                try
                {
                    var frameworkModel = new FrameworkModel()
                        {
                            Id = (uint) p.Properties.Item("TargetFramework").Value,
                            Name = (string) p.Properties.Item("TargetFrameworkMoniker").Value
                        };
                    projectModel.Framework = frameworkModel;
                }
                catch (ArgumentException e) //possible when project still loading
                {
                    Debug.WriteLine("ArgumentException on " + p + e);
                }
            }
            return projectModel;
        }

        async void projectsUpdateList_UpdateFired()
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
                        projectModel.DteProject.Properties.Item("TargetFrameworkMoniker").Value = frameworkModel.Name;
                        //                projectModel.DteProject.Properties.Item("TargetFramework").Value = frameworkModel.Id;
                    }
                });
        }
    }
}