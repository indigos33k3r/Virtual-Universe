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

using Nini.Config;
using Nini.Ini;
using System;
using System.IO;
using System.Reflection;

namespace Aurora.Simulator.Base
{
    public class BinMigratorService
    {
        const int CurrentBinVersion = 10;

        public void MigrateBin()
        {
            int currentVersion = GetBinVersion();
			if (currentVersion != CurrentBinVersion)
            {
                UpgradeToTarget(currentVersion);
				SetBinVersion(CurrentBinVersion);
            }
        }

        public static int GetBinVersion ()
		{
			if (!File.Exists ("version"))
				return 0;
			string file = File.ReadAllText ("version");
			return int.Parse (file);
		}

        public static void SetBinVersion (int version)
		{
			File.WriteAllText ("version", version.ToString ());
		}

        public bool UpgradeToTarget(int currentVersion)
        {
            try
            {
				while (currentVersion != CurrentBinVersion)
                {
                    MethodInfo info = GetType().GetMethod("RunMigration" + ++currentVersion);
                    if (info != null)
                        info.Invoke(this, null);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error running bin migration " + currentVersion + ", " + ex);
                return false;
            }
            return true;
        }

        //Next: 9

        public static void RunMigration9 ()
		{
			if (File.Exists ("UserServer.exe"))
				File.Delete ("UserServer.exe");
		}

        public static void RunMigration10 ()
		{
			foreach (string dir in Directory.GetDirectories ("Storage/Scripts/")) {
				Directory.Delete (dir, true);
			}
		}
    }

    public enum MigratorAction
    {
        Add,
        Remove
    }

    public class IniMigrator
    {
        public static void UpdateIniFile(string fileName, string handler, string[] names, string[] values,
                                         MigratorAction[] actions)
        {
            if (File.Exists(fileName + ".example")) //Update the .example files too if people haven't
                UpdateIniFile(fileName + ".example", handler, names, values, actions);
            if (File.Exists(fileName))
            {
                IniConfigSource doc = new IniConfigSource(fileName, IniFileType.AuroraStyle);
                IConfig section = doc.Configs[handler];
                for (int i = 0; i < names.Length; i++)
                {
                    string name = names[i];
                    string value = values[i];
                    MigratorAction action = actions[i];
                    if (action == MigratorAction.Add)
                        section.Set(name, value);
                    else
                        section.Remove(name);
                }
                doc.Save();
            }
        }
    }
}