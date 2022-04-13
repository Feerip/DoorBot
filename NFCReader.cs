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

using System.Runtime.InteropServices;
using System.Runtime;
using Iot.Device.FtCommon;

namespace DoorBot
{
    public sealed class NFCReader : IDisposable
    {
#if Windows
        private readonly string _device = "COM3";
#else   
        private readonly string _device = "/dev/ttyS0";
#endif
        private readonly DoorUserDB _doorAuth;
        private readonly Pn532 _pn532;
        private readonly GpioOutput _gpioOutput;

        private static void DebugOutput(string message)
        {
#if DEBUG
            Console.WriteLine(message);
#else
            // Uncomment for noisy mode
            Console.Error.WriteLine(message);
#endif
        }

        public NFCReader()
        {

            _doorAuth = DoorUserDB.GetInstance();
            _gpioOutput = GpioOutput.GetInstance();

            DebugOutput("Initializing PN532 in DoorControl ctor...");
            _pn532 = new(_device);
            DebugOutput("Finished initializing PN532 in DoorControl ctor.");
        }

        public void ReadMiFare()
        {
            DebugOutput("READMIFARE");
            if (_pn532 is not null)
            {
                DebugOutput("READMIFARE NOT NULL");
                //while (true)
                //{
                byte[]? retData = null;
                //while ((!Console.KeyAvailable))
                while (true)
                {
                    DebugOutput("ENTERING KEYAVAILABLE WHILE");
                    //PollingType[] type = new PollingType[] { PollingType.Passive106kbpsISO144443_4A, PollingType.MifareCard, PollingType.GenericPassive106kbps, PollingType.Passive106kbps, PollingType.Passive106kbpsISO144443_4B, PollingType.DepPassive106kbps, PollingType.DepActive106kbps  };
                    //retData = pn532.AutoPoll(0xFF, 200, type);
                    retData = _pn532.ListPassiveTarget(MaxTarget.One, TargetBaudRate.B106kbpsTypeA);


                    if (retData is not null)
                    {
                        //CheckingBeep();
                        DebugOutput("Found MiFare");
                        break;
                    }

                    // Give time to PN532 to process
                    //Thread.Sleep(200);
                    //Task.Delay(1000);
                    Thread.Sleep(1000);
                }

                if (retData is null)
                {
                    DebugOutput("HELP");
                    return;
                }
                else
                {
                    DebugOutput("Data is not null");
                    //Thread.Sleep(100);
                }

                //var decrypted = pn532.TryDecodeData106kbpsTypeB(retData.AsSpan().Slice(1));
                var decrypted = _pn532.TryDecode106kbpsTypeA(retData.AsSpan().Slice(1));

                if (decrypted is not null)
                {
                    string id = $"{BitConverter.ToString(decrypted.NfcId)}";
                    //Console.WriteLine($"{BitConverter.ToString(decrypted.NfcId)} - {decrypted.Sak} - {decrypted.Atqa} - {BitConverter.ToString(decrypted.Ats)}");
                    Console.WriteLine($"{BitConverter.ToString(decrypted.NfcId)}");
                    // Sanitize the input from pn532 by removing -'s
                    string processedID = id.Replace("-", "");

                    // Get authorization
                    string[]? user = _doorAuth.GetAuthorizedUser(processedID);

                    // If authorized
                    if (user is not null)
                    {
                        // Fire off the log entry and move on without waiting
                        /* Task log =*/
                        _doorAuth.AddToLog(user[0], user[1], user[2], user[3], true);
                        _ = _gpioOutput.OpenDoorWithBeep();
                        //Task cLog = Console.Out.WriteLineAsync($"Authorized: {DateTime.Now} {user[0]} {user[1]} {user[2]} {user[3]}");
                    }
                    // If not
                    else
                    {
                        // Fire off the log entry and move on without waiting
                        /*Task log =*/ _doorAuth.AddToLog(processedID, null, null, null, false);
                        _ = _gpioOutput.BadBeep();
                        // Fires off an async RefreshDB
                        _doorAuth.RefreshDB();
                        // Timeout of 2.0s when card failed

                        //Task.Delay(2000);
                        Thread.Sleep(2000);
                        //Task cLog = Console.Out.WriteLineAsync($"Denied: {DateTime.Now} {processedID}");
                        // If the refresh isn't done yet, wait until it is.
                    }
                    Console.WriteLine();
                }
                //}
            }
        }

        public void Dispose()
        {
            DebugOutput("FINALIZING=============");
            _pn532.Dispose();
        }
    }
}