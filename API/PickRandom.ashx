<%@ WebHandler Language="C#" Class="PickRandom" %>

using System;
using System.Web;

public class PickRandom : IHttpHandler {
    
    public void ProcessRequest (HttpContext context) {
        context.Response.ContentType = "text/plain";
        string locationID = "richland";
        if (Stepframe.Common.GetRequestVar("location") != String.Empty) {
            locationID = Stepframe.Common.GetRequestVar("location");
        }
        Campus location = new Campus(locationID);

        context.Response.Write(Newtonsoft.Json.JsonConvert.SerializeObject(location.GetRandomSongs()));
    }
 
    public bool IsReusable {
        get {
            return false;
        }
    }

}