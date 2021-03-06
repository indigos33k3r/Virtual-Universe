﻿/*
 * Copyright (c) Contributors, http://virtual-planets.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 * For an explanation of the license of each contributor and the content it 
 * covers please see the Licenses directory.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the Virtual Universe Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System.Collections.Generic;
using System.Linq;
using OpenMetaverse;
using Universe.Framework.DatabaseInterfaces;
using Universe.Framework.Modules;
using Universe.Framework.Servers.HttpServer.Implementation;
using Universe.Framework.Services;
using Universe.Framework.Services.ClassHelpers.Profile;
using Universe.Framework.Utilities;

namespace Universe.Modules.Web
{
    public class UserProfilePage : IWebInterfacePage
    {
        public string[] FilePath
        {
            get
            {
                return new[]
                           {
                               "html/user_profile.html"
                           };
            }
        }

        public bool RequiresAuthentication
        {
            get { return true; }
        }

        public bool RequiresAdminAuthentication
        {
            get { return false; }
        }

        public Dictionary<string, object> Fill(WebInterface webInterface, string filename, OSHttpRequest httpRequest,
            OSHttpResponse httpResponse, Dictionary<string, object> requestParameters,
            ITranslator translator, out string response)
        {
            response = null;
            var vars = new Dictionary<string, object>();

            IWebHttpTextureService webhttpService = webInterface.Registry.RequestModuleInterface<IWebHttpTextureService>();

            UserAccount account = Authenticator.GetAuthentication(httpRequest);

            if (account == null)
                return vars;

            vars.Add("UserName", account.Name);
            //  TODO: User Profile inworld shows this as the standard mm/dd/yyyy
            //  Do we want this to be localised into the users Localisation or keep it as standard 
            vars.Add("UserBorn", Util.ToDateTime(account.Created).ToShortDateString());

            IUserProfileInfo profile = Framework.Utilities.DataManager.RequestPlugin<IProfileConnector>().GetUserProfile(account.PrincipalID);
            string picUrl = "../images/icons/no_avatar.jpg";

            if (profile != null)
            {
                vars.Add("UserType", profile.MembershipGroup == "" ? "Citizen" : profile.MembershipGroup);

                if (profile.Partner != UUID.Zero)
                {
                    account = webInterface.Registry.RequestModuleInterface<IUserAccountService>().GetUserAccount(null, profile.Partner);
                    vars.Add("UserPartner", account.Name);
                }
                else
                    vars.Add("UserPartner", "No partner");

                vars.Add("UserAboutMe", profile.AboutText == "" ? "Nothing here" : profile.AboutText);

                if (webhttpService != null && profile.Image != UUID.Zero)
                    picUrl = webhttpService.GetTextureURL(profile.Image);
            }
            else
            {
                // no profile yet
                vars.Add("UserType", "Citizen");
                vars.Add("UserPartner", "Not specified yet");
                vars.Add("UserAboutMe", "Nothing here yet");
            }

            vars.Add("UserPictureURL", picUrl);

            // TODO:  This is only showing online status if you are logged in ??
            UserAccount ourAccount = Authenticator.GetAuthentication(httpRequest);
            if (ourAccount != null)
            {
                IFriendsService friendsService = webInterface.Registry.RequestModuleInterface<IFriendsService>();
                var friends = friendsService.GetFriends(account.PrincipalID);
                UUID friendID = UUID.Zero;

                if (friends.Any(f => UUID.TryParse(f.Friend, out friendID) && friendID == ourAccount.PrincipalID))
                {
                    IAgentInfoService agentInfoService = webInterface.Registry.RequestModuleInterface<IAgentInfoService>();
                    IGridService gridService = webInterface.Registry.RequestModuleInterface<IGridService>();
                    UserInfo ourInfo = agentInfoService.GetUserInfo(account.PrincipalID.ToString());

                    if (ourInfo != null && ourInfo.IsOnline)
                        vars.Add("OnlineLocation", gridService.GetRegionByUUID(null, ourInfo.CurrentRegionID).RegionName);

                    vars.Add("UserIsOnline", ourInfo != null && ourInfo.IsOnline);
                    vars.Add("IsOnline",
                        ourInfo != null && ourInfo.IsOnline
                        ? translator.GetTranslatedString("Online")
                        : translator.GetTranslatedString("Offline"));
                }
                else
                {
                    vars.Add("OnlineLocation", "");
                    vars.Add("UserIsOnline", false);
                    vars.Add("IsOnline", translator.GetTranslatedString("Offline"));
                }
            }
            else
            {
                vars.Add("OnlineLocation", "");
                vars.Add("UserIsOnline", false);
                vars.Add("IsOnline", translator.GetTranslatedString("Offline"));
            }

            vars.Add("UsersGroupsText", translator.GetTranslatedString("UsersGroupsText"));

            IGroupsServiceConnector groupsConnector = Framework.Utilities.DataManager.RequestPlugin<IGroupsServiceConnector>();
            List<Dictionary<string, object>> groups = new List<Dictionary<string, object>>();

            if (groupsConnector != null)
            {
                var groupsIn = groupsConnector.GetAgentGroupMemberships(account.PrincipalID, account.PrincipalID);

                if (groupsIn != null)
                {
                    foreach (var grp in groupsIn)
                    {
                        var grpData = groupsConnector.GetGroupProfile(account.PrincipalID, grp.GroupID);
                        string url = "../images/icons/no_groups.jpg";

                        if (grpData != null)
                        {
                            if (webhttpService != null && grpData.InsigniaID != UUID.Zero)
                                url = webhttpService.GetTextureURL(grpData.InsigniaID);

                            groups.Add(new Dictionary<string, object> {
                            { "GroupPictureURL", url },
                            { "GroupName", grp.GroupName }
                        });
                        }
                    }
                }

                if (groups.Count == 0)
                {
                    groups.Add(new Dictionary<string, object> {
                        { "GroupPictureURL", "../images/icons/no_groups.jpg" },
                        { "GroupName", "None yet" }
                    });
                }
            }

            vars.Add("GroupNameText", translator.GetTranslatedString("GroupNameText"));
            vars.Add("Groups", groups);
            vars.Add("GroupsJoined", groups.Count);

            // Menus
            vars.Add("MenuProfileTitle", translator.GetTranslatedString("MenuProfileTitle"));
            vars.Add("TooltipsMenuProfile", translator.GetTranslatedString("TooltipsMenuProfile"));
            vars.Add("MenuGroupTitle", translator.GetTranslatedString("MenuGroupTitle"));
            vars.Add("TooltipsMenuGroups", translator.GetTranslatedString("TooltipsMenuGroups"));
            vars.Add("MenuPicksTitle", translator.GetTranslatedString("MenuPicksTitle"));
            vars.Add("TooltipsMenuPicks", translator.GetTranslatedString("TooltipsMenuPicks"));
            vars.Add("MenuRegionsTitle", translator.GetTranslatedString("MenuRegionsTitle"));
            vars.Add("TooltipsMenuRegions", translator.GetTranslatedString("TooltipsMenuRegions"));

            // User data
            vars.Add("UserProfileFor", translator.GetTranslatedString("UserProfileFor"));
            vars.Add("ResidentSince", translator.GetTranslatedString("ResidentSince"));
            vars.Add("AccountType", translator.GetTranslatedString("AccountType"));
            vars.Add("PartnersName", translator.GetTranslatedString("PartnersName"));
            vars.Add("AboutMe", translator.GetTranslatedString("AboutMe"));
            vars.Add("IsOnlineText", translator.GetTranslatedString("IsOnlineText"));
            vars.Add("OnlineLocationText", translator.GetTranslatedString("OnlineLocationText"));

            // Style Switcher
            vars.Add("styles1", translator.GetTranslatedString("styles1"));
            vars.Add("styles2", translator.GetTranslatedString("styles2"));
            vars.Add("styles3", translator.GetTranslatedString("styles3"));
            vars.Add("styles4", translator.GetTranslatedString("styles4"));
            vars.Add("styles5", translator.GetTranslatedString("styles5"));

            vars.Add("StyleSwitcherStylesText", translator.GetTranslatedString("StyleSwitcherStylesText"));
            vars.Add("StyleSwitcherLanguagesText", translator.GetTranslatedString("StyleSwitcherLanguagesText"));
            vars.Add("StyleSwitcherChoiceText", translator.GetTranslatedString("StyleSwitcherChoiceText"));

            // Language Switcher
            vars.Add("en", translator.GetTranslatedString("en"));
            vars.Add("fr", translator.GetTranslatedString("fr"));
            vars.Add("de", translator.GetTranslatedString("de"));
            vars.Add("ga", translator.GetTranslatedString("ga"));
            vars.Add("it", translator.GetTranslatedString("it"));
            vars.Add("es", translator.GetTranslatedString("es"));
            vars.Add("nl", translator.GetTranslatedString("nl"));
            vars.Add("ru", translator.GetTranslatedString("ru"));
            vars.Add("zh_CN", translator.GetTranslatedString("zh_CN"));

            var settings = webInterface.GetWebUISettings();
            vars.Add("ShowLanguageTranslatorBar", !settings.HideLanguageTranslatorBar);
            vars.Add("ShowStyleBar", !settings.HideStyleBar);

            return vars;
        }

        public bool AttemptFindPage(string filename, ref OSHttpResponse httpResponse, out string text)
        {
            text = "";
            return false;
        }
    }
}