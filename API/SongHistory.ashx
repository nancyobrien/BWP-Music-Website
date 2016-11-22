<%@ WebHandler Language="C#" Class="SongHistory" %>

using System;
using System.Web;

public class SongHistory : IHttpHandler {
    
    public void ProcessRequest (HttpContext context) {
        context.Response.ContentType = "text/plain";
        string locationID = "richland";
        if (Stepframe.Common.GetRequestVar("location") != String.Empty) {
            locationID = Stepframe.Common.GetRequestVar("location");
        }
        Campus location = new Campus(locationID);

        DateTime startDate = PCO.PCOUsage.DefaultStartDate;
        if (Stepframe.Common.GetRequestVar("startdate") != String.Empty) {
            DateTime.TryParse(context.Request["startdate"], out startDate);
        }

        context.Response.Write(Newtonsoft.Json.JsonConvert.SerializeObject(location.GetSongHistory(Stepframe.Common.GetRequestVar("songID"), startDate)));
    }
 
    public bool IsReusable {
        get {
            return false;
        }
    }

}