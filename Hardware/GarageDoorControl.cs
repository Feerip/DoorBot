using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DoorBot.Hardware
{
    public static class GarageDoorControl
    {
        public enum GarageDoorState
        {
            CommandSuccess,
            Closed,
            AlreadyClosed,
            Open,
            AlreadyOpen,
            Error
        }

        private static readonly string isGarageDoorOpen = @"Scripts/isGarageDoorOpen.py";
        private static readonly string closeGarageDoor = @"Scripts/close_garageDoor.py";
        private static readonly string openGarageDoor = @"Scripts/open_garageDoor.py";
        private static readonly string windowsPython = @"C:\Python310\python.exe";
        private static readonly string linuxPython = @"/usr/bin/python";

        private static int ExecutePythonScript(string script)
        {
            string pythonLocation = linuxPython;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                pythonLocation = windowsPython;

            Process process = new();

            process.StartInfo = new ProcessStartInfo(pythonLocation, script)
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode < 0)
                throw new AggregateException(output);

            return process.ExitCode;
        }

        public static GarageDoorState OpenGarageDoor()
        {
            int result = ExecutePythonScript(openGarageDoor);
            if (result == 0)
                return GarageDoorState.CommandSuccess;
            else if (result == 1)
                return GarageDoorState.AlreadyOpen;
            else
                return GarageDoorState.Error;

        }
        public static GarageDoorState CloseGarageDoor()
        {
            int result = ExecutePythonScript(closeGarageDoor);
            if (result == 0)
                return GarageDoorState.CommandSuccess;
            else if (result == 1)
                return GarageDoorState.AlreadyClosed;
            else
                return GarageDoorState.Error;
        }
        public static GarageDoorState GetGarageDoorState()
        {
            int result = ExecutePythonScript(isGarageDoorOpen);
            if (result == 0)
                return GarageDoorState.Closed;
            else if (result == 1)
                return GarageDoorState.Open;
            else
                return GarageDoorState.Error;
            //throw new Exception($"Unknown state of Garage Door ({result}) returned.");
        }
    }
}
