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

using Aurora.Framework;
using Aurora.Framework.DatabaseInterfaces;
using Aurora.Framework.Servers.HttpServer;
using Aurora.Framework.Servers.HttpServer.Implementation;
using OpenMetaverse;
using System.Collections.Generic;

namespace Aurora.Modules.Web
{
    public class EditNewPage : IWebInterfacePage
    {
        public string[] FilePath
        {
            get
            {
                return new[]
                           {
                               "www/Central46/edit_news.html"
                           };
            }
        }

        public bool RequiresAuthentication
        {
            get { return true; }
        }

        public bool RequiresAdminAuthentication
        {
            get { return true; }
        }

        public Dictionary<string, object> Fill(WebInterface webInterface, string filename, OSHttpRequest httpRequest,
                                               OSHttpResponse httpResponse, Dictionary<string, object> requestParameters,
                                               ITranslator translator, out string response)
        {
            response = null;
            var vars = new Dictionary<string, object>();
            IGenericsConnector connector = Framework.Utilities.DataManager.RequestPlugin<IGenericsConnector>();
            GridNewsItem news;
            if (requestParameters.ContainsKey("Submit"))
            {
                string title = requestParameters["NewsTitle"].ToString();
                string text = requestParameters["NewsText"].ToString();
                string id = requestParameters["NewsID"].ToString();
                news = connector.GetGeneric<GridNewsItem>(UUID.Zero, "WebGridNews", id);
                connector.RemoveGeneric(UUID.Zero, "WebGridNews", id);
                GridNewsItem item = new GridNewsItem {Text = text, Time = news.Time, Title = title, ID = int.Parse(id)};
                connector.AddGeneric(UUID.Zero, "WebGridNews", id, item.ToOSD());
                response = "<h3>News item editted successfully, redirecting to main page</h3>" +
                           "<script language=\"javascript\">" +
                           "setTimeout(function() {window.location.href = \"index.html?page=news_manager\";}, 0);" +
                           "</script>";
                return null;
            }


            news = connector.GetGeneric<GridNewsItem>(UUID.Zero, "WebGridNews", httpRequest.Query["newsid"].ToString());
            vars.Add("NewsTitle", news.Title);
            vars.Add("NewsText", news.Text);
            vars.Add("NewsID", news.ID.ToString());

            vars.Add("NewsItemTitle", translator.GetTranslatedString("NewsItemTitle"));
            vars.Add("NewsItemText", translator.GetTranslatedString("NewsItemText"));
            vars.Add("EditNewsText", translator.GetTranslatedString("EditNewsText"));
            vars.Add("Submit", translator.GetTranslatedString("Submit"));
            return vars;
        }

        public bool AttemptFindPage(string filename, ref OSHttpResponse httpResponse, out string text)
        {
            text = "";
            return false;
        }
    }
}