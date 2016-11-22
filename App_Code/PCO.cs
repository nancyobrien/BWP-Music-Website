using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Stepframe;
using System.Net;
using System.Text;
using System.Configuration;
using Newtonsoft.Json;
using System.IO;


/// <summary>
/// Summary description for PCO
/// </summary>
public class PCO {
    // protected static string songDataPath = Stepframe.Common.context.Server.MapPath("/data/songs.json");
    // protected static string leaderDataPath = Stepframe.Common.context.Server.MapPath("/data/leaders.json");
    // private static string servicesDataPath = Stepframe.Common.context.Server.MapPath("/data/plans.json");

    private bool _haveCurrentData = false;


    public int CurrentDatePage = 0;
    public DateTime LastUpdated;
    [JsonIgnore]
    private Campus Location;


    public List<PCO.PCOService> Plans;


    public PCO(Campus location) {
        this.Location = location;
        this.Plans = new List<PCOService>();
        this.GetCurrentData();
    }

    public static List<PCOService> GetServices(Campus location) {
        return GetServices(location, false);
    }

    public static List<PCOService> GetServices(Campus location, bool forceUpdate) {
        int currentFetchPage = 0;
        //int lastValidPage = 0;
        PCO thisPCO = new PCO(location);
        thisPCO.GetCurrentData();
        bool isLastPage = false;
        currentFetchPage = thisPCO.CurrentDatePage;
        while (!isLastPage) {
            dynamic pcoData = Utility.GetRequestObject(thisPCO.Location.ServicesURL.Replace("{recordNo}", currentFetchPage.ToString()));

            if (pcoData != null) {

                if (pcoData.data != null) {
                    isLastPage = (pcoData.data.Count == 0);
                    foreach (dynamic serviceDynamic in pcoData.data) {
                        int serviceID = (serviceDynamic.id != null) ? Convert.ToInt32(serviceDynamic.id.ToString()) : 0;
                        PCOService thisService = thisPCO.GetService(serviceID);
                        if (thisService == null) {
                            thisService = new PCOService(location, serviceDynamic);


                            if (thisService.IsComplete) {
                                thisPCO.Plans.Add(thisService);
                            }

                        } else {
                            if (!thisService.IsConfirmed || forceUpdate) {
                                thisService.Update(serviceDynamic);

                            }
                        }

                        if (thisService.IsPast) {
                            thisPCO.CurrentDatePage = currentFetchPage;
                        } else {
                            isLastPage = true;
                        }

                        if (forceUpdate) {
                            thisPCO.LastUpdated = DateTime.Now;
                            thisPCO.UpdateData();
                        }

                    }
                }

                if (!isLastPage) { currentFetchPage += 25; }
            }

        }
        thisPCO.LastUpdated = DateTime.Now;
        thisPCO.UpdateData();
        return thisPCO.Plans;
    }

    private void SetProperties(dynamic DataJSON) {
        if (this.Plans == null) { this.Plans = new List<PCOService>(); }
        if (DataJSON != null) {
            this.CurrentDatePage = (DataJSON.CurrentDatePage != null) ? Convert.ToInt32(DataJSON.CurrentDatePage.ToString()) : 150;
            this.LastUpdated = (DataJSON.LastUpdated != null) ? Convert.ToDateTime(DataJSON.LastUpdated.ToString()) : null;

            if (DataJSON.Plans != null) {
                foreach (dynamic plan in DataJSON.Plans) {
                    this.Plans.Add(new PCOService(this.Location, plan, true));
                }
            }
        }

    }

    public void GetCurrentData() {
        if (!_haveCurrentData) {
            this.SetProperties(JSONHandler.JSONReader.ReadData(this.Location.ServicesDataPath));
            _haveCurrentData = true;
        }
    }

    public PCOService GetService(int id) {
        PCOService checkService = this.Plans.Find(p => p.ID == id);

        return checkService;
    }

    public bool ServiceExists(int id) {
        PCOService checkService = GetService(id);

        if (checkService != null && checkService.IsComplete) {
            return true;
        } else {
            return false;
        }
    }

    public void UpdateData() {
        StreamWriter sw = File.CreateText(this.Location.ServicesDataPath);
        sw.Write(JsonConvert.SerializeObject(this));
        sw.Close();
    }



    public class PCOService {
        private dynamic _serviceData = null;
        private bool _isComplete = false;
        public int ID;
        public DateTime Date;
        public string URL;
        [JsonIgnore]
        public Campus Location;
        private string ItemsURL;
        private string TeamURL;
        public int WorshipLeaderID;
        [JsonIgnore]
        public List<TeamMember> Leaders;

        public List<int> LeaderIDs {
            get {
                List<int> leadIDs = null;
                if (Leaders != null && Leaders.Count > 0) {
                    leadIDs = new List<int>();
                    foreach (TeamMember mem in Leaders) {
                        leadIDs.Add(mem.ID);
                    }
                }
                return leadIDs;
            }
        }

        [JsonIgnore]
        public PCOLeader WorshipLeader {
            get {
                if (WorshipLeaderID > 0) {
                    return new PCOLeader(this.WorshipLeaderID);
                } else {
                    return null;
                }
            }
        }
        public List<PCOItem> Songs;


        public bool IsConfirmed { get; set; }

        public bool IsComplete {
            get {
                if (!_isComplete && (this.IsPast || this.Songs.Count > 1)) {
                    _isComplete = true;
                }
                return _isComplete;
            }
            set {
                _isComplete = value;
            }
        }

        public bool IsPast {
            get {
                return (this.Date < DateTime.Now);
            }

        }

        public PCOService(Campus location) {
            this.Location = location;
            this.Songs = new List<PCOItem>();
        }

        public PCOService(Campus location, dynamic DataJSON, Boolean loadFromFile) {
            this.Location = location;
            this.Songs = new List<PCOItem>();
            if (loadFromFile) {
                this.UpdateFromFile(DataJSON);

            } else {
                this.Update(DataJSON);

            }
        }
        public PCOService(Campus location, dynamic DataJSON) {
            this.Location = location;
            this.Songs = new List<PCOItem>();
            this.Update(DataJSON);
        }

        public void UpdateFromFile(dynamic DataJSON) {
            this.URL = (DataJSON.URL != null) ? DataJSON.URL.ToString() : "";
            this.Date = (DataJSON.Date != null) ? Convert.ToDateTime(DataJSON.Date.ToString()) : "";
            this.ID = (DataJSON.ID != null) ? Convert.ToInt32(DataJSON.ID.ToString()) : 0;
            this.IsComplete = (DataJSON.IsComplete != null) ? Convert.ToBoolean(DataJSON.IsComplete.ToString()) : false;
            this.IsConfirmed = (DataJSON.IsConfirmed != null) ? Convert.ToBoolean(DataJSON.IsConfirmed.ToString()) : false;
            this.WorshipLeaderID = (DataJSON.WorshipLeaderID != null) ? Convert.ToInt32(DataJSON.WorshipLeaderID.ToString()) : 0;
            if (DataJSON.Songs != null) {
                LoadSongs(DataJSON.Songs);
            }
        }

        public void Update(dynamic DataJSON) {
            this.URL = (DataJSON.links != null && DataJSON.links.self != null) ? DataJSON.links.self.ToString() : "";
            this.Date = (DataJSON.attributes != null && DataJSON.attributes.dates != null) ? Convert.ToDateTime(DataJSON.attributes.dates.ToString()) : "";
            this.ID = (DataJSON.id != null) ? Convert.ToInt32(DataJSON.id.ToString()) : 0;
            GetTeam();
            GetLeader();
            GetSongs();
            this.IsConfirmed = this.IsPast;
        }

        public void LoadSongs(dynamic DataJSON) {
            if (DataJSON != null) {

                foreach (dynamic song in DataJSON) {
                    this.Songs.Add(new PCOItem(this, song, true));
                }
            }
        }

        public string GetTeamURL() {
            string teamURL = String.Empty;

            if (this.URL != String.Empty) {
                if (_serviceData == null) {
                    _serviceData = Utility.GetRequestObject(this.URL);
                }
                if (_serviceData.data != null && _serviceData.data.links != null) {
                    dynamic links = _serviceData.data.links;

                    if (links.team_member != null) {
                        teamURL = links.team_member.ToString();
                    } else if (links.team_members != null) {
                        teamURL = links.team_members.ToString();
                    }
                }


            }
            return teamURL;
        }

        public void GetTeam() {
            if (this.Leaders != null && this.Leaders.Count > 0) { return; }
            if (this.URL != String.Empty) {
                this.TeamURL = this.GetTeamURL();
                this.Leaders = TeamMember.GetMembers(this.TeamURL);
                /*if (this.TeamURL != String.Empty) {
                    dynamic teamMemberData = Utility.GetRequestObject(this.TeamURL);
                    if (teamMemberData.data != null) {
                        this.Leaders = new List<TeamMember>();
                        foreach (dynamic itemDynamic in teamMemberData.data) {


                        }
                    }
                }*/

            }
        }

        public void GetLeader() {
            if (this.WorshipLeaderID > 0) { return; }
            if (this.URL != String.Empty) {
                this.TeamURL = this.GetTeamURL();
                if (this.TeamURL != String.Empty) {
                    dynamic teamMemberData = Utility.GetRequestObject(this.TeamURL);
                    if (teamMemberData.data != null) {
                        foreach (dynamic itemDynamic in teamMemberData.data) {
                            if (Array.IndexOf(PCOLeader.ValidPositions, itemDynamic.attributes.team_position_name) > -1) {
                                PCOLeader thisLeader = new PCOLeader(itemDynamic);
                                // this.WorshipLeader = new PCOLeader(itemDynamic);
                                this.WorshipLeaderID = thisLeader.ID;
                                break;
                            }
                        }
                    }
                }

            }
        }
        public void GetSongs() {
            this.Songs = new List<PCOItem>();
            if (this.URL != String.Empty) {
                if (_serviceData == null) {
                    _serviceData = Utility.GetRequestObject(this.URL);
                }
                this.ItemsURL = (_serviceData.data.links.items != null) ? _serviceData.data.links.items.ToString() : String.Empty;
                if (this.ItemsURL != String.Empty) {
                    dynamic itemsData = Utility.GetRequestObject(this.ItemsURL);
                    if (itemsData.data != null) {
                        int order = 0;
                        foreach (dynamic itemDynamic in itemsData.data) {
                            if (itemDynamic.attributes.item_type == "song") {
                                order += 1;
                                PCOItem song = new PCOItem(this, itemDynamic);
                                if (this.WorshipLeader != null) {
                                    Location.GetLeader(this.WorshipLeader.ID); //Need to update the list
                                    song.LeaderID = this.WorshipLeader.ID;
                                } else {
                                    string x = "1";
                                    x += "X";
                                }
                                song.Order = order;
                                this.Songs.Add(song);

                            }
                        }
                    }
                }

            }
        }
    }


    public class PCOItem {
        private string _title = String.Empty;
        private string _author = String.Empty;
        private string _ccli = String.Empty;
        private string _copyright = String.Empty;
        private string _admin = String.Empty;
        public int ID;
        public int SongID;
        public string URL;
        private string KeyURL;
        private string SongURL;
        public string Key;
        public string Description;
        public string VocalDescription;
        public string FoundLeader;
        public int LeaderID;
        public int Order;
        [JsonIgnore]
        public PCOService Service;

        [JsonIgnore]
        public string SongTitle {
            get {
                if (_title == String.Empty) {
                    PCOSong thisSong = Service.Location.GetSong(this.SongID);
                    if (thisSong != null) {
                        _title = thisSong.Title;
                    }
                }
                return _title;
            }
        }

        [JsonIgnore]
        public string SongAuthor {
            get {
                if (_author == String.Empty) {
                    PCOSong thisSong = Service.Location.GetSong(this.SongID);
                    if (thisSong != null) {
                        _author = thisSong.Author;
                    }
                }
                return _author;
            }
        }

        [JsonIgnore]
        public string CCLINumber {
            get {
                if (_ccli == String.Empty) {
                    PCOSong thisSong = Service.Location.GetSong(this.SongID);
                    if (thisSong != null) {
                        _ccli = thisSong.CCLINumber;
                    }
                }
                return _ccli;
            }
        }


        [JsonIgnore]
        public string Copyright {
            get {
                if (_copyright == String.Empty) {
                    PCOSong thisSong = Service.Location.GetSong(this.SongID);
                    if (thisSong != null) {
                        _copyright = thisSong.Copyright;
                    }
                }
                return _copyright;
            }
        }

        [JsonIgnore]
        public string Administrator {
            get {
                if (_admin == String.Empty) {
                    PCOSong thisSong = Service.Location.GetSong(this.SongID);
                    if (thisSong != null) {
                        _admin = thisSong.Admin;
                    }
                }
                return _admin;
            }
        }

        public string ServiceDate {
            get {
                return this.Service.Date.ToString("MM/dd/yyyy");
            }
        }

        public string LeaderName {
            get {
                if (this.LeaderID > 0) {
                    return this.Service.Location.GetLeader(this.LeaderID).Name;
                } else {
                    return String.Empty;

                }
            }
        }

        public PCOItem() {

        }

        public PCOItem(PCOService service, dynamic DataJSON) {
            this.Service = service;
            this.Update(DataJSON);
        }


        public PCOItem(PCOService service, dynamic DataJSON, Boolean loadFromFile) {
            this.Service = service;
            if (loadFromFile) {
                this.UpdateFromFile(DataJSON);

            } else {
                this.Update(DataJSON);

            }
        }
        public void UpdateFromFile(dynamic DataJSON) {
            this.URL = (DataJSON.URL != null) ? DataJSON.URL.ToString() : "";
            this.ID = (DataJSON.ID != null) ? Convert.ToInt32(DataJSON.ID.ToString()) : 0;
            this.LeaderID = (DataJSON.LeaderID != null) ? Convert.ToInt32(DataJSON.LeaderID.ToString()) : 0;
            this.SongID = (DataJSON.SongID != null) ? Convert.ToInt32(DataJSON.SongID.ToString()) : 0;
            this.Key = (DataJSON.Key != null) ? DataJSON.Key.ToString() : "";

            this.Order = (DataJSON.Order != null) ? Convert.ToInt32(DataJSON.Order.ToString()) : 0;
        }

        public void Update(dynamic DataJSON) {
            this.URL = (DataJSON.links != null && DataJSON.links.self != null) ? DataJSON.links.self.ToString() : String.Empty;
            this.ID = (DataJSON.id != null) ? Convert.ToInt32(DataJSON.id.ToString()) : 0;
            this.Description = (DataJSON.attributes != null && DataJSON.attributes.description != null) ? DataJSON.attributes.description.ToString() : String.Empty;
            this.SongID = (DataJSON.attributes.song_id != null) ? Convert.ToInt32(DataJSON.attributes.song_id.ToString()) : 0;
            if (this.SongID == 0) {
                this.SongID = (DataJSON.relationships != null && DataJSON.relationships.song != null && DataJSON.relationships.song.data != null && DataJSON.relationships.song.data.id != null) ? Convert.ToInt32(DataJSON.relationships.song.data.id.ToString()) : 0;
            }
            GetNotes();
            GetDetails();
            FindLeader();
        }

        public void GetNotes() {
            if (this.URL != String.Empty) {
                dynamic itemData = Utility.GetRequestObject(this.URL + "/item_notes");

                foreach (dynamic note in itemData.data) {
                    if (note.type == "ItemNote") {
                        if (note.attributes != null && note.attributes.category_name != null && note.attributes.category_name.ToString() == "Vocals") {
                            this.VocalDescription = note.attributes.content.ToString();
                        }
                    }
                }
            }
        }

        public void GetDetails() {
            if (this.URL != String.Empty) {
                dynamic itemData = Utility.GetRequestObject(this.URL);
                this.KeyURL = (itemData.data.links.key != null) ? itemData.data.links.key.ToString() : String.Empty;
                this.SongURL = (itemData.data.links.song != null) ? itemData.data.links.song.ToString() : String.Empty;
                if (this.KeyURL != String.Empty) {
                    dynamic keyData = Utility.GetRequestObject(this.KeyURL);
                    this.Key = (keyData.data != null && keyData.data.attributes != null && keyData.data.attributes.starting_key != null) ? keyData.data.attributes.starting_key.ToString() : "";
                }

                Service.Location.GetSong(this.SongID);
            }
        }


        public void FindLeader() {
            TeamMember thisLeader = null;
            string leaderName = String.Empty;
            if (this.Service.Leaders.Count == 0) { return; }
            if (this.VocalDescription != null) {
                leaderName = this.VocalDescription;
            } else if (this.Description != null) {
                leaderName = this.Description.Replace(" leads", String.Empty).Replace("-", String.Empty).Trim();
            }

            foreach (TeamMember mem in this.Service.Leaders) {
                if (mem.Name.Contains(leaderName)) { thisLeader = mem; }
            }

            if (thisLeader != null) {
                this.FoundLeader = thisLeader.Name;

            }

        }
    }

    public class PCOLeader {
        //public static string[] ValidPositions = { "Worship Leader", "Female bVox" };
        public static string[] ValidPositions = { "Worship Leader" };

        public static string LeaderPCOURL = "https://api.planningcenteronline.com/services/v2/people/{leaderID}";

        public int ID;
        public string Name;
        public string URL;

        public static string PCOUrl(int leaderID) {
            return LeaderPCOURL.Replace("{leaderID}", leaderID.ToString());
        }


        public PCOLeader() {

        }


        public PCOLeader(int leaderID) {
            this.ID = leaderID;
        }

        public PCOLeader(dynamic DataJSON) {
            this.Update(DataJSON);
        }


        public PCOLeader(dynamic DataJSON, Boolean loadFromFile) {
            if (loadFromFile) {
                this.UpdateFromFile(DataJSON);
            } else {
                this.Update(DataJSON);
            }
        }
        public void UpdateFromFile(dynamic DataJSON) {
            this.URL = (DataJSON.URL != null) ? DataJSON.URL.ToString() : "";
            this.Name = (DataJSON.Name != null) ? DataJSON.Name.ToString() : "";
            this.ID = (DataJSON.ID != null) ? Convert.ToInt32(DataJSON.ID.ToString()) : 0;
        }

        public void Update(dynamic DataJSON) {
            dynamic data = (DataJSON.data != null) ? DataJSON.data : DataJSON;

            if (data != null && data.type != null && (data.type == "Person")) {
                dynamic attributes = (data.attributes != null) ? data.attributes : data;
                this.ID = (data.id != null) ? Convert.ToInt32(data.id) : 0;
                this.URL = (data.links != null && data.links.self != null) ? data.links.self.ToString() : PCOUrl(this.ID);
                this.Name = (attributes.first_name != null && attributes.last_name != null) ? attributes.first_name.ToString() + " " + attributes.last_name.ToString() : String.Empty;

            } else if (data != null && data.type != null && (data.type == "PlanPerson")) {
                dynamic attributes = (data.attributes != null) ? data.attributes : data;
                this.ID = (data.relationships != null && data.relationships.person != null && data.relationships.person.data != null && data.relationships.person.data.id != null) ? Convert.ToInt32(data.relationships.person.data.id) : 0;
                this.URL = (data.links != null && data.links.person != null) ? data.links.person.ToString() : PCOUrl(this.ID);
                this.Name = (attributes.name != null) ? attributes.name.ToString() : String.Empty;

            } else {
                dynamic attributes = (data != null && data.attributes != null) ? data.attributes : ((data.person_id != null) ? data : null);
                this.ID = (attributes != null && attributes.person_id != null) ? Convert.ToInt32(attributes.person_id.ToString()) : 0;
                this.URL = PCOUrl(this.ID);
                this.Name = (attributes.name != null) ? attributes.name.ToString() : String.Empty;

            }
        }


        public static PCO.PCOLeader GetLeader(int leaderID) {
            PCO.PCOLeader newLeader = GetLeader(PCO.PCOLeader.PCOUrl(leaderID));
            return newLeader;
        }


        public static PCO.PCOLeader GetLeader(string leaderURL) {
            PCO.PCOLeader newLeader = null;
            if (leaderURL != String.Empty) {
                newLeader = new PCOLeader(Utility.GetRequestObject(leaderURL));
            }
            return newLeader;
        }

    }

    public class PCOSong {
        private static string songPCOURL = "https://api.planningcenteronline.com/services/v2/songs/{songID}";

        public static string PCOUrl(int songID) {
            return songPCOURL.Replace("{songID}", songID.ToString());
        }

        public int ID;
        public string Title;
        public string URL;
        public string Copyright;
        public string Author;
        public int DefaultOrder = 0;
        public bool ExcludeFromRotation = false;
        public string CCLINumber = String.Empty;
        public string[] Tags;
        public string Admin = String.Empty;

        public static PCOSong GetSong(int songID) {
            PCO.PCOSong newSong = GetSong(PCO.PCOSong.PCOUrl(songID));
            return newSong;
        }

        public static PCOSong GetSong(string songURL) {
            PCOSong newSong = null;
            if (songURL != String.Empty) {
                newSong = new PCOSong(Utility.GetRequestObject(songURL));
            }
            return newSong;
        }

        public PCOSong() {

        }

        public PCOSong(dynamic DataJSON) {
            this.Update(DataJSON);
        }


        public PCOSong(dynamic DataJSON, Boolean loadFromFile) {
            if (loadFromFile) {
                this.UpdateFromFile(DataJSON);
            } else {
                this.Update(DataJSON);
            }
        }
        public void UpdateFromFile(dynamic DataJSON) {
            List<object> tmpTags = (DataJSON.Tags != null) ? DataJSON.Tags : null;
            this.URL = (DataJSON.URL != null) ? DataJSON.URL.ToString() : String.Empty;
            this.Title = (DataJSON.Title != null) ? DataJSON.Title.ToString() : String.Empty;
            this.ID = (DataJSON.ID != null) ? Convert.ToInt32(DataJSON.ID.ToString()) : 0;
            this.Copyright = (DataJSON.Copyright != null) ? DataJSON.Copyright.ToString() : String.Empty;
            this.Admin = (DataJSON.Admin != null) ? DataJSON.Admin.ToString() : String.Empty;
            this.Author = (DataJSON.Author != null) ? DataJSON.Author.ToString() : "";
            this.ExcludeFromRotation = (DataJSON.ExcludeFromRotation != null) ? Convert.ToBoolean(DataJSON.ExcludeFromRotation.ToString()) : false;
            this.CCLINumber = (DataJSON.CCLINumber != null) ? DataJSON.CCLINumber.ToString() : String.Empty;
            this.Tags = (tmpTags != null) ? tmpTags.Select(i => i.ToString()).ToArray() : null;

            this.DefaultOrder = (DataJSON.DefaultOrder != null) ? Convert.ToInt32(DataJSON.DefaultOrder.ToString()) : 0;
        }

        public void Update(dynamic DataJSON) {
            dynamic data = DataJSON.data;
            dynamic attributes = (DataJSON.data != null && DataJSON.data.attributes != null) ? DataJSON.data.attributes : null;
            string tmpTags = (attributes.themes != null) ? (attributes.themes).ToString() : String.Empty;
            this.ID = (data.id != null) ? Convert.ToInt32(data.id.ToString()) : 0;
            this.URL = (data.links != null && data.links.self != null) ? data.links.self.ToString() : "";
            this.Title = (attributes.title != null) ? attributes.title.ToString() : String.Empty;
            this.Copyright = (attributes.copyright != null) ? attributes.copyright.ToString() : String.Empty;
            this.Admin = (attributes.admin != null) ? attributes.admin.ToString() : String.Empty;
            this.Author = (attributes.author != null) ? attributes.author.ToString() : String.Empty;
            this.CCLINumber = (attributes.ccli_number != null) ? attributes.ccli_number.ToString() : String.Empty;
            if (tmpTags != string.Empty) {
                this.Tags = tmpTags.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()).ToArray();
            }
        }

    }

    public class PCOUsage {
        public string lastUpdated;
        public string startDate;
        public string title;
        public string artist;
        public string ccliNumber;
        public int songID;
        [JsonIgnore]
        public Campus Location;
        public int useCount {
            get {
                return this.dates.Count;
            }
        }

        public List<DateTime> dates;
        public String lastUsed {
            get {
                return lastUsedDate.ToString("MM/dd/yyyy");
            }
        }
        public DateTime lastUsedDate {
            get {
                return dates.Max();
            }
        }

        public Dictionary<int, int> slots;
        public int preferredSlot {
            get {
                return slots.FirstOrDefault(x => x.Value == slots.Values.Max()).Key;
            }
        }

        public int weeksSince {
            get {
                decimal timeConversion = 7;

                TimeSpan ts = DateTime.Now - dates.Max();
                int differenceInDays = ts.Days;
                return (int)Utility.RoundAwayFromZero(differenceInDays / timeConversion);
            }
        }



        public PCOUsage(Campus location) {
            this.Location = location;
            this.dates = new List<DateTime>();
            intializeSlots();
        }

        public PCOUsage(Campus location, DateTime itemDate, PCOItem songItem, int songSlot) {

            this.Location = location;
            this.dates = new List<DateTime>();
            intializeSlots();
            this.dates.Add(itemDate);
            if (this.slots.ContainsKey(songSlot)) {
                this.slots[songSlot] += 1;

            } else {
                this.slots.Add(songSlot, 1);
            }

            this.title = songItem.SongTitle;
            this.artist = songItem.SongAuthor;
            this.songID = songItem.SongID;
            this.ccliNumber = songItem.CCLINumber;

        }
        private void intializeSlots() {
            this.slots = new Dictionary<int, int>();
            for (int i = 1; i < 6; i++) {
                this.slots.Add(i, 0);
            }
        }

        public void Update(DateTime itemDate, int songSlot) {
            this.dates.Add(itemDate);
            if (this.slots.ContainsKey(songSlot)) {
                this.slots[songSlot] += 1;

            } else {
                this.slots.Add(songSlot, 1);

            }
        }

        public static DateTime DefaultStartDate {
            get {
                DateTime startDate = Convert.ToDateTime("1/1/" + DateTime.Now.Year.ToString());
                TimeSpan ts = DateTime.Now - startDate;
                if (ts.Days < 270) {
                    startDate = startDate.AddYears(-1);
                }
                return startDate;
            }
        }

        public static List<PCOUsage> GetUsage(Campus location) {

            return GetUsage(DefaultStartDate, location);
        }

        public static List<PCOUsage> GetUsage(DateTime startDate, Campus location) {
            List<PCOUsage> allUsage = new List<PCOUsage>();
            PCO thisPCO = location.PCOData;
            //thisPCO.GetCurrentData();
            List<PCOService> planList = thisPCO.Plans.FindAll(s => s.Date >= startDate);
            foreach (PCOService plan in planList) {

                foreach (PCOItem item in plan.Songs) {
                    PCOUsage thisSong = allUsage.Find(s => s.songID == item.SongID);
                    if (thisSong == null) {
                        thisSong = new PCOUsage(location, plan.Date, item, item.Order);
                        thisSong.startDate = startDate.ToString("MM/dd/yyyy");
                        allUsage.Add(thisSong);
                    } else {
                        thisSong.Update(plan.Date, item.Order);
                    }
                    thisSong.lastUpdated = thisPCO.LastUpdated.ToString("MM/dd/yyyy hh:mm tt");

                }

            }

            return allUsage;
        }


    }

    public class PCOSet {
        public static int ExcludeRecent = 0;
        private List<PCOUsage> _songUsage;
        [JsonIgnore]
        public Campus Location;

        public PCOSet(Campus location) {
            this.Location = location;
            GetSongs(location);
        }

        public PCOSet(Campus location, int slot) {
            GetSongs(slot, slot);
        }

        public List<PCOUsage> GetSongs(Campus location) {
            return GetSongs(1, 4);
        }

        public List<PCOUsage> GetSongs(Boolean anySlot) {
            if (!anySlot) {
                return GetSongs(1, 4);
            } else {
                return GetSongs(1, 4, true);
            }

        }

        public List<PCOUsage> GetSongs(int iStart, int iEnd) {
            return GetSongs(1, 4, false);
        }
        public List<PCOUsage> GetSongs(int iStart, int iEnd, bool anySlot) {
            List<PCOUsage> songs = new List<PCOUsage>();
            _songUsage = PCOUsage.GetUsage(Location);

            for (int i = iStart; i <= iEnd; i++) {
                if (anySlot) {
                    songs.Add(PickSong(songs));
                } else {
                    songs.Add(PickSong(i, songs));
                }
            }
            return songs;
        }

        private PCOUsage PickSong(int slot, List<PCOUsage> excludeList) {
            PCOUsage newSong = PickSong(slot);
            if (newSong != null && excludeList.Count(s => s.songID == newSong.songID) > 0) {
                newSong = PickSong(slot, excludeList);
            }
            return newSong;
        }

        private PCOUsage PickSong(List<PCOUsage> excludeList) {
            PCOUsage newSong = PickSong();
            if (excludeList.Count(s => s.songID == newSong.songID) > 0) {
                newSong = PickSong(excludeList);
            }
            return newSong;
        }

        private PCOUsage PickSong(int slot) {
            Random rnd = new Random();

            List<PCOUsage> availableSongs = _songUsage.FindAll(s => s.slots[slot] > 0).FindAll(s => s.weeksSince > ExcludeRecent);
            if (availableSongs.Count > 0) {
                return availableSongs.OrderBy(x => rnd.Next()).First();
            } else {
                return null;
            }
        }


        private PCOUsage PickSong() {
            Random rnd = new Random();
            List<PCOUsage> availableSongs = _songUsage.FindAll(s => s.weeksSince > ExcludeRecent);

            return availableSongs.OrderBy(x => rnd.Next()).First();
        }
    }
}