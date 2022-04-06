using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DoorBot
{
    public sealed class GpioOutput
    {
        private static GpioOutput _instance = new();
        
        private readonly int BUZZER_PIN = 17;
        private readonly int LOCK_SIGNAL_PIN = 23;
#if !DEBUG
        GpioController _controller;
#endif
        private GpioOutput()
        {
#if !DEBUG
            _controller = new();

            _controller.OpenPin(BUZZER_PIN, PinMode.Output);
            Console.WriteLine($"GPIO pin enabled for Buzzer: {BUZZER_PIN}");
            _controller.OpenPin(LOCK_SIGNAL_PIN, PinMode.Output);
            Console.WriteLine($"GPIO pin enabled for door signal output: {LOCK_SIGNAL_PIN}");
#endif
        }

        public static GpioOutput GetInstance()
        {
            return _instance;
        }

        public void BuzzerHigh()
        {
#if !DEBUG
            _controller.Write(BUZZER_PIN, PinValue.High);
#endif
        }

        public void BuzzerLow()
        {
#if !DEBUG
            _controller.Write(BUZZER_PIN, PinValue.Low);
#endif
        }

        public void MagnetHigh()
        {
#if !DEBUG
            _controller.Write(LOCK_SIGNAL_PIN, PinValue.High);
#endif
        }

        public void MagnetLow()
        {
#if !DEBUG
            _controller.Write(LOCK_SIGNAL_PIN, PinValue.Low);
#endif
        }

        private void Beep(int milliseconds)
        {
#if DEBUG
            // Adding a runtime platform check so .Net stops complaining
            // even though I know this code will never run on anything than Windows
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Console.Beep(3000, milliseconds);

#else
            BuzzerHigh();
            Thread.Sleep(milliseconds);
            BuzzerLow();
#endif
            Thread.Sleep(50);
        }

        public void TestBeep()
        {
            Beep(3000);
        }

        public void GoodBeep()
        {
            Beep(100);
            Beep(100);

        }
        public Task BadBeep()
        {
            Beep(250);
            Beep(250);
            Beep(250);
            return Task.CompletedTask;
        }
        public void LockBeep()
        {
            Beep(25);
        }
        public void CheckingBeep()
        {
            Beep(100);
        }

        public Task OpenDoorWithBeep()
        {
            // Open it up
            MagnetHigh();
            GoodBeep();
            Thread.Sleep(7000);

            // Lock it down again 
            LockBeep();
            MagnetLow();
            return Task.CompletedTask;
        }
    }
}
