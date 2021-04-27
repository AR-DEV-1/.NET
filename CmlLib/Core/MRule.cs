﻿using Newtonsoft.Json.Linq;
using System;
using System.Runtime.InteropServices;

namespace CmlLib.Core
{
    public static class MRule
    {
        static MRule()
        {
            OSName = getOSName();

            if (Environment.Is64BitOperatingSystem)
                Arch = "64";
            else
                Arch = "32";
        }

        public static readonly string Windows = "windows";
        public static readonly string OSX = "osx";
        public static readonly string Linux = "linux";

        public static string OSName { get; private set; }
        public static string Arch { get; private set; }

        private static string getOSName()
        {
            // Environment.OSVersion.Platform does not work in NET Core   
#if NETCOREAPP
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return OSX;
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return Windows;
            else
                return Linux;
#elif NETFRAMEWORK
            var osType = Environment.OSVersion.Platform;

            if (osType == PlatformID.MacOSX)
                return OSX;
            else if (osType == PlatformID.Unix)
                return Linux;
            else
                return Windows;
#endif
        }

        public static bool CheckOSRequire(JArray arr)
        {
            var require = true;

            foreach (var token in arr)
            {
                var job = token as JObject;
                if (job == null)
                    continue;

                bool action = true; // true : "allow", false : "disallow"
                bool containCurrentOS = true; // if 'os' JArray contains current os name

                foreach (var item in job)
                {
                    if (item.Key == "action")
                        action = (item.Value?.ToString() == "allow");
                    else if (item.Key == "os")
                        containCurrentOS = checkOSContains((JObject)item.Value);
                    else if (item.Key == "features") // etc
                        return false;
                }

                if (!action && containCurrentOS)
                    require = false;
                else if (action && containCurrentOS)
                    require = true;
                else if (action && !containCurrentOS)
                    require = false;
            }

            return require;
        }

        private static bool checkOSContains(JObject job)
        {
            foreach (var os in job)
            {
                if (os.Key == "name" && os.Value.ToString() == OSName)
                    return true;
            }
            return false;
        }
    }
}
