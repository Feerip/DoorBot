using Iot.Device.Buzzer;

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
        private readonly int PASSIVE_BUZZER_PIN = 27;



#if !Windows
        Buzzer _passiveBuzzer;
        GpioController _controller;
#endif
        private GpioOutput()
        {
#if !Windows
            _controller = new( PinNumberingScheme.Logical, new System.Device.Gpio.Drivers.RaspberryPi3Driver());

            _controller.OpenPin(BUZZER_PIN, PinMode.Output);
            Console.WriteLine($"GPIO pin enabled for Buzzer: {BUZZER_PIN}");
            _controller.OpenPin(LOCK_SIGNAL_PIN, PinMode.Output);
            Console.WriteLine($"GPIO pin enabled for door signal output: {LOCK_SIGNAL_PIN}");

            _passiveBuzzer = new(PASSIVE_BUZZER_PIN);
            _passiveBuzzer.PlayTone(880, 100);
            Thread.Sleep(50);
            _passiveBuzzer.PlayTone(880, 100);
            Thread.Sleep(50);
            _passiveBuzzer.PlayTone(880, 100);
            Thread.Sleep(50);
            _passiveBuzzer.PlayTone(880, 100);
#endif
        }

        public static GpioOutput GetInstance()
        {
            return _instance;
        }

        public void BuzzerHigh()
        {
#if !Windows
            _controller.Write(BUZZER_PIN, PinValue.High);
#endif
        }

        public void BuzzerLow()
        {
#if !Windows
            _controller.Write(BUZZER_PIN, PinValue.Low);
#endif
        }

        public void MagnetHigh()
        {
#if !Windows
            _controller.Write(LOCK_SIGNAL_PIN, PinValue.High);
#endif
        }

        public void MagnetLow()
        {
#if !Windows
            _controller.Write(LOCK_SIGNAL_PIN, PinValue.Low);
#endif
        }

        private void Beep(int milliseconds)
        {
#if Windows
            // Adding a runtime platform check so .Net stops complaining
            // even though I know this code will never run on anything than Windows
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Console.Beep(3000, milliseconds);

#else
            _passiveBuzzer.StartPlaying(800);
            //BuzzerHigh();
            Thread.Sleep(milliseconds);
            _passiveBuzzer.StopPlaying();
            //BuzzerLow();
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
