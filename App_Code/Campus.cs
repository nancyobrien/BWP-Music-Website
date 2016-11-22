using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web;

/// <summary>
/// Summary description for Campus
/// </summary>
public class Campus {
    private string _dataPath {
        get {
            if (!Directory.Exists(Stepframe.Common.context.Server.MapPath("/data/" + locationCode))) {
                Directory.CreateDirectory(Stepframe.Common.context.Server.MapPath("/data/" + locationCode));
            }
            return "/data/" + locationCode;
        }
    }

    private List<PCO.PCOService> _serviceList;
    private List<PCO.PCOLeader> _leaderList;
    private List<PCO.PCOSong> _songList;
    private PCO _pco;

    public PCO PCOData {
        get {
            if (_pco == null) {
                _pco = new PCO(this);
            }
            return _pco;
        }
    }

    private string locationCode = "richland";

    public string ID {
        get {
            return ConfigurationManager.AppSettings.Get(locationCode);
        }
    }

    private static string ServicesURLBase = "https://api.planningcenteronline.com/services/v2/service_types/{locationID}/plans?offset={recordNo}";


    public string ServicesURL {
        get {
            return ServicesURLBase.Replace("{locationID}", this.ID);
        }
    }
    public string SongDataPath {
        get {
            return Stepframe.Common.context.Server.MapPath(_dataPath + "/songs.json");
        }
    }
    public string LeaderDataPath {
        get {
            return Stepframe.Common.context.Server.MapPath(_dataPath + "/leaders.json");
        }
    }

    public string ServicesDataPath {
        get {
            return Stepframe.Common.context.Server.MapPath(_dataPath + "/plans.json");
        }
    }

    public DateTime LastUpdated {
        get {
            return PCOData.LastUpdated;
        }
    }

    public Boolean OutOfDate {
        get {
            int numSundays = Utility.NumberOfDays(PCOData.LastUpdated, DateTime.Now, DayOfWeek.Sunday);
            //TimeSpan ts = DateTime.Now - PCOData.LastUpdated;
            return (numSundays > 0);
        }

    }

    public Campus(string campusName) {
        this.locationCode = campusName;
    }

    public void RefreshData() {
        PCO.GetServices(this);
    }

    #region Services

    public List<PCO.PCOService> AllServices {
        get {
            if (_serviceList == null) {
                PCOData.GetCurrentData();
                _serviceList = PCOData.Plans;
            }
            return _serviceList;
        }
    }

    public List<PCO.PCOService> GetAllServices(DateTime startDate, DateTime endDate) {
        List<PCO.PCOService> services = this.AllServices.Where(d => d.Date >= startDate && d.Date <= endDate).ToList();
        return services;
    }

    #endregion

    #region Leaders

    public List<PCO.PCOLeader> AllLeaders {
        get {
            if (_leaderList == null) {
                _leaderList = GetAllLeaders();
            }
            return _leaderList;
        }
    }

    public List<PCO.PCOLeader> GetAllLeaders() {
        _leaderList = new List<PCO.PCOLeader>();
        dynamic DataJSON = JSONHandler.JSONReader.ReadData(LeaderDataPath);
        if (DataJSON != null) {
            try {
                foreach (dynamic leader in DataJSON) {
                    _leaderList.Add(new PCO.PCOLeader(leader, true));
                }

            } catch (Exception) {

            }
        }
        return _leaderList;
    }

    public PCO.PCOLeader GetLeader(int leaderID) {
        PCO.PCOLeader newLeader = AllLeaders.Find(s => s.ID == leaderID);
        if (newLeader == null) {
            newLeader = PCO.PCOLeader.GetLeader(leaderID);
            if (newLeader != null) {
                _leaderList.Add(newLeader);
                UpdateLeaders();
            }
        }
        return newLeader;
    }

    public void UpdateLeaders() {
        WriteData(LeaderDataPath, AllLeaders);
    }


    #endregion


    #region Songs
    private Dictionary<int, PCO.PCOSong> _songDict;
    public Dictionary<int, PCO.PCOSong> AllSongs {
        get {
            if (_songList == null) {
                _songDict = GetAllSongs();
            }
            return _songDict;
        }
    }
    public Dictionary<int, PCO.PCOSong> GetAllSongs() {
        _songList = new List<PCO.PCOSong>();
        _songDict = new Dictionary<int, PCO.PCOSong>();
        dynamic DataJSON = ReadData(this.SongDataPath);

        if (DataJSON != null) {
            try {
                foreach (dynamic song in DataJSON) {
                    PCO.PCOSong newSong = new PCO.PCOSong(song, true);
                    _songList.Add(newSong);
                    _songDict.Add(newSong.ID, newSong);
                }
            } catch (Exception) {

            }
        }
        return _songDict;
    }

 

    public List<PCO.PCOItem> GetSongHistory(string songIDString, DateTime startDate) {
        int songID = 0;
        Int32.TryParse(songIDString, out songID);

        //List<PCO.PCOItem> songsPlayed = AllServices.Where(s => s.Songs.Find(p => p.SongID == songID));

        List<PCO.PCOItem> songsPlayed = AllServices.Where(d => d.Date >= startDate).SelectMany(q => q.Songs).Where(a => a.SongID == songID).ToList();
        songsPlayed.Reverse();
        return songsPlayed;
    }

    public PCO.PCOSong GetSong(int songiD) {
        PCO.PCOSong newSong = AllSongs.ContainsKey(songiD) ? AllSongs[songiD] : null; //AllSongs.Find(s => s.ID == songiD);
        if (newSong == null) {
            newSong = PCO.PCOSong.GetSong(songiD);
            if (newSong != null) {
                _songList.Add(newSong);
                _songDict.Add(newSong.ID, newSong);
                UpdateSongs();
            }
        }
        return newSong;
    }
    public void UpdateSongs() {
        WriteData(SongDataPath, _songList);
    }
    #endregion


    #region RandomSet

    public List<PCO.PCOUsage> GetRandomSongs() {
        PCO.PCOSet set = new PCO.PCOSet(this);
        return set.GetSongs(1, 4);
    }

    #endregion

    #region SongUsage
    public List<PCO.PCOUsage> GetUsage() {
        return PCO.PCOUsage.GetUsage(this);
    }
    #endregion


    public static dynamic ReadData(string dataPath) {
        return JSONHandler.JSONReader.ReadData(dataPath);
    }

    public static void WriteData(string dataPath, object dataObject) {
        StreamWriter sw = File.CreateText(dataPath);
        sw.Write(JsonConvert.SerializeObject(dataObject));
        sw.Close();
    }
}