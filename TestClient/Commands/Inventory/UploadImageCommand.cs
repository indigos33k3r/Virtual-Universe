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

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Drawing;
using OpenMetaverse;
using OpenMetaverse.Http;
using OpenMetaverse.Imaging;

namespace OpenMetaverse.TestClient
{
    public class UploadImageCommand : Command
    {
        AutoResetEvent UploadCompleteEvent = new AutoResetEvent(false);
        UUID TextureID = UUID.Zero;
        DateTime start;

        public UploadImageCommand(TestClient testClient)
        {
            Name = "uploadimage";
            Description = "Upload an image to your inventory. Usage: uploadimage [inventoryname] [timeout] [filename]";
            Category = CommandCategory.Inventory;
        }

        public override string Execute(string[] args, UUID fromAgentID)
        {
            string inventoryName;
            uint timeout;
            string fileName;

            if (args.Length != 3)
                return "Usage: uploadimage [inventoryname] [timeout] [filename]";

            TextureID = UUID.Zero;
            inventoryName = args[0];
            fileName = args[2];
            if (!UInt32.TryParse(args[1], out timeout))
                return "Usage: uploadimage [inventoryname] [timeout] [filename]";

            Console.WriteLine("Loading image " + fileName);
            byte[] jpeg2k = LoadImage(fileName);
            if (jpeg2k == null)
                return "Failed to compress image to JPEG2000";
            Console.WriteLine("Finished compressing image to JPEG2000, uploading...");
            start = DateTime.Now;
            DoUpload(jpeg2k, inventoryName);

            if (UploadCompleteEvent.WaitOne((int)timeout, false))
            {
                return String.Format("Texture upload {0}: {1}", (TextureID != UUID.Zero) ? "succeeded" : "failed",
                    TextureID);
            }
            else
            {
                return "Texture upload timed out";
            }
        }

        private void DoUpload(byte[] UploadData, string FileName)
        {
            if (UploadData != null)
            {
                string name = System.IO.Path.GetFileNameWithoutExtension(FileName);

                Client.Inventory.RequestCreateItemFromAsset(UploadData, name, "Uploaded with TestClient",
                    AssetType.Texture, InventoryType.Texture, Client.Inventory.FindFolderForType(AssetType.Texture),
                    delegate(bool success, string status, UUID itemID, UUID assetID)
                    {
                        Console.WriteLine(String.Format(
                            "RequestCreateItemFromAsset() returned: Success={0}, Status={1}, ItemID={2}, AssetID={3}",
                            success, status, itemID, assetID));

                        TextureID = assetID;
                        Console.WriteLine(String.Format("Upload took {0}", DateTime.Now.Subtract(start)));
                        UploadCompleteEvent.Set();
                    }
                );
            }
        }

        private byte[] LoadImage(string fileName)
        {
            byte[] UploadData;
            string lowfilename = fileName.ToLower();
            Bitmap bitmap = null;

            try
            {
                if (lowfilename.EndsWith(".jp2") || lowfilename.EndsWith(".j2c"))
                {
                    Image image;
                    ManagedImage managedImage;

                    // Upload JPEG2000 images untouched
                    UploadData = System.IO.File.ReadAllBytes(fileName);
                    
                    OpenJPEG.DecodeToImage(UploadData, out managedImage, out image);
                    bitmap = (Bitmap)image;
                }
                else
                {
                    if (lowfilename.EndsWith(".tga"))
                        bitmap = LoadTGAClass.LoadTGA(fileName);
                    else
                        bitmap = (Bitmap)System.Drawing.Image.FromFile(fileName);

                    int oldwidth = bitmap.Width;
                    int oldheight = bitmap.Height;

                    if (!IsPowerOfTwo((uint)oldwidth) || !IsPowerOfTwo((uint)oldheight))
                    {
                        Bitmap resized = new Bitmap(256, 256, bitmap.PixelFormat);
                        Graphics graphics = Graphics.FromImage(resized);

                        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                        graphics.InterpolationMode =
                           System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        graphics.DrawImage(bitmap, 0, 0, 256, 256);

                        bitmap.Dispose();
                        bitmap = resized;

                        oldwidth = 256;
                        oldheight = 256;
                    }

                    // Handle resizing to prevent excessively large images
                    if (oldwidth > 1024 || oldheight > 1024)
                    {
                        int newwidth = (oldwidth > 1024) ? 1024 : oldwidth;
                        int newheight = (oldheight > 1024) ? 1024 : oldheight;

                        Bitmap resized = new Bitmap(newwidth, newheight, bitmap.PixelFormat);
                        Graphics graphics = Graphics.FromImage(resized);

                        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                        graphics.InterpolationMode =
                           System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        graphics.DrawImage(bitmap, 0, 0, newwidth, newheight);

                        bitmap.Dispose();
                        bitmap = resized;
                    }

                    UploadData = OpenJPEG.EncodeFromImage(bitmap, false);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString() + " SL Image Upload ");
                return null;
            }
            return UploadData;
        }

        private static bool IsPowerOfTwo(uint n)
        {
            return (n & (n - 1)) == 0 && n != 0;
        }
    }
}