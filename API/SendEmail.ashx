<%@ WebHandler Language="C#" Class="SendEmail" %>

using System;
using System.Web;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;

public class SendEmail : IHttpHandler {

    public void ProcessRequest(HttpContext context) {
        context.Response.ContentType = "text/plain";
 
        if (ConfigurationManager.AppSettings.Get("trelloEmail") != null) {
            try {
            string trelloEmail = ConfigurationManager.AppSettings.Get("trelloEmail");


            string subject = Stepframe.Common.GetRequestVar("label");
            if (subject == String.Empty) { subject = "Feedback"; }
            subject += " - " + DateTime.Now.ToString("MM/dd/yyyy") + " #feedback";

            string message = Stepframe.Common.GetRequestVar("message");
            string botCatcher = Stepframe.Common.GetRequestVar("tripper");

            if (subject != String.Empty && botCatcher == String.Empty) {
                Stepframe.Emailer email = new Stepframe.Emailer("feedback@bwpmusic.org", trelloEmail, subject, message);
                email.send();

            }
            } catch (Exception ex) {
                context.Response.Write("{\"errNo\": 200, \"status\": \"" + ex.Message + "\"}");
            }


            context.Response.Write("{\"errNo\": 0, \"status\": \"success\"}");
        } else {
            context.Response.Write("{\"errNo\": 100, \"status\": \"Email not configured\"}");
        }
    }

    public bool IsReusable {
        get {
            return false;
        }
    }

}