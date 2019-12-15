TargetFrameworkMigrator
=======================

Visual Studio Bulk Change Target Framework Extension

Migrate all your .Net projects to another .Net Framework Version at once.

If you have solution with many projects and want to migrate to new version of .Net framework with just Visual Studio, you must manually change  target framework in properties of every project. With this extension you can update all projects at one click.

Features:

* Support .Net Frameworks 2.0-4.8
* Support solution folders 

Available through Tools -> Target Framework Migrator


Development
===================

Frameworks list
-------------------

Edit Frameworks.xml in main project to add new framework.
Where to get Id for new framework? I get it via runtime (change one project's framework in visual studio project properties and get it's Id in debug mode).

How to debug visual studio extension
------------------------------------

Set "Run external program" in Debug to Visual Studio devenv.exe (e.g. C:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\IDE\devenv.exe) and command line arguments to "/rootsuffix Exp"
