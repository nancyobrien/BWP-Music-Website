using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

/// <summary>
/// Summary description for TeamMember
/// </summary>
public class TeamMember {



    public static string[] ValidPositions = { "Worship Leader", "Female bVox" };
    //  public static string[] ValidPositions = { "Worship Leader" };

    public static string TeamMemberURL = "https://api.planningcenteronline.com/services/v2/people/{teamMemberID}";

    public int ID;
    public string Name;
    public string URL;

    public static string PCOUrl(int id) {
        return TeamMemberURL.Replace("{teamMemberID}", id.ToString());
    }


    public TeamMember() {
        //
        // TODO: Add constructor logic here
        //
    }

    public TeamMember(int id) {
        this.ID = id;
    }

    public TeamMember(dynamic DataJSON) {
        this.Update(DataJSON);
    }


    public TeamMember(dynamic DataJSON, Boolean loadFromFile) {
        if (loadFromFile) {
            this.UpdateFromFile(DataJSON);
        } else {
            this.Update(DataJSON);
        }
    }
    public void UpdateFromFile(dynamic DataJSON) {
        this.URL = (DataJSON.URL != null) ? DataJSON.URL.ToString() : "";
        this.Name = (DataJSON.Name != null) ? DataJSON.Name.ToString() : "";
        this.ID = (DataJSON.ID != null) ? Convert.ToInt32(DataJSON.ID.ToString()) : 0;
    }

    public void Update(dynamic DataJSON) {
        dynamic data = (DataJSON.data != null) ? DataJSON.data : DataJSON;

        if (data != null && data.type != null && (data.type == "Person")) {
            dynamic attributes = (data.attributes != null) ? data.attributes : data;
            this.ID = (data.id != null) ? Convert.ToInt32(data.id) : 0;
            this.URL = (data.links != null && data.links.self != null) ? data.links.self.ToString() : PCOUrl(this.ID);
            this.Name = (attributes.first_name != null && attributes.last_name != null) ? attributes.first_name.ToString() + " " + attributes.last_name.ToString() : String.Empty;

        } else if (data != null && data.type != null && (data.type == "PlanPerson")) {
            dynamic attributes = (data.attributes != null) ? data.attributes : data;
            this.ID = (data.relationships != null && data.relationships.person != null && data.relationships.person.data != null && data.relationships.person.data.id != null) ? Convert.ToInt32(data.relationships.person.data.id) : 0;
            this.URL = (data.links != null && data.links.person != null) ? data.links.person.ToString() : PCOUrl(this.ID);
            this.Name = (attributes.name != null) ? attributes.name.ToString() : String.Empty;

        } else {
            dynamic attributes = (data != null && data.attributes != null) ? data.attributes : ((data.person_id != null) ? data : null);
            this.ID = (attributes != null && attributes.person_id != null) ? Convert.ToInt32(attributes.person_id.ToString()) : 0;
            this.URL = PCOUrl(this.ID);
            this.Name = (attributes.name != null) ? attributes.name.ToString() : String.Empty;

        }
    }

    public static List<TeamMember> GetMembers(string url) {
        List<TeamMember> members = new List<TeamMember>();

        dynamic teamMemberData = Utility.GetRequestObject(url);
        if (teamMemberData.data != null) {
            foreach (dynamic itemDynamic in teamMemberData.data) {
                if (Array.IndexOf(TeamMember.ValidPositions, itemDynamic.attributes.team_position_name) > -1) {
                    TeamMember mem = new TeamMember(itemDynamic);
                    if (!members.Exists(myObject => myObject.ID == mem.ID)) {
                        members.Add(mem);
                    }

                }
            }
        }

        if (teamMemberData.links != null && teamMemberData.links.next != null) {
            members.AddRange(GetMembers(teamMemberData.links.next));
        }

        return members;
    }

    public static TeamMember GetMember(int id) {
        TeamMember member = GetMember(TeamMember.PCOUrl(id));
        return member;
    }


    public static TeamMember GetMember(string url) {
        TeamMember member = null;
        if (url != String.Empty) {
            member = new TeamMember(Utility.GetRequestObject(url));
        }
        return member;
    }

}

