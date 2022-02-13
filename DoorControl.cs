using Discord;
using Discord.WebSocket;

using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Data = Google.Apis.Sheets.v4.Data;

using System.Collections.Generic;
using System.Device.Gpio;
using System.Device.I2c;
using System.Device.Spi;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Iot.Device.Card;
using Iot.Device.Card.CreditCardProcessing;
using Iot.Device.Card.Mifare;
using Iot.Device.Card.Ultralight;
using Iot.Device.Common;
using Iot.Device.Ndef;
using Iot.Device.Pn532;
using Iot.Device.Pn532.ListPassive;


namespace DoorBot
{
    public sealed class DoorControl
    {
        private static DoorControl _instance = new();
        private DoorAuth doorAuth;

        string device = "/dev/ttyS0";
        Pn532 pn532;

        int BUZZER = 17;
        int LOCK_SIGNAL = 23;

        GpioController controller;

        private DoorControl()
        {
            pn532 = new(device);
            doorAuth = DoorAuth.GetDoorAuth();
            controller = new();

            controller.OpenPin(BUZZER, PinMode.Output);
            Console.WriteLine($"GPIO pin enabled for Buzzer: {BUZZER}");
            controller.OpenPin(LOCK_SIGNAL, PinMode.Output);
            Console.WriteLine($"GPIO pin enabled for door signal output: {LOCK_SIGNAL}");


            if (pn532.FirmwareVersion is FirmwareVersion version)
            {
                Console.WriteLine(
                    $"Is it a PN532!: {version.IsPn532}, Version: {version.Version}, Version supported: {version.VersionSupported}");
                // To adjust the baud rate, uncomment the next line
                // pn532.SetSerialBaudRate(BaudRate.B0921600);

                // To dump all the registers, uncomment the next line
                // DumpAllRegisters(pn532);

                // To run tests, uncomment the next line
                // RunTests(pn532);
                // ProcessUltralight(pn532);
                //ReadMiFare(pn532);
                // TestGPIO(pn532);

                // To read Credit Cards, uncomment the next line
                // ReadCreditCard(pn532);
            }
            else
            {
                Console.WriteLine($"Error");
            }
        }
        private void ReadMiFare(Pn532 pn532)
        {
            while (true)
            {
                byte[]? retData = null;
                while ((!Console.KeyAvailable))
                {
                    retData = pn532.ListPassiveTarget(MaxTarget.One, TargetBaudRate.B106kbpsTypeA);
                    if (retData is object)
                    {
                        CheckingBeep();
                        break;
                    }

                    // Give time to PN532 to process
                    Thread.Sleep(200);
                }

                if (retData is null)
                {
                    return;
                }
                else
                {
                    Thread.Sleep(500);
                }

                var decrypted = pn532.TryDecode106kbpsTypeA(retData.AsSpan().Slice(1));
                if (decrypted is object)
                {
                    string id = $"{BitConverter.ToString(decrypted.NfcId)}";
                    //Console.WriteLine(id);

                    if (doorAuth.UserAuthorized(id))
                    {
                        OpenDoor();
                    }
                    else
                    {
                        BadBeep();
                    }    
                }
            }
        }

        public static DoorControl GetDoorControl()
        {
            return _instance;
        }

        private void GoodBeep()
        {
            Beep(100);
            Beep(100);
        }
        private void BadBeep()
        {
            Beep(250);
            Beep(250);
            Beep(250);
        }
        private void LockBeep()
        {
            Beep(25);
        }
        private void CheckingBeep()
        {
            Beep(100);
        }

        private void Beep(int milliseconds)
        {
            controller.Write(BUZZER, PinValue.High);
            Thread.Sleep(milliseconds);
            controller.Write(BUZZER, PinValue.Low);
            Thread.Sleep(100);
        }

        public void OpenDoor()
        {
            // Open it up
            controller.Write(LOCK_SIGNAL, PinValue.High);

            GoodBeep();
            OpenDoor();
            Thread.Sleep(7000);
            LockBeep();

            // Lock it down again 
            controller.Write(LOCK_SIGNAL, PinValue.Low);
        }

        public async Task CheckLoop()
        {
            while (true)
            {
                ReadMiFare(pn532);
            }
        }



    }
}