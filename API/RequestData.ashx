<%@ WebHandler Language="C#" Class="RequestData" %>

using System;
using System.Web;
using Newtonsoft.Json;

public class RequestData : IHttpHandler {
    
    public void ProcessRequest (HttpContext context) {
        context.Response.ContentType = "text/plain";
        string locationID = "richland";
        bool forceUpdate = false;
        if (Stepframe.Common.GetRequestVar("forceUpdate") != String.Empty) {
            forceUpdate = true;
        }
        
        if (Stepframe.Common.GetRequestVar("location") != String.Empty) {
            locationID = Stepframe.Common.GetRequestVar("location");
        }
        Campus location = new Campus(locationID);
        context.Response.Write(JsonConvert.SerializeObject(PCO.GetServices(location, forceUpdate)));
    }
 
    public bool IsReusable {
        get {
            return false;
        }
    }

}