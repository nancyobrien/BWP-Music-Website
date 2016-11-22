using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Web;

/// <summary>
/// Summary description for Utilitiy
/// </summary>
public class Utility
{
	public Utility()
	{
		//
		// TODO: Add constructor logic here
		//
	}


    private static string UserName {
        get {
            return ConfigurationManager.AppSettings.Get("pcoUser");
        }
    }
    private static string Password {
        get {
            return ConfigurationManager.AppSettings.Get("pcoKey");
        }
    }

    public static int NumberOfDays(DateTime startDate, DateTime endDate, DayOfWeek day) {
       List<DateTime> dates = new List<DateTime>();

        for (DateTime dt = startDate; dt < endDate; dt = dt.AddDays(1.0)) {
            dates.Add(dt);
        }

        return dates.Where(d => d.DayOfWeek == day).Count(); 
 
    }

    public static string GetRequest(string url) {
        var client = new WebClient { Credentials = new NetworkCredential(UserName, Password) };
        try {

            return client.DownloadString(url);

        } catch (Exception ex) {
            throw new Exception(ex.Message + " - " + url);
        }
    }

    public static dynamic GetRequestObject(string url) {
        string returnData = GetRequest(url);
        dynamic data = JSONHandler.JSONReader.ConvertJSON(returnData);

        return data;
    }

    public static int RoundAwayFromZero(decimal value) {
        return value >= 0 ? (int)Math.Ceiling(value) : (int)Math.Floor(value);
    }
}