using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Stepframe;


/// <summary>
/// Summary description for Song
/// </summary>
public class Song {
    public static string DataFile = "data/songs.json";

    public string title = String.Empty;
    public string artist = String.Empty;
    public List<Comment> comments;
    public List<Usage> usage = new List<Usage>();
    public int bpm;
    public bool firstSong = false;
    public bool secondSong = false;
    public bool thirdSong = false;
    public bool fourthSong = false;


    public Song() {
        //
        // TODO: Add constructor logic here
        //
    }

    public Song(dynamic DataJSON) {
        this.Update(DataJSON);
    }

    public void Update(dynamic DataJSON) {
        this.title = (DataJSON.title != null) ? DataJSON.title.ToString() : "";
        this.artist = (DataJSON.artist != null) ? DataJSON.artist.ToString() : "";
        this.bpm = (DataJSON.bpm != null) ? Convert.ToInt32(DataJSON.bpm.ToString()) : 0;

        if (DataJSON.comments != null) {
            this.comments = new List<Comment>();
            foreach (dynamic el in DataJSON.comments) {
                this.comments.Add(new Comment(el));
            }
            this.comments.Sort((s1, s2) => s1.date.CompareTo(s2.date));
        }
        if (DataJSON.usage != null) {
            this.usage = new List<Usage>();
            foreach (dynamic el in DataJSON.usage) {
                this.usage.Add(new Usage(el));
            }
            this.usage.Sort((s1, s2) => s1.date.CompareTo(s2.date));
        }
    }

    public List<Song> GetSongs(dynamic SongsJSON) {
        List<Song> songs = new List<Song>();

        if (SongsJSON != null && SongsJSON is Array) {
            foreach (dynamic song in SongsJSON) {
                songs.Add(new Song(song));
            }
        }

        return songs;
    }

    public static void SaveSong(string SongString) {
        string thisDataFile = Common._context.Server.MapPath(Song.DataFile);

        List<Song> songs = Song.GetSongs(JSONHandler.JSONReader.ReadData(thisDataFile));

        if (SongString != String.Empty) {
            dynamic updateSong = JSONHandler.JSONReader.ConvertJSON(SongString);
            foreach (Song song in songs) {
                if (song.title.ToLower() == updateSong.title.ToLower()) {
                    song.Update(updateSong);
                }
            }
        }

        JSONHandler.JSONReader.WriteData(songs, thisDataFile);
    }

    public class Usage {
        public DateTime date;
        public int position;

        public Usage() {

        }

        public Usage(dynamic DataJSON) {
            this.Update(DataJSON);
        }

        public void Update(dynamic DataJSON) {
            this.position = (DataJSON.position != null) ? Convert.ToInt32(DataJSON.position.ToString()) : 0;
            this.date = (DataJSON.date != null) ? Convert.ToDateTime(DataJSON.date.ToString()) : 0;
        }
    }

    public class Comment {
        public DateTime date;
        public string comment;
        public string commenter;

        public Comment() {

        }

        public Comment(dynamic DataJSON) {
            this.Update(DataJSON);
        }

        public void Update(dynamic DataJSON) {
            this.comment = (DataJSON.comment != null) ? DataJSON.comment.ToString() : "";
            this.commenter = (DataJSON.commenter != null) ? DataJSON.commenter.ToString() : "";
            this.date = (DataJSON.date != null) ? Convert.ToDateTime(DataJSON.date.ToString()) : 0;
        }
    }
}