using Discord;
using Discord.WebSocket;

using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;

using Microsoft.Extensions.Configuration;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Data = Google.Apis.Sheets.v4.Data;


namespace DoorBot
{
    public sealed class DoorUserDB
    {
        public static IConfiguration? _config { get; private set; } = null;
        private static DoorUserDB _instance = new();

        private readonly string _applicationName = "DoorBot";

        private readonly int ID = 0;
        private readonly int NAME = 1;
        private readonly int KEY_TYPE = 2;
        private readonly int COLOR = 3;

        private readonly int DATE = 4;
        private readonly int ACCESS_GRANTED = 5;

        private readonly SheetsService _service;
        private readonly GoogleCredential _credentials;

        private readonly string[] _scopes = { SheetsService.Scope.Spreadsheets };

        private readonly string _credentialsFile = "Config/doorbot-credentials.json";
        private readonly string _spreadsheetID;
        private readonly string _authorizedUsersRange;
        private readonly string _accessLogRange;

        private List<List<string>> _database = new();

        IConfiguration _configuration;


        private DoorUserDB()
        {

            _configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables(prefix: "DC_")
                .AddJsonFile("Config/config.json", optional: true)
                .Build();

            _spreadsheetID = _configuration.GetValue<string>("spreadsheetID");
            _authorizedUsersRange = _configuration.GetValue<string>("authorizedUsersRange");
            _accessLogRange = _configuration.GetValue<string>("accessLogRange");

            _credentials = GoogleCredential.FromFile(_credentialsFile).CreateScoped(_scopes);
            _service = new SheetsService(new()
            {
                HttpClientInitializer = _credentials,
                ApplicationName = _applicationName
            });

            RefreshDB();
        }

        public static DoorUserDB GetInstance()
        {
            return _instance;
        }
        public static DoorUserDB GetInstance(IConfiguration config)
        {
            _config = config;
            return _instance;
        }

        public Task RefreshDB()
        {
            List<List<string>> output = new();

            SpreadsheetsResource.ValuesResource.GetRequest request = _service.Spreadsheets.Values.Get(_spreadsheetID, _authorizedUsersRange);
            ValueRange response = request.Execute();

            IList<IList<object>> values = response.Values;

            if (values != null && values.Count > 0)
            {
                foreach (IList<object> row in values)
                {
                    List<string> dbRow = new();

                    dbRow.Add(row[ID].ToString() ?? "");
                    dbRow.Add(row[NAME].ToString() ?? "");
                    dbRow.Add(row[KEY_TYPE].ToString() ?? "");
                    dbRow.Add(row[COLOR].ToString() ?? "");

                    output.Add(dbRow);
                }
            }
            _database = output;

            return Task.CompletedTask;
        }

        public string[]? GetAuthorizedUser(string id)
        {
            Console.WriteLine(id);
            string[] user = new string[4];
            foreach (List<string> row in _database)
            {
                if (row[ID].Equals(id, StringComparison.OrdinalIgnoreCase))
                {
                    user[ID] = row[ID];
                    user[NAME] = row[NAME];
                    user[KEY_TYPE] = row[KEY_TYPE];
                    user[COLOR] = row[COLOR];
                    return user;
                }
            }
            return null;
        }

        public Task AddToLog(string id, string? name, string? keyType, string? color, bool accessGranted)
        {
            List<IList<object>> newEntries = new();
            List<object> rowToAppend = new();

            rowToAppend.Add(id);
            if (string.IsNullOrEmpty(name))
            {
                rowToAppend.Add("");
                rowToAppend.Add("");
                rowToAppend.Add("");
            }
            else
            {
                rowToAppend.Add(name);
                rowToAppend.Add(keyType ?? "");
                rowToAppend.Add(color ?? "");
            }
            rowToAppend.Add(DateTime.Now.ToString());
            rowToAppend.Add(accessGranted.ToString());

            newEntries.Add(rowToAppend);

            // Done adding stuff, time to send it off

            ValueRange requestBody = new();
            requestBody.Values = newEntries;

            SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum VIO = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.RAW;
            SpreadsheetsResource.ValuesResource.AppendRequest.InsertDataOptionEnum IDO = SpreadsheetsResource.ValuesResource.AppendRequest.InsertDataOptionEnum.INSERTROWS;

            SpreadsheetsResource.ValuesResource.AppendRequest request = _service.Spreadsheets.Values.Append(requestBody, _spreadsheetID, _accessLogRange);
            request.ValueInputOption = VIO;
            request.InsertDataOption = IDO;

            Data.AppendValuesResponse response = request.Execute();

            return Task.CompletedTask;
        }
    }
}