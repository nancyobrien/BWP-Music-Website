<%@ WebHandler Language="C#" Class="RequestData" %>

using System;
using System.Web;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Data;

public class RequestData : IHttpHandler {

    public void ProcessRequest(HttpContext context) {
        //context.Response.ContentType = "text/plain";


        DateTime startDate = PCO.PCOUsage.DefaultStartDate;
        if (Stepframe.Common.GetRequestVar("startdate") != String.Empty) {
            DateTime.TryParse(context.Request["startdate"], out startDate);
        }

        DateTime endDate = DateTime.Now;
        if (Stepframe.Common.GetRequestVar("endDate") != String.Empty) {
            DateTime.TryParse(context.Request["endDate"], out endDate);
        }


        context.Response.Clear();
        context.Response.Buffer = true;
        context.Response.ContentType = "application/vnd.ms-excel";
        context.Response.AddHeader("content-disposition", "attachment;filename=BethelSongHistory.xls");
        context.Response.Charset = "";

        Dictionary<string, string> locations = new Dictionary<string, string>();

        locations.Add("richland", "LIVE");
        locations.Add("gallery", "Gallery");
        locations.Add("sundaypm", "PM");
        locations.Add("intersectAM", "iNTERSECT AM");
        locations.Add("intersectPM", "iNTERSECT PM");
        locations.Add("secondSat", "Young Adults Second Sat");
        locations.Add("pasco", "West Pasco");

        string sheetsText = String.Empty;
        foreach (KeyValuePair<string, string> loc in locations) {


            string locationID = loc.Key;


            Campus location = new Campus(locationID);

            List<PCO.PCOService> currentServices = location.GetAllServices(startDate, endDate);

            DataTable dt = new DataTable();
            DataSet ds = new DataSet();

            dt.TableName = location.ID;
            //Create all the Columns
            DataColumn dcTitle = dt.Columns.Add("Title", typeof(String));

            Hashtable titleRowHash = new Hashtable();
            string titleRowString = String.Empty;
            Hashtable cellHash1 = new Hashtable();
            cellHash1.Add("cellStyle", "ssColumnTitle");
            cellHash1.Add("dataType", "String");
            cellHash1.Add("dataValue", "Date");
            titleRowString += Stepframe.MoustacheTemplate.ApplyTemplate(context.Server.MapPath("/templates/excelCell.txt"), cellHash1);


            foreach (PCO.PCOService service in currentServices) {
                if (service.Songs.Count > 0) {
                    DataColumn dc = dt.Columns.Add(service.ID.ToString(), typeof(String));
                    Hashtable cellHash = new Hashtable();
                    cellHash.Add("cellStyle", "ssColumnTitle");
                    cellHash.Add("dataType", "String");
                    cellHash.Add("dataValue", service.Date.ToString("MM/dd/yyyy"));
                    titleRowString += Stepframe.MoustacheTemplate.ApplyTemplate(context.Server.MapPath("/templates/excelCell.txt"), cellHash);
                }
            }
            titleRowHash.Add("DataCells", titleRowString);

            int rowOffset = 0;
            foreach (PCO.PCOService service in currentServices) {
                if (service.Songs.Count > 0) {

                    string colName = service.ID.ToString();
                    int iBlock = 0;
                    foreach (PCO.PCOItem item in service.Songs) {
                        //6 rows per song (Title, author, ccli#, copyright, admin, space)
                        iBlock += 1;
                        int titleRowID = (iBlock - 1) * 6 + 0 + rowOffset;
                        int authorRowID = (iBlock - 1) * 6 + 1 + rowOffset;
                        int ccliRowID = (iBlock - 1) * 6 + 2 + rowOffset;
                        int copyrightRowID = (iBlock - 1) * 6 + 3 + rowOffset;
                        int adminRowID = (iBlock - 1) * 6 + 4 + rowOffset;
                        int spaceRowID = (iBlock - 1) * 6 + 5 + rowOffset;
                        if (dt.Rows.Count < iBlock * 6) {
                            //Create rows if not already there, and title them
                            DataRow titleRow = dt.NewRow();
                            titleRow["Title"] = "Song";
                            DataRow authorRow = dt.NewRow();
                            authorRow["Title"] = "Author";
                            DataRow ccliRow = dt.NewRow();
                            ccliRow["Title"] = "CCLI #";
                            DataRow copyrightRow = dt.NewRow();
                            copyrightRow["Title"] = "Copyright";
                            DataRow adminRow = dt.NewRow();
                            adminRow["Title"] = "Admin";
                            DataRow spaceRow = dt.NewRow();
                            spaceRow["Title"] = "";

                            dt.Rows.Add(titleRow);
                            dt.Rows.Add(authorRow);
                            dt.Rows.Add(ccliRow);
                            dt.Rows.Add(copyrightRow);
                            dt.Rows.Add(adminRow);
                            dt.Rows.Add(spaceRow);


                        }
                        //Add data to appropriate columns/rows
                        dt.Rows[titleRowID][colName] = item.SongTitle;
                        dt.Rows[authorRowID][colName] = item.SongAuthor;
                        dt.Rows[ccliRowID][colName] = item.CCLINumber;
                        dt.Rows[copyrightRowID][colName] = item.Copyright;
                        dt.Rows[adminRowID][colName] = item.Administrator;
                    }
                }
            }

            string rowsText = String.Empty;
            foreach (DataRow row in dt.Rows) {
                Hashtable rowHash = new Hashtable();
                string rowData = String.Empty;
                Boolean isTitleRow = (row[0] == "Song");
                for (int i = 0; i < dt.Columns.Count; i++) {
                    Hashtable cellHash = new Hashtable();

                    if (i == 0) {
                        cellHash.Add("cellStyle", "ssHeavyRight");
                    } else {
                        if (row[i].ToString() == String.Empty) {
                            cellHash.Add("cellStyle", "ssSimple");
                        } else {
                            if (isTitleRow) {
                                cellHash.Add("cellStyle", "ssPrimaryFieldBold");
                            } else {
                                cellHash.Add("cellStyle", "ssPrimaryField");
                            }
                        }
                    }
                    cellHash.Add("dataType", "String");
                    cellHash.Add("dataValue", row[i].ToString());
                    rowData += Stepframe.MoustacheTemplate.ApplyTemplate(context.Server.MapPath("/templates/excelCell.txt"), cellHash);
                }
                rowHash.Add("DataCells", rowData);
                rowsText += Stepframe.MoustacheTemplate.ApplyTemplate(context.Server.MapPath("/templates/excelRow.txt"), rowHash);
            }

            Hashtable sheetHash = new Hashtable();
            sheetHash.Add("locationName", loc.Value);
            sheetHash.Add("HeaderRow", Stepframe.MoustacheTemplate.ApplyTemplate(context.Server.MapPath("/templates/excelHeaderRow.txt"), sheetHash));
            sheetHash.Add("TitleRow", Stepframe.MoustacheTemplate.ApplyTemplate(context.Server.MapPath("/templates/excelRow.txt"), titleRowHash));


            sheetHash.Add("Rows", rowsText);
            sheetsText += Stepframe.MoustacheTemplate.ApplyTemplate(context.Server.MapPath("/templates/excelWorksheet.txt"), sheetHash);
        }
        Hashtable bookHash = new Hashtable();
        bookHash.Add("WorkSheets", sheetsText);
        string bookText = Stepframe.MoustacheTemplate.ApplyTemplate(context.Server.MapPath("/templates/excelWorkbook.txt"), bookHash);

        //System.IO.File.WriteAllText(context.Server.MapPath("/export/ccliInfo.xml"), bookText);

        context.Response.Write(bookText);
        context.Response.End();


    }

    public bool IsReusable {
        get {
            return false;
        }
    }

}