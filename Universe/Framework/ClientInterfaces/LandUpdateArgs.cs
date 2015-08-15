/*
 * Copyright (c) Contributors, http://virtual-planets.org/, http://Universe-sim.org/, http://aurora-sim.org, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the Universe-Sim Project nor the
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

using System;
using OpenMetaverse;

namespace Universe.Framework.ClientInterfaces
{
    public class LandUpdateArgs : EventArgs
    {
        public UUID AuthBuyerID;
        public ParcelCategory Category;
        public string Desc;
        public UUID GroupID;
        public byte LandingType;
        public byte MediaAutoScale;
        public string MediaDescription;
        public int MediaHeight;
        public UUID MediaID;
        public bool MediaLoop;
        public string MediaType;
        public string MediaURL;
        public int MediaWidth;
        public string MusicURL;
        public string Name;
        public bool ObscureMedia;
        public bool ObscureMusic;
        public uint ParcelFlags;
        public float PassHours;
        public int PassPrice;
        public bool Privacy;
        public int SalePrice;
        public UUID SnapshotID;
        public Vector3 UserLocation;
        public Vector3 UserLookAt;
    }
}