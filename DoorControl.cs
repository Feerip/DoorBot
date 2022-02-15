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
        private async void ReadMiFare(Pn532 pn532)
        {
            while (true)
            {
                byte[]? retData = null;
                while (/*(!Console.KeyAvailable)*/ true)
                {
                    PollingType[] type = new PollingType[1] { PollingType.GenericPassive106kbps };
                    retData = pn532.AutoPoll(0x01, 100, type);
                    //retData = pn532.ListPassiveTarget(MaxTarget.One, TargetBaudRate.B106kbpsTypeA);
                    if (retData is object)
                    {
                        //CheckingBeep();
                        Console.WriteLine("Found MiFare");
                        break;
                    }

                    // Give time to PN532 to process
                    Thread.Sleep(200);
                }

                if (retData is null)
                {
                    Console.WriteLine("HELP");
                    return;
                }
                else
                {
                    Console.WriteLine("Data is not null");
                    //Thread.Sleep(100);
                }

                var decrypted = pn532.TryDecode106kbpsTypeA(retData.AsSpan().Slice(1));
                if (decrypted is object)
                {
                    string id = $"{BitConverter.ToString(decrypted.NfcId)}";
                    // Sanitize the input from pn532 by removing -'s
                    string processedID = id.Replace("-", "");
                    
                    // Get authorization
                    string[]? user = doorAuth.GetAuthorizedUser(processedID);

                    // If authorized
                    if (user is not null)
                    {
                        // Fire off the log entry and move on without waiting
                        Task log = doorAuth.AddToLog(user[0], user[1], user[2], user[3], true);

                        OpenDoor();
                        Task cLog = Console.Out.WriteLineAsync($"Authorized: {DateTime.Now} {user[0]} {user[1]} {user[2]} {user[3]}");
                    }
                    // If not
                    else
                    {
                        // Fire off the log entry and move on without waiting
                        Task log = doorAuth.AddToLog(processedID, null, null, null, false);

                        BadBeep();

                        // Fires off an async RefreshDB
                        Task refresh = doorAuth.RefreshDB();
                        // Timeout of 2.0s when card failed
                        Thread.Sleep(2000);
                        Task cLog = Console.Out.WriteLineAsync($"Denied: {DateTime.Now} {processedID}");
                        // If the refresh isn't done yet, wait until it is.
                        await Task.WhenAll(refresh);
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
            Thread.Sleep(7000);

            // Lock it down again 
            LockBeep();
            controller.Write(LOCK_SIGNAL, PinValue.Low);
        }

        public Task CheckLoop()
        {
            while (true)
            {
                try
                {
                    ReadMiFare(pn532);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                Console.WriteLine("End of Loop");
            }
            Console.WriteLine("End of loop function (going to main)");
        }



    }
}