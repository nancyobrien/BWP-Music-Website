using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

/// <summary>
/// Summary description for PCOValidation
/// </summary>
public class PCOValidation {

    protected static string plansSample = Stepframe.Common.context.Server.MapPath("/data/sample/plans.json");

    public PCOValidation() {
        //
        // TODO: Add constructor logic here
        //
    }

    public static Boolean ValidateServices(dynamic servicesList) {
        dynamic samplePlan = ReadData(plansSample);
        Dictionary<string, object> sampleProps = GetProperties(samplePlan);
        Dictionary<string, object> actualProps = GetProperties(servicesList);

        foreach (KeyValuePair<string, object> prop in sampleProps) {
            int x = 1;
        }

        return true;
    }


    public static dynamic ReadData(string dataPath) {
        return JSONHandler.JSONReader.ReadData(dataPath);
    }

    public static Dictionary<string, object> GetProperties(dynamic dyno) {
        Dictionary<string, object> props = new Dictionary<string, object>();

        object o = dyno;
        var propList = o.GetType().GetProperties();
        string[] propertyNames = o.GetType().GetProperties().Select(p => p.Name).ToArray();
        foreach (var prop in propertyNames) {
            object propValue = o.GetType().GetProperty(prop).GetValue(o, null);
            props.Add(prop, propValue);
        }

        return props;


    }
}