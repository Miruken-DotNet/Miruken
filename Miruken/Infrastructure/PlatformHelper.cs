namespace Miruken.Infrastructure
{
    using System;
    using System.IO;
    using System.Reflection;

    public static class PlatformHelper
    {
        public static string GetCurrentDirectory()
        {
            var os = Environment.OSVersion;
            if (os.Platform == PlatformID.WinCE)
            {
                return Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetName().CodeBase);
            }
            return Directory.GetCurrentDirectory();
        }

        public static bool IsEmulator()
        {
            //var os = Environment.OSVersion;
            //if (os.Platform == PlatformID.WinCE)
            //{
            //    var oem = DeviceManagement.OemInfo;
            //    if (oem == "Microsoft DeviceEmulator")
            //        return true;
            //}
            return false;
        }

        public static bool IsFirstInstance(Guid programId)
        {
            var created = false;
            //var mutex = new NamedMutex(true, programId.ToString(), out created);
            //if (!created)
            //{
            //    //TODO: Find existing instance and bring forward
            //}
            return created;
        }

        public static int ShutdownOtherInstances()
        {
            var count = 0;
            //var id = (uint)ProcessHelper.GetCurrentProcessID();
            //var procs = ProcessEntry.GetProcesses();
            //var me = procs.First(p => p.ProcessID == id);

            //foreach (var proc in procs.Where(p => p.ProcessID != id && p.ExeFile == me.ExeFile))
            //{
            //    proc.Kill();
            //    count++;
            //}
            return count;
        }

        public static bool IsHandheld()
        {
            //var os = Environment.OSVersion;
            //if (os.Platform == PlatformID.WinCE)
            //{
            //    var oem = DeviceManagement.OemInfo;
            //    if (oem.ToLower().Contains("falcon"))
            //        return true;
            //}
            return false;
        }
    }
}
