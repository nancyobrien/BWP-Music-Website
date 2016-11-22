<%@ WebHandler Language="C#" Class="Venues" %>

using System;
using System.Web;

public class Venues : IHttpHandler {
    
    public void ProcessRequest (HttpContext context) {
        context.Response.ContentType = "text/plain";
       // context.Response.Write(Newtonsoft.Json.JsonConvert.SerializeObject(PlanningCenter.Venue.GetVenues()));
        context.Response.Write(Newtonsoft.Json.JsonConvert.SerializeObject(PlanningCenter.Folder.GetFolders()));

    }
 
    public bool IsReusable {
        get {
            return false;
        }
    }

}