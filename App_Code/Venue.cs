using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Stepframe;
using System.Net;
using System.Text;
using System.Configuration;
using Newtonsoft.Json;
using System.IO;


/// <summary>
/// Summary description for Venue
/// </summary>
/// 
namespace PlanningCenter {
    public class Folder {
        private static List<Folder> _folders;

        private static string FoldersURL = "https://api.planningcenteronline.com/services/v2/folders";

        public List<Folder> ChildFolders;
        public int ID;
        public int ParentID;
        public string Name;
        public string URL;
        [JsonIgnore]
        public Folder Parent;
        public List<Venue> Venues;

        public Folder() {
            this.ChildFolders = new List<Folder>();
            this.Venues = new List<Venue>();
        }


        public Folder(dynamic DataJSON) {
            this.ChildFolders = new List<Folder>();
            this.Venues = new List<Venue>();
            Folder._folders.Add(this);
            this.Update(DataJSON);
        }


        public void Update(dynamic DataJSON) {
            this.ID = (DataJSON.id != null) ? Convert.ToInt32(DataJSON.id.ToString()) : 0;
            this.Name = (DataJSON.attributes != null && DataJSON.attributes.name != null) ? DataJSON.attributes.name.ToString() : "";
            this.URL = (DataJSON.links != null && DataJSON.links.self != null) ? DataJSON.links.self.ToString() : "";
            this.ParentID = (DataJSON.attributes != null && DataJSON.attributes.parent_id != null) ? Convert.ToInt32(DataJSON.attributes.parent_id.ToString()) : 0;
            this.AddToParent();
        }

        public void AddToParent() {
            if (this.Parent == null && this.ParentID != 0) {
                if (Folder._folders == null) { Folder._folders = new List<Folder>(); }

                foreach (Folder fol in Folder._folders) {
                    if (fol.ID == this.ParentID) {
                        fol.AddChild(this);
                        this.Parent = this;
                        _folders.Remove(this);
                        break;
                    }
                }
            }
        }

        public void AddVenue(Venue venue) {
            if (this.Venues == null) {this.Venues = new List<Venue>();}
            this.Venues.Add(venue);
        }

        public Folder FindFolder(int id) {
            if (this.ID == id) { return this; }

            foreach (Folder fol in this.ChildFolders) {
                Folder newFolder = fol.FindFolder(id);
                if (newFolder != null) { return newFolder; }
            }

            return null;
        }

        public void AddChild(Folder childFolder) {
            this.ChildFolders.Add(childFolder);
            if (!this.ChildFolders.Contains(childFolder)) {
                this.ChildFolders.Add(childFolder);
            }
        }

        public static void AddNewFolder(dynamic DataJSON) {
            Folder newFolder = new Folder(DataJSON);
        }

        public static void AddVenueToFolder(Venue venue) {
            Folder venueFolder = Folder.GetFolder(venue.ParentID);
            if (venueFolder != null) {
                venueFolder.AddVenue(venue);
            }
        }

        public static Folder GetFolder(int id) {
            if (Folder._folders == null) { return null; }

            foreach (Folder fol in Folder._folders) {
                Folder newFolder = fol.FindFolder(id);
                if (newFolder != null) { return newFolder; }
            }

            return null;
        }

        public static Folder AddToFolder(Folder childFolder) {
            Folder parent = null;
            if (Folder._folders == null) { Folder._folders = new List<Folder>(); }

            foreach (Folder fol in Folder._folders) {
                if (fol.ID == childFolder.ParentID) {
                    parent = fol;
                    fol.AddChild(childFolder);
                }
            }
            return parent;
        }

        public static List<Folder> GetFolders() {

            Folder._folders = new List<Folder>();
            dynamic venuesData = Utility.GetRequestObject(FoldersURL);

            if (venuesData.data != null) {
                foreach (dynamic itemDynamic in venuesData.data) {
                    Folder.AddNewFolder(itemDynamic);
                }
            }

            //Once the folders have been added, loop through again to check for parents.
            foreach (Folder fol in _folders) {
                fol.AddToParent();
            }

            Venue.GetVenues();

            return Folder._folders;
        }
    }

    public class Venue {
        private static List<Folder> _folders;
        private static string VenuesURL = "https://api.planningcenteronline.com/services/v2/service_types/";

        public static List<Folder> Folders {
            get {
                if (_folders == null) {


                }
                return _folders;
            }

        }

        public Venue(int id) {
            //
            // TODO: Add constructor logic here
            //
        }

        public Venue(dynamic DataJSON) {
            this.Update(DataJSON);
        }

        public int ID;
        public string Name;
        public string URL;
        public string Permissions;
        public int ParentID;

        public void Update(dynamic DataJSON) {
            this.ID = (DataJSON.id != null) ? Convert.ToInt32(DataJSON.id.ToString()) : 0;
            this.Name = (DataJSON.attributes != null && DataJSON.attributes.name != null) ? DataJSON.attributes.name.ToString() : "";
            this.Permissions = (DataJSON.attributes != null && DataJSON.attributes.permissions != null) ? DataJSON.attributes.permissions.ToString() : "";
            this.ParentID = (DataJSON.attributes != null && DataJSON.attributes.parent_id != null) ? Convert.ToInt32(DataJSON.attributes.parent_id.ToString()) : 0;
            this.URL = (DataJSON.links != null && DataJSON.links.self != null) ? DataJSON.links.self.ToString() : "";
            Folder.AddVenueToFolder(this);
        }

        public static List<Venue> GetVenues() {
            List<Venue> venues = new List<Venue>();
            dynamic venuesData = Utility.GetRequestObject(VenuesURL);

            if (venuesData.data != null) {
                foreach (dynamic itemDynamic in venuesData.data) {
                    venues.Add(new Venue(itemDynamic));
                }
            }
            return venues;
        }


    }
}