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
    public static class GpioOutput
    {
        private static Semaphore _pool = new(1, 1);
        
        private static readonly int BUZZER_PIN = 17;
        private static readonly int LOCK_SIGNAL_PIN = 23;
        private static readonly int PASSIVE_BUZZER_PIN = 27;



        static Buzzer _passiveBuzzer;
        static GpioController _controller;

        private static void SetUp()
        {
#if DEBUG
            Console.WriteLine("Setup waiting for semaphore...");
#endif
            _pool.WaitOne();
#if DEBUG
            Console.WriteLine("Got semaphore. Continuing setup.");
#endif
#if !Windows
            _controller = new( PinNumberingScheme.Logical, new System.Device.Gpio.Drivers.RaspberryPi3Driver());
#else
            _controller = new(PinNumberingScheme.Logical, new Iot.Device.Board.DummyGpioDriver());
#endif

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

        }

        private static void ShutDown()
        {
            _controller.ClosePin(BUZZER_PIN);
            _controller.ClosePin(LOCK_SIGNAL_PIN);
            _passiveBuzzer.Dispose();
            _controller.Dispose();

            _pool.Release();
#if DEBUG
            Console.WriteLine("Released semaphore");
#endif
        }

        private static void BuzzerHigh()
        {
            _controller.Write(BUZZER_PIN, PinValue.High);
        }

        private static void BuzzerLow()
        {

            _controller.Write(BUZZER_PIN, PinValue.Low);
        }

        private static void MagnetHigh()
        {
            _controller.Write(LOCK_SIGNAL_PIN, PinValue.High);
        }

        private static void MagnetLow()
        {
            _controller.Write(LOCK_SIGNAL_PIN, PinValue.Low);
        }

        private static void Beep(int milliseconds)
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

        public static void TestBeep()
        {
            SetUp();
            Beep(3000);
            ShutDown();
        }

        private static void GoodBeep()
        {
            Beep(100);
            Beep(100);

        }
        public static Task BadBeep()
        {
            SetUp();
            Beep(250);
            Beep(250);
            Beep(250);
            ShutDown();
            return Task.CompletedTask;
        }
        private static void LockBeep()
        {
            Beep(25);
        }
        private static void CheckingBeep()
        {
            Beep(100);
        }

        public static Task OpenDoorWithBeep()
        {
            SetUp();
            // Open it up
            MagnetHigh();
            GoodBeep();
            Thread.Sleep(7000);

            // Lock it down again 
            LockBeep();
            MagnetLow();
            ShutDown();
            return Task.CompletedTask;
        }
    }
}
