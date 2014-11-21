/***************************************************************************
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

using System.Collections.Generic;
using Aurora.Framework.ClientInterfaces;
using Aurora.Framework.DatabaseInterfaces;
using Aurora.Framework.Modules;
using Aurora.Framework.Services;
using Aurora.Framework.Utilities;
using Nini.Config;
using OpenMetaverse;

namespace Aurora.Services.DataService
{
    public class LocalOfflineMessagesConnector : ConnectorBase, IOfflineMessagesConnector, 
	#region IOfflineMessagesConnector Members
	#endregion
	System.IDisposable
	{
		IGenericData GD;
		int m_maxOfflineMessages = 20;
		public void Initialize (IGenericData GenericData, IConfigSource source, IRegistryCore simBase, string defaultConnectionString)
		{
			GD = GenericData;
			if (source.Configs [Name] != null)
				defaultConnectionString = source.Configs [Name].GetString ("ConnectionString", defaultConnectionString);
			if (GD != null)
				GD.ConnectToDatabase (defaultConnectionString, "Generics", source.Configs ["Connectors"].GetBoolean ("ValidateTables", true));
			Framework.Utilities.DataManager.RegisterPlugin (Name + "Local", this);
			m_maxOfflineMessages = source.Configs ["Connectors"].GetInt ("MaxOfflineMessages", m_maxOfflineMessages);
			if (source.Configs ["Connectors"].GetString ("OfflineMessagesConnector", "LocalConnector") == "LocalConnector") {
				Framework.Utilities.DataManager.RegisterPlugin (this);
			}
			Init (simBase, Name);
		}
		public string Name {
			get {
				return "IOfflineMessagesConnector";
			}
		}
		/// <summary>
		///     Gets all offline messages for the user in GridInstantMessage format.
		/// </summary>
		/// <param name="agentID"></param>
		/// <returns></returns>
		[CanBeReflected (ThreatLevel = ThreatLevel.Low)]
		public List<GridInstantMessage> GetOfflineMessages (UUID agentID)
		{
			object remoteValue = DoRemote (agentID);
			if (remoteValue != null || m_doRemoteOnly)
				return (List<GridInstantMessage>)remoteValue;
			//Get all the messages
			List<GridInstantMessage> Messages = GenericUtils.GetGenerics<GridInstantMessage> (agentID, "OfflineMessages", GD);
			Messages.AddRange (GenericUtils.GetGenerics<GridInstantMessage> (agentID, "GroupOfflineMessages", GD));
			//Clear them out now that we have them
			GenericUtils.RemoveGenericByType (agentID, "OfflineMessages", GD);
			GenericUtils.RemoveGenericByType (agentID, "GroupOfflineMessages", GD);
			return Messages;
		}
		/// <summary>
		///     Adds a new offline message for the user.
		/// </summary>
		/// <param name="message"></param>
		[CanBeReflected (ThreatLevel = ThreatLevel.Low)]
		public bool AddOfflineMessage (GridInstantMessage message)
		{
			object remoteValue = DoRemote (message);
			if (remoteValue != null || m_doRemoteOnly)
				return remoteValue == null ? false : (bool)remoteValue;
			if (m_maxOfflineMessages <= 0 || GenericUtils.GetGenericCount (message.ToAgentID, "OfflineMessages", GD) < m_maxOfflineMessages) {
				GenericUtils.AddGeneric (message.ToAgentID, "OfflineMessages", UUID.Random ().ToString (), message.ToOSD (), GD);
				return true;
			}
			return false;
		}
		public void Dispose ()
		{
		}
	}
}