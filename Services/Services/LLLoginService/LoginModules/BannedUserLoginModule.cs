﻿/***************************************************************************
 *	                VIRTUAL REALITY PUBLIC SOURCE LICENSE
 * 
 * Date				: Sun January 1, 2006
 * Copyright		: (c) 2006-2014 by Virtual Reality Development Team. 
 *                    All Rights Reserved.
 * Website			: http://www.syndarveruleiki.is
 *
 * Product Name		: Virtual Reality
 * License Text     : packages/docs/VRLICENSE.txt
 * 
 * Planetary Info   : Information about the Planetary code
 * 
 * Copyright        : (c) 2014-2024 by Second Galaxy Development Team
 *                    All Rights Reserved.
 * 
 * Website          : http://www.secondgalaxy.com
 * 
 * Product Name     : Virtual Reality
 * License Text     : packages/docs/SGLICENSE.txt
 * 
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the WhiteCore-Sim Project nor the
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
***************************************************************************/

using Aurora.Framework.ConsoleFramework;
using Aurora.Framework.DatabaseInterfaces;
using Aurora.Framework.Modules;
using Aurora.Framework.Services;
using Aurora.Framework.Services.ClassHelpers.Profile;
using Nini.Config;
using System;
using System.Collections;
using System.IO;

namespace Aurora.Services
{
    public class BannedUserLoginModule : ILoginModule
    {
        protected IAuthenticationService m_AuthenticationService;
        protected ILoginService m_LoginService;
        protected bool m_UseTOS = true;
        protected string m_TOSLocation = "Settings/Laws/";

        public string Name
        {
            get { return GetType().Name; }
        }

        public void Initialize(ILoginService service, IConfigSource config, IRegistryCore registry)
        {
            IConfig loginServerConfig = config.Configs["LoginService"];
            if (loginServerConfig != null)
            {
                m_UseTOS = loginServerConfig.GetBoolean("UseTermsOfServiceOnFirstLogin", false);
                m_TOSLocation = loginServerConfig.GetString("FileNameOfTOS", "*.txt");
            }
            m_AuthenticationService = registry.RequestModuleInterface<IAuthenticationService>();
            m_LoginService = service;
        }

        public LoginResponse Login(Hashtable request, UserAccount account, IAgentInfo agentInfo, string authType,
                                   string password, out object data)
        {
            IAgentConnector agentData = Framework.Utilities.DataManager.RequestPlugin<IAgentConnector>();
            data = null;

            if (request == null)
                return null;
                    //If its null, its just a verification request, allow them to see things even if they are banned

            bool tosExists = false;
            string tosAccepted = "";
            if (request.ContainsKey("agree_to_tos"))
            {
                tosExists = true;
                tosAccepted = request["agree_to_tos"].ToString();
            }

            //MAC BANNING START
            string mac = (string) request["mac"];
            if (mac == "")
            {
                data = "Bad Viewer Connection";
                return new LLFailedLoginResponse(LoginResponseEnum.Indeterminant, data.ToString(), false);
            }

            // TODO: Some TPV's now send their version in the Channel
			string channel;
            if (request.Contains("channel") && request["channel"] != null)
                channel = request["channel"].ToString();

            bool AcceptedNewTOS = false;
            //This gets if the viewer has accepted the new TOS
            if (!agentInfo.AcceptTOS && tosExists)
            {
                if (tosAccepted == "0")
                    AcceptedNewTOS = false;
                else if (tosAccepted == "1")
                    AcceptedNewTOS = true;
                else
                    AcceptedNewTOS = bool.Parse(tosAccepted);

                if (agentInfo.AcceptTOS != AcceptedNewTOS)
                {
                    agentInfo.AcceptTOS = AcceptedNewTOS;
                    agentData.UpdateAgent(agentInfo);
                }
            }
            if (!AcceptedNewTOS && !agentInfo.AcceptTOS && m_UseTOS)
            {
                data = "TOS not accepted";
                return new LLFailedLoginResponse(LoginResponseEnum.ToSNeedsSent, File.ReadAllText(Path.Combine(Environment.CurrentDirectory, m_TOSLocation)), false);
            }
            if ((agentInfo.Flags & IAgentFlags.PermBan) == IAgentFlags.PermBan)
            {
                MainConsole.Instance.InfoFormat(
                    "[LLOGIN SERVICE]: Login failed for user {0}, reason: user is permanently banned.", account.Name);
                data = "Permanently banned";
                return LLFailedLoginResponse.PermanentBannedProblem;
            }

            if ((agentInfo.Flags & IAgentFlags.TempBan) == IAgentFlags.TempBan)
            {
                bool IsBanned = true;
                string until = "";

                if (agentInfo.OtherAgentInformation.ContainsKey("TemperaryBanInfo"))
                {
                    DateTime bannedTime = agentInfo.OtherAgentInformation["TemperaryBanInfo"].AsDate();
                    until = string.Format(" until {0}" + " {1}", bannedTime.ToLocalTime().ToShortDateString(),
                                          bannedTime.ToLocalTime().ToLongTimeString());

                    //Check to make sure the time hasn't expired
                    if (bannedTime.Ticks < DateTime.Now.ToUniversalTime().Ticks)
                    {
                        //The banned time is less than now, let the user in.
                        IsBanned = false;
                    }
                }

                if (IsBanned)
                {
                    MainConsole.Instance.InfoFormat(
                        "[LLOGIN SERVICE]: Login failed for user {0}, reason: user is temporarily banned {1}.",
                        account.Name, until);
                    data =  string.Format("You are blocked from connecting to this service{" + "0}.", until);
                    return new LLFailedLoginResponse(LoginResponseEnum.Indeterminant,
                                                    data.ToString(), false);
                }
            }
            return null;
        }
    }
}