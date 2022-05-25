﻿// Copyright (c) 2013 Pavel Samokha
using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using System.Threading;
using System.Windows.Forms;
using EnvDTE;
using Microsoft.Win32;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using VSChangeTargetFrameworkExtension;
using IServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
using Thread = System.Threading.Thread;

namespace VHQLabs.TargetFrameworkMigrator
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    ///
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the 
    /// IVsPackage interface and uses the registration attributes defined in the framework to 
    /// register itself and its components with the shell.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the information needed to show this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(GuidList.guidTargetFrameworkMigratorPkgString)]
    public sealed class TargetFrameworkMigratorPackage : Package
    {
        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require 
        /// any Visual Studio service because at this point the package object is created but 
        /// not sited yet inside Visual Studio environment. The place to do all the other 
        /// initialization is the Initialize method.
        /// </summary>
        public TargetFrameworkMigratorPackage()
        {
            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));
        }



        /////////////////////////////////////////////////////////////////////////////
        // Overridden Package Implementation
        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            Debug.WriteLine (string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this.ToString()));
            base.Initialize();
            
            // Add our command handlers for menu (commands must exist in the .vsct file)
            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if ( null != mcs )
            {
                DTE dte = (DTE)GetService(typeof(DTE));

                if (dte != null)
                {
                    migrator = new Migrator(dte);

                    var serviceProvider = new ServiceProvider((IServiceProvider)dte);
                    solutionLoadEvents = new SolutionLoadEvents(serviceProvider);
                    synchronizationContext = SynchronizationContext.Current;

                    solutionLoadEvents.BeforeSolutionLoaded += () =>
                    {
                        synchronizationContext.Post(_ => migrator.OnBeforeSolutionLoaded(), null);
                    };

                    solutionLoadEvents.AfterSolutionLoaded += () =>
                    {
                        synchronizationContext.Post(_ => migrator.OnAfterSolutionLoaded(), null);
                    };
                }

                // Create the command for the menu item.
                CommandID menuCommandID = new CommandID(GuidList.guidTargetFrameworkMigratorCmdSet, (int)PkgCmdIDList.cmdidTargetFrameworkMigrator);
                MenuCommand menuItem = new MenuCommand(MenuItemCallback, menuCommandID);
                mcs.AddCommand(menuItem);
            }
        }
        #endregion

        private SynchronizationContext synchronizationContext;
        private SolutionLoadEvents solutionLoadEvents;
        private Migrator migrator;

        /// <summary>
        /// This function is the callback used to execute a command when the a menu item is clicked.
        /// See the Initialize method to see how the menu item is associated to this function using
        /// the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        private void MenuItemCallback(object sender, EventArgs e)
        {
            DTE dte = (DTE)GetService(typeof(DTE));

            migrator?.Show();
        }

    }
}
