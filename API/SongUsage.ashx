<%@ WebHandler Language="C#" Class="SongUsage" %>

using System;
using System.Web;

public class SongUsage : IHttpHandler {

    public void ProcessRequest(HttpContext context) {
        bool forceRefresh = false;
        context.Response.ContentType = "text/plain";

        string locationID = "richland";
        if (Stepframe.Common.GetRequestVar("location") != String.Empty) {
            locationID = Stepframe.Common.GetRequestVar("location");
        }
        if (Stepframe.Common.GetRequestVar("forceRefresh") == "true") {
            forceRefresh = true;
        }
        
        Campus location = new Campus(locationID);
        if (location.OutOfDate || forceRefresh) {
            location.RefreshData();
        }


        DateTime startDate = PCO.PCOUsage.DefaultStartDate;
        if (Stepframe.Common.GetRequestVar("startdate") != String.Empty) {
            DateTime.TryParse(context.Request["startdate"], out startDate);
        }
        context.Response.Write(Newtonsoft.Json.JsonConvert.SerializeObject(PCO.PCOUsage.GetUsage(startDate, location)));

    }

    public bool IsReusable {
        get {
            return false;
        }
    }

}