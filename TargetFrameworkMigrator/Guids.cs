// Guids.cs
// MUST match guids.h
using System;

namespace VHQLabs.TargetFrameworkMigrator
{
    static class GuidList
    {
        public const string guidTargetFrameworkMigratorPkgString = "4c5b2b2f-17b1-4b85-aceb-f5e8865dc05a";
        public const string guidTargetFrameworkMigratorCmdSetString = "bbba49fb-2ebd-4110-a269-fc45a929fab9";

        public static readonly Guid guidTargetFrameworkMigratorCmdSet = new Guid(guidTargetFrameworkMigratorCmdSetString);
    };
}