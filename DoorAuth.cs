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
    public sealed class DoorAuth
    {
        private static DoorAuth _instance = new();

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


        private DoorAuth()
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

        public static DoorAuth GetDoorAuth()
        {
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

                    dbRow.Add(row[ID].ToString());
                    dbRow.Add(row[NAME].ToString());
                    dbRow.Add(row[KEY_TYPE].ToString());
                    dbRow.Add(row[COLOR].ToString());

                    output.Add(dbRow);
                }
            }
            _database = output;

            return Task.CompletedTask;
        }

        public string[]? GetAuthorizedUser(string id)
        {
            Console.WriteLine(processedID);
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
                rowToAppend.Add(keyType);
                rowToAppend.Add(color);
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

        //        private FruitPantry()
        //        {
        //            _applicationName = "FruitBot";
        //            _credentialsFile = "Config/credentials.json";
        //            _spreadsheetId = "1iCJHsiC4nEjjFz1Gmw4aTldnMFR5ZAlGSuJfHbP262s";


        //            _dropLogRange = "Drop Log!A2:J";
        //            _classificationRange = $"Classifications!A2:C";
        //            _thresholdValuesRange = $"Classifications!D2:E2";
        //            _itemDatabaseRange = $"Item Database!A2:G";
        //            _playerDatabaseRange = $"Players!A2:D";
        //            _botVoteTrackerRange = $"Vote Tracker!A2:C";
        //            _gobVoteTrackerRange = $"Vote Tracker!A2:E";
        //            _bugReportRange = $"Bug Reports!A2:D";
        //            _suggestionRange = $"Suggestions!A2:D";

        //            _dropLog = new(comparer: new LogEntryKeyComparer());


        //            _credentials = GoogleCredential.FromFile(_credentialsFile).CreateScoped(_scopes);

        //            _service = new SheetsService(new()
        //            {
        //                HttpClientInitializer = _credentials,
        //                ApplicationName = _applicationName
        //            });

        //            _rand = new();

        //            RefreshEverything();
        //        }

    }
    //    public sealed class FruitPantry
    //    {
    //        public static string _version = "1.7";

    //        private static readonly FruitPantry _instance = new();

    //        private readonly string[] _scopes = { SheetsService.Scope.Spreadsheets };
    //        private readonly string _applicationName;
    //        private readonly string _credentialsFile;
    //        private readonly string _spreadsheetId;

    //        private readonly string _dropLogRange;
    //        private readonly string _classificationRange;
    //        private readonly string _playerDatabaseRange;
    //        private readonly string _itemDatabaseRange;
    //        private readonly string _thresholdValuesRange;
    //        private readonly string _botVoteTrackerRange;
    //        private readonly string _gobVoteTrackerRange;
    //        private readonly string _bugReportRange;
    //        private readonly string _suggestionRange;

    //        private readonly SheetsService _service;
    //        private readonly GoogleCredential _credentials;



    //        private SortedDictionary<string, DropLogEntry> _dropLog;
    //        public Dictionary<string, ItemDatabaseEntry> _itemDatabase { get; private set; }
    //        public Dictionary<string, float> _classificationList { get; set; }
    //        public Dictionary<string, Discord.Color> _classificationColorList { get; set; }
    //        public Dictionary<string, List<string>> _discordUsers { get; set; }
    //        public Dictionary<string, List<string>> _runescapePlayers { get; set; }

    //        public int _universalThresholdValue { get; set; }
    //        public float _thresholdMultiplier { get; set; }


    //        public readonly int RSN = 0;
    //        public readonly int Fruit = 1;
    //        public readonly int Drop = 2;
    //        public readonly int Timestamp = 3;
    //        public readonly int RuneMetricsID = 4;
    //        public readonly int PlayerAvatarLink = 5;
    //        public readonly int DropIconLink = 6;
    //        public readonly int BossName = 7;
    //        public readonly int PointValue = 8;
    //        public readonly int EntryKey = 9;

    //        public readonly int Classification = 0;
    //        public readonly int PointsPerDrop = 1;
    //        public readonly int ClassificationColor = 2;

    //        public readonly int PlayerDB_RSN = 0;
    //        public readonly int PlayerDB_Fruit = 1;
    //        public readonly int PlayerDB_DiscordTag = 2;
    //        public readonly int PlayerDB_DiscordID = 3;

    //        public readonly int ThresholdValue = 0;
    //        public readonly int ThresholdMultiplier = 1;

    //        public readonly int ItemDBItemName = 0;
    //        public readonly int ItemDBClassification = 1;
    //        public readonly int ItemDBWikiLink = 4;
    //        public readonly int ItemDBImageURL = 5;
    //        public readonly int ItemDBActiveFlag = 6;

    //        public readonly int Version = 0;
    //        public readonly int GoodBotVotes = 1;
    //        public readonly int BadBotVotes = 2;
    //        public readonly int GoodGobVotes = 3;
    //        public readonly int BadGobVotes = 4;


    //        public static int NumNewEntries = 0;

    //        public Random _rand;

    //        public static class PointsCalculator
    //        {
    //            private static readonly FruitPantry _thePantry = GetFruitPantry();

    //            // Calculates the point value of a single drop entry with all variables considered
    //            public static float CalculatePoints(DropLogEntry drop)
    //            {
    //                float basePoints = _thePantry._classificationList[drop._bossName];
    //                float thresholdMultiplier = _thePantry._thresholdMultiplier;

    //                int thresholdLevel = ThresholdLevel(drop._playerName, drop._bossName);

    //                float pointValue = basePoints * (float)(Math.Pow(thresholdMultiplier, thresholdLevel));


    //                return pointValue;

    //            }

    //            // Takes the drop log of a single player and returns the threshold level they are currently at for any given boss
    //            public static int ThresholdLevel(string playerName, string bossName)
    //            {
    //                int thresholdValue = _thePantry._universalThresholdValue;

    //                // If universal threshold is set to 0 in db, that means admins don't want points being awarded right now. 
    //                // They should have also set the threshold multiplier to 0. In which case, returning 1 marks the drop as
    //                // triggering the threshold, and thus multiplying the point value by 0, getting a final 
    //                // point value of 0 for the drop.
    //                if (thresholdValue == 0)
    //                {
    //                    return 1;
    //                }

    //                List<DropLogEntry> playerLog = FilterByPlayer(playerName);

    //                int dropsFromThisBoss = 0;

    //                foreach (DropLogEntry entry in playerLog)
    //                {
    //                    if (entry._bossName.Equals(bossName) && (float.Parse(entry._pointValue) > 0))
    //                    {
    //                        dropsFromThisBoss++;
    //                    }
    //                }

    //                int thresholdLevel = dropsFromThisBoss / thresholdValue;
    //                return thresholdLevel;
    //            }

    //            // Takes the whole drop log and filter it down to just those of a certain player
    //            public static List<DropLogEntry> FilterByPlayer(string playerName)
    //            {
    //                List<DropLogEntry> playerLog = new();

    //                foreach (KeyValuePair<string, DropLogEntry> entry in _thePantry._dropLog)
    //                {
    //                    if (entry.Value._playerName.ToLower().Equals(playerName.ToLower()))
    //                    {
    //                        if ((_thePantry._runescapePlayers.ContainsKey(playerName)) && (!entry.Value._fruit.Equals(_thePantry._runescapePlayers[entry.Value._playerName.ToLower()][0])))
    //                        {
    //                            continue;
    //                        }

    //                        playerLog.Add(entry.Value);
    //                    }
    //                }
    //                return playerLog;
    //            }

    //            public static SortedDictionary<string, float> PointsOfAllParticipants()
    //            {
    //                List<string> everyone = new();

    //                foreach (KeyValuePair<string, List<string>> player in _thePantry._runescapePlayers)
    //                {
    //                    everyone.Add(player.Key);
    //                }

    //                return PointsForListOfPlayers(everyone);
    //            }

    //            public static SortedDictionary<string, float> PointsOfFruitTeamMembers(string fruit)
    //            {
    //                List<string> fruitTeamMembers = new();

    //                foreach (KeyValuePair<string, List<string>> player in _thePantry._runescapePlayers)
    //                {
    //                    if (player.Value[0].Equals(fruit, StringComparison.OrdinalIgnoreCase))
    //                    {
    //                        fruitTeamMembers.Add(player.Key);
    //                    }
    //                }

    //                return PointsForListOfPlayers(fruitTeamMembers);
    //            }

    //            public static SortedDictionary<string, float> PointsForListOfPlayers(List<string> runescapePlayers)
    //            {
    //                SortedDictionary<string, float> output = new();

    //                foreach (string player in runescapePlayers)
    //                {
    //                    output.Add(player, PointsByRSN(player));
    //                }

    //                return output;
    //            }

    //            // Calculates and returns the point value contributions of a given player
    //            public static float PointsByRSN(string runescapeName)
    //            {
    //                return PointsForListOfEntries(FilterByPlayer(runescapeName));
    //            }

    //            // Calculates and returns the point value contributions of a given player - by Discord tag
    //            public static float PointsByDiscordTag(string discordTag)
    //            {
    //                return PointsByRSN(_thePantry._discordUsers[discordTag][1].ToLower());
    //            }

    //            // Totals up and returns the point value for all drops in a given list.
    //            public static float PointsForListOfEntries(List<DropLogEntry> entries)
    //            {
    //                float result = 0;
    //                foreach (DropLogEntry entry in entries)
    //                {
    //                    result += float.Parse(entry._pointValue);
    //                }
    //                return result;
    //            }
    //        }

    //        private FruitPantry()
    //        {
    //            _applicationName = "FruitBot";
    //            _credentialsFile = "Config/credentials.json";
    //            _spreadsheetId = "1iCJHsiC4nEjjFz1Gmw4aTldnMFR5ZAlGSuJfHbP262s";


    //            _dropLogRange = "Drop Log!A2:J";
    //            _classificationRange = $"Classifications!A2:C";
    //            _thresholdValuesRange = $"Classifications!D2:E2";
    //            _itemDatabaseRange = $"Item Database!A2:G";
    //            _playerDatabaseRange = $"Players!A2:D";
    //            _botVoteTrackerRange = $"Vote Tracker!A2:C";
    //            _gobVoteTrackerRange = $"Vote Tracker!A2:E";
    //            _bugReportRange = $"Bug Reports!A2:D";
    //            _suggestionRange = $"Suggestions!A2:D";

    //            _dropLog = new(comparer: new LogEntryKeyComparer());


    //            _credentials = GoogleCredential.FromFile(_credentialsFile).CreateScoped(_scopes);

    //            _service = new SheetsService(new()
    //            {
    //                HttpClientInitializer = _credentials,
    //                ApplicationName = _applicationName
    //            });

    //            _rand = new();

    //            RefreshEverything();
    //        }

    //        public SortedDictionary<string, DropLogEntry> RefreshEverything()
    //        {
    //            Parallel.For(0, 5, idx =>
    //            {
    //                if (idx == 0)
    //                {
    //                    RefreshDropLog();
    //                }
    //                else if (idx == 1)
    //                {
    //                    RefreshClassifications();
    //                }
    //                else if (idx == 2)
    //                {
    //                    RefreshPlayerDatabase();
    //                }
    //                else if (idx == 3)
    //                {
    //                    RefreshThresholdValues();
    //                }
    //                else if (idx == 4)
    //                {
    //                    RefreshItemDatabase();
    //                }
    //            });

    //            return _dropLog;
    //        }

    //        public async Task<int> ScrapeGameData(IDiscordClient discordClient)
    //        {
    //            RefreshEverything();

    //            await Add(DropLogEntry.CreateListFullAuto().Result, (DiscordSocketClient)discordClient);


    //            return RefreshEverything().Count;
    //        }

    //        public void RefreshClassifications()
    //        {
    //            Dictionary<string, float> parsedPoints = new();
    //            Dictionary<string, Discord.Color> parsedColors = new();
    //            SpreadsheetsResource.ValuesResource.GetRequest request = _service.Spreadsheets.Values.Get(_spreadsheetId, _classificationRange);
    //            ValueRange response = request.Execute();

    //            IList<IList<object>> values = response.Values;

    //            if (values != null && values.Count > 0)
    //            {
    //                foreach (IList<object> row in values)
    //                {
    //                    parsedPoints.Add(row[Classification].ToString(), float.Parse(row[PointsPerDrop].ToString()));
    //                    parsedColors.Add(row[Classification].ToString(), new(Convert.ToUInt32(row[ClassificationColor].ToString(), 16)));

    //                }
    //            }
    //            _classificationList = parsedPoints;
    //            _classificationColorList = parsedColors;
    //        }

    //        public void RefreshItemDatabase()
    //        {
    //            Dictionary<string, ItemDatabaseEntry> output = new();
    //            SpreadsheetsResource.ValuesResource.GetRequest request = _service.Spreadsheets.Values.Get(_spreadsheetId, _itemDatabaseRange);
    //            ValueRange response = request.Execute();

    //            IList<IList<object>> values = response.Values;

    //            if (values != null && values.Count > 0)
    //            {
    //                foreach (IList<object> row in values)
    //                {
    //                    ItemDatabaseEntry newItem = new();
    //                    newItem._itemName = row[ItemDBItemName].ToString();
    //                    newItem._classification = row[ItemDBClassification].ToString();
    //                    newItem._wikiLink = row[ItemDBWikiLink].ToString();
    //                    newItem._imageURL = row[ItemDBImageURL].ToString();
    //                    if (row[ItemDBActiveFlag].ToString().Equals("TRUE", StringComparison.OrdinalIgnoreCase))
    //                    {
    //                        newItem._monitored = true;
    //                    }
    //                    else
    //                    {
    //                        newItem._monitored = false;
    //                    }

    //                    output.Add(row[ItemDBItemName].ToString().ToLower(), newItem);
    //                }
    //            }
    //            _itemDatabase = output;
    //            Console.WriteLine();
    //        }
    //        public void RefreshThresholdValues()
    //        {
    //            int thresholdValue = new();
    //            float thresholdMultiplier = new();
    //            SpreadsheetsResource.ValuesResource.GetRequest request = _service.Spreadsheets.Values.Get(_spreadsheetId, _thresholdValuesRange);
    //            ValueRange response = request.Execute();

    //            IList<IList<object>> values = response.Values;

    //            if (values != null && values.Count > 0)
    //            {
    //                thresholdValue = int.Parse(values[0][ThresholdValue].ToString());
    //                thresholdMultiplier = float.Parse(values[0][ThresholdMultiplier].ToString());
    //            }
    //            _universalThresholdValue = thresholdValue;
    //            _thresholdMultiplier = thresholdMultiplier;

    //        }
    //        public void RefreshPlayerDatabase()
    //        {
    //            Dictionary<string, List<string>> discordUsersOutput = new();
    //            Dictionary<string, List<string>> runescapePlayersOutput = new();
    //            SpreadsheetsResource.ValuesResource.GetRequest request = _service.Spreadsheets.Values.Get(_spreadsheetId, _playerDatabaseRange);
    //            ValueRange response = request.Execute();

    //            IList<IList<object>> values = response.Values;

    //            if (values != null && values.Count > 0)
    //            {
    //                foreach (IList<object> row in values)
    //                {
    //                    discordUsersOutput.Add(row[PlayerDB_DiscordID].ToString(), new() { row[PlayerDB_Fruit].ToString(), row[PlayerDB_RSN].ToString().ToLower() });
    //                    runescapePlayersOutput.Add(row[PlayerDB_RSN].ToString().ToLower(), new() { row[PlayerDB_Fruit].ToString(), row[PlayerDB_DiscordID].ToString() });
    //                }
    //            }
    //            _discordUsers = discordUsersOutput;
    //            _runescapePlayers = runescapePlayersOutput;
    //        }

    //        public static FruitPantry GetFruitPantry()
    //        {
    //            return _instance;
    //        }

    //        // Refreshes and returns the current drop log as per google sheets.
    //        public SortedDictionary<string, DropLogEntry> RefreshDropLog()
    //        {
    //            SortedDictionary<string, DropLogEntry> output = new(new LogEntryKeyComparer());

    //            SpreadsheetsResource.ValuesResource.GetRequest request = _service.Spreadsheets.Values.Get(_spreadsheetId, _dropLogRange);

    //            // Add() function is too stupid fast, we need time for google to process the new entry before we can pull it. 
    //            Thread.Sleep(1000);
    //            ValueRange response = request.Execute();

    //            IList<IList<object>> values = response.Values;

    //            if (values != null && values.Count > 0)
    //            {
    //                foreach (IList<object> row in values)
    //                {
    //                    output.Add(row[EntryKey].ToString(), new(
    //                                                playerName: row[RSN],
    //                                                fruit: row[Fruit],
    //                                                dropName: row[Drop],
    //                                                timestamp: row[Timestamp],
    //                                                playerAvatarPNG: row[PlayerAvatarLink],
    //                                                dropIconWEBP: row[DropIconLink],
    //                                                bossName: row[BossName],
    //                                                runemetricsDropID: row[RuneMetricsID],
    //                                                pointValue: row[PointValue],
    //                                                entryKey: row[EntryKey]));
    //                }

    //            }

    //            _dropLog = output;
    //            return _dropLog;
    //        }

    //        public SortedDictionary<string, DropLogEntry> PurgeThePantry()
    //        {
    //            Data.ClearValuesRequest requestBody = new Data.ClearValuesRequest();
    //            SpreadsheetsResource.ValuesResource.ClearRequest request = _service.Spreadsheets.Values.Clear(requestBody, _spreadsheetId, _dropLogRange);
    //            Data.ClearValuesResponse response = request.Execute();

    //            return RefreshEverything();
    //        }


    //        // Theoretically should be a very fast way to check uniqueness
    //        public bool AlreadyExists(DropLogEntry entry)
    //        {
    //            return _dropLog.ContainsKey(entry._entryKey);
    //        }

    //        public bool IsBeingMonitored(DropLogEntry entry)
    //        {
    //            if (_itemDatabase.ContainsKey(entry._dropName.ToLower()))
    //            {
    //                return _itemDatabase[entry._dropName.ToLower()]._monitored;
    //            }
    //            else
    //            {
    //                return false;
    //            }
    //        }

    //        public bool UnknownsBeingMonitored()
    //        {
    //            if (_itemDatabase.ContainsKey("unknown"))
    //            {
    //                return _itemDatabase["unknown"]._monitored;
    //            }
    //            else
    //            {
    //                return false;
    //            }
    //        }


    //        // Adds an entry to the drop log, sending it to google sheets. Refreshes _masterList and returns it. 
    //        public async Task<SortedDictionary<string, DropLogEntry>> Add(DropLogEntry entry)
    //        {
    //            List<IList<object>> newEntries = new();


    //            List<object> rowToAppend = new();
    //            rowToAppend.Add(entry._playerName);
    //            rowToAppend.Add(entry._fruit);
    //            rowToAppend.Add(entry._dropName);
    //            rowToAppend.Add(entry._timestamp);
    //            rowToAppend.Add(entry._runemetricsDropID);
    //            rowToAppend.Add(entry._playerAvatarPNG);
    //            rowToAppend.Add(entry._dropIconWEBP);
    //            rowToAppend.Add(entry._bossName);
    //            rowToAppend.Add(entry._pointValue);
    //            rowToAppend.Add(entry._entryKey);

    //            newEntries.Add(rowToAppend);

    //            ValueRange requestBody = new();
    //            requestBody.Values = newEntries;

    //            SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum VIO = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.RAW;
    //            SpreadsheetsResource.ValuesResource.AppendRequest.InsertDataOptionEnum IDO = SpreadsheetsResource.ValuesResource.AppendRequest.InsertDataOptionEnum.INSERTROWS;

    //            SpreadsheetsResource.ValuesResource.AppendRequest request = _service.Spreadsheets.Values.Append(requestBody, _spreadsheetId, _dropLogRange);
    //            request.ValueInputOption = VIO;
    //            request.InsertDataOption = IDO;

    //            Data.AppendValuesResponse response = request.Execute();
    //            RefreshDropLog();

    //            return _dropLog;
    //        }

    //        public void RegisterPlayer(string runescapeName, string fruit = null, string discordTag = null, ulong? discordId = null)
    //        {
    //            List<IList<object>> newEntries = new();


    //            List<object> rowToAppend = new();
    //            rowToAppend.Add(runescapeName);
    //            rowToAppend.Add(fruit);
    //            rowToAppend.Add(discordTag);
    //            rowToAppend.Add(discordId);

    //            newEntries.Add(rowToAppend);

    //            ValueRange requestBody = new();
    //            requestBody.Values = newEntries;
    //            SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum VIO = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.RAW;
    //            SpreadsheetsResource.ValuesResource.AppendRequest.InsertDataOptionEnum IDO = SpreadsheetsResource.ValuesResource.AppendRequest.InsertDataOptionEnum.INSERTROWS;

    //            SpreadsheetsResource.ValuesResource.AppendRequest request = _service.Spreadsheets.Values.Append(requestBody, _spreadsheetId, _playerDatabaseRange);
    //            request.ValueInputOption = VIO;
    //            request.InsertDataOption = IDO;

    //            Data.AppendValuesResponse response = request.Execute();

    //            RefreshEverything();
    //            return;
    //        }

    //        public async Task<SortedDictionary<string, DropLogEntry>> Add(List<DropLogEntry> entries, DiscordSocketClient discordClient)
    //        {
    //            foreach (DropLogEntry entry in entries)
    //            {
    //                if (!AlreadyExists(entry) && IsBeingMonitored(entry))
    //                {
    //                    NumNewEntries++;
    //                    try
    //                    {
    //                        entry._fruit = _runescapePlayers[entry._playerName.ToLower()][0];
    //                    }
    //                    catch (KeyNotFoundException)
    //                    {
    //#if FRUITWARSMODE
    //                        Console.WriteLine(e.Message);
    //                        await discordClient.GetGuild(769476224363397140).GetTextChannel(862385904719364096).SendMessageAsync(
    //                            $"Warning: Found a fruitless heathen ({entry._playerName}) in scraped Runepixels data. This drop will not be added to the drop log.");
    //                        continue;
    //#endif
    //                    }
    //                    entry._bossName = _itemDatabase[entry._dropName.ToLower()]._classification;
    //                    entry._pointValue = PointsCalculator.CalculatePoints(entry).ToString();
    //                    await Add(entry);

    //                }
    //                //If unknown item
    //                else if (!AlreadyExists(entry) && UnknownsBeingMonitored() && !_itemDatabase.ContainsKey(entry._dropName.ToLower()))
    //                {
    //                    NumNewEntries++;
    //                    try
    //                    {
    //                        entry._fruit = _runescapePlayers[entry._playerName.ToLower()][0];
    //                    }
    //                    catch (KeyNotFoundException)
    //                    {
    //#if FRUITWARSMODE
    //                        //Console.WriteLine(e.Message);
    //                        //await discordClient.GetGuild(769476224363397140).GetTextChannel(862385904719364096).SendMessageAsync(
    //                        //    $"Warning: Found a fruitless heathen ({entry._playerName}) in scraped Runepixels data. This drop will not be added to the drop log.");
    //                        //continue;
    //#endif
    //                    }
    //                    entry._bossName = "Unknowns";
    //                    entry._pointValue = "0";
    //                    await Add(entry);

    //                }
    //            }
    //            return _dropLog;
    //        }

    //        public SortedDictionary<string, DropLogEntry> GetDropLog()
    //        {
    //            return _dropLog;
    //        }

    //        public class VoteResponse
    //        {
    //            public string version = "";
    //            public int goodBot = 0;
    //            public int badBot = 0;
    //            public string message;
    //        }

    //        public VoteResponse QueryGoodBot()
    //        {
    //            // 1 vote for good bot
    //            VoteResponse voteResponse = QueryBotVotes(1, 0);
    //            voteResponse.message = FruitBotResponses.goodBotResponses[_rand.Next(FruitBotResponses.goodBotResponses.Count())];

    //            return voteResponse;
    //        }
    //        public VoteResponse QueryBadBot()
    //        {
    //            // 2 votes for bad bot
    //            VoteResponse voteResponse = QueryBotVotes(0, 2);
    //            voteResponse.message = FruitBotResponses.badBotResponses[_rand.Next(FruitBotResponses.badBotResponses.Count())];

    //            return voteResponse;
    //        }

    //        private VoteResponse QueryBotVotes(int upvoteInput = 0, int downvoteInput = 0)
    //        {
    //            VoteResponse voteResponse = new();

    //            // Download part
    //            SpreadsheetsResource.ValuesResource.GetRequest request = _service.Spreadsheets.Values.Get(_spreadsheetId, _botVoteTrackerRange);
    //            ValueRange response = request.Execute();
    //            IList<IList<object>> values = response.Values;

    //            if (values != null && values.Count > 0)
    //            {
    //                IList<object> row = values[0];

    //                voteResponse.version = row[Version].ToString();
    //                voteResponse.goodBot = int.Parse(row[GoodBotVotes].ToString());
    //                voteResponse.badBot = int.Parse(row[BadBotVotes].ToString());

    //            }
    //            else
    //            {
    //                return null;
    //            }

    //            // Apply the new vote(s)
    //            voteResponse.goodBot += upvoteInput;
    //            voteResponse.badBot += downvoteInput;

    //            // Upload part
    //            List<IList<object>> newEntries = new();
    //            List<object> rowToAppend = new();
    //            rowToAppend.Add(voteResponse.version.ToString());
    //            rowToAppend.Add(voteResponse.goodBot);
    //            rowToAppend.Add(voteResponse.badBot);

    //            newEntries.Add(rowToAppend);

    //            ValueRange requestBody = new();
    //            requestBody.Values = newEntries;

    //            SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum VIO = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;

    //            SpreadsheetsResource.ValuesResource.UpdateRequest request2 = _service.Spreadsheets.Values.Update(requestBody, _spreadsheetId, _botVoteTrackerRange);
    //            request2.ValueInputOption = VIO;

    //            Data.UpdateValuesResponse response2 = request2.Execute();



    //            return voteResponse;
    //        }
    //        public VoteResponse QueryGoodGob()
    //        {
    //            // 1 vote for good gob
    //            VoteResponse voteResponse = QueryGobVotes(1, 0);

    //            return voteResponse;
    //        }
    //        public VoteResponse QueryBadGob()
    //        {
    //            // 1 votes for bad gob
    //            VoteResponse voteResponse = QueryGobVotes(0, 1);

    //            return voteResponse;
    //        }
    //        private VoteResponse QueryGobVotes(int upvoteInput = 0, int downvoteInput = 0)
    //        {
    //            VoteResponse voteResponse = new();

    //            // Download part
    //            SpreadsheetsResource.ValuesResource.GetRequest request = _service.Spreadsheets.Values.Get(_spreadsheetId, _gobVoteTrackerRange);
    //            ValueRange response = request.Execute();
    //            IList<IList<object>> values = response.Values;

    //            if (values != null && values.Count > 0)
    //            {
    //                foreach (IList<object> row in values)
    //                {
    //                    voteResponse.version = row[Version].ToString();
    //                    voteResponse.goodBot = int.Parse(row[GoodGobVotes].ToString());
    //                    voteResponse.badBot = int.Parse(row[BadGobVotes].ToString());
    //                }
    //            }
    //            else
    //            {
    //                return null;
    //            }

    //            // Apply the new vote(s)
    //            voteResponse.goodBot += upvoteInput;
    //            voteResponse.badBot += downvoteInput;

    //            // Upload part
    //            List<IList<object>> newEntries = new();
    //            List<object> rowToAppend = new();
    //            rowToAppend.Add(voteResponse.version.ToString());
    //            rowToAppend.Add(null);
    //            rowToAppend.Add(null);
    //            rowToAppend.Add(voteResponse.goodBot);
    //            rowToAppend.Add(voteResponse.badBot);

    //            newEntries.Add(rowToAppend);

    //            ValueRange requestBody = new();
    //            requestBody.Values = newEntries;

    //            SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum VIO = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;

    //            SpreadsheetsResource.ValuesResource.UpdateRequest request2 = _service.Spreadsheets.Values.Update(requestBody, _spreadsheetId, _gobVoteTrackerRange);
    //            request2.ValueInputOption = VIO;

    //            Data.UpdateValuesResponse response2 = request2.Execute();



    //            return voteResponse;
    //        }

    //        public void UploadBugReport(string discordTag, string report, string timestamp)
    //        {
    //            List<IList<object>> newEntries = new();
    //            List<object> rowToAppend = new();
    //            rowToAppend.Add(discordTag);
    //            rowToAppend.Add(report);
    //            rowToAppend.Add(timestamp);
    //            rowToAppend.Add("Received");

    //            newEntries.Add(rowToAppend);

    //            ValueRange requestBody = new();
    //            requestBody.Values = newEntries;

    //            SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum VIO = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.RAW;
    //            SpreadsheetsResource.ValuesResource.AppendRequest.InsertDataOptionEnum IDO = SpreadsheetsResource.ValuesResource.AppendRequest.InsertDataOptionEnum.INSERTROWS;

    //            SpreadsheetsResource.ValuesResource.AppendRequest request2 = _service.Spreadsheets.Values.Append(requestBody, _spreadsheetId, _bugReportRange);
    //            request2.ValueInputOption = VIO;
    //            request2.InsertDataOption = IDO;

    //            Data.AppendValuesResponse response2 = request2.Execute();
    //        }

    //        public void UploadSuggestion(string discordTag, string suggestionText, string timestamp)
    //        {
    //            List<IList<object>> newEntries = new();
    //            List<object> rowToAppend = new();
    //            rowToAppend.Add(discordTag);
    //            rowToAppend.Add(suggestionText);
    //            rowToAppend.Add(timestamp);
    //            rowToAppend.Add("Received");

    //            newEntries.Add(rowToAppend);

    //            ValueRange requestBody = new();
    //            requestBody.Values = newEntries;

    //            SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum VIO = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.RAW;
    //            SpreadsheetsResource.ValuesResource.AppendRequest.InsertDataOptionEnum IDO = SpreadsheetsResource.ValuesResource.AppendRequest.InsertDataOptionEnum.INSERTROWS;

    //            SpreadsheetsResource.ValuesResource.AppendRequest request2 = _service.Spreadsheets.Values.Append(requestBody, _spreadsheetId, _suggestionRange);
    //            request2.ValueInputOption = VIO;
    //            request2.InsertDataOption = IDO;

    //            Data.AppendValuesResponse response2 = request2.Execute();
    //        }

    //        public SortedDictionary<string, float> GetClassifications()
    //        {
    //            SortedDictionary<string, float> output = new();

    //            SpreadsheetsResource.ValuesResource.GetRequest request = _service.Spreadsheets.Values.Get(_spreadsheetId, _classificationRange);
    //            ValueRange response = request.Execute();
    //            IList<IList<object>> values = response.Values;

    //            foreach (IList<object> classification in values)
    //            {
    //                output.Add(classification[0].ToString(), float.Parse(classification[1].ToString()));
    //            }

    //            return output;
    //        }
    //    }




}
