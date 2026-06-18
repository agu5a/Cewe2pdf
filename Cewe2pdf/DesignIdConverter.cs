using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SkiaSharp;

namespace Cewe2pdf {
    class DesignIdConverter {

        private static Dictionary<string, SKBitmap> _imageCache = new Dictionary<string, SKBitmap>();

        private static string getPath(string pId) {
            string path;

            // from installation
            path = getIdPathFromInstallationDirectly(pId);
            if (!String.IsNullOrWhiteSpace(path)) {
                return path;
            }

            // in downloaded content
            path = getIdPathFromProgramData(pId);
            if (!String.IsNullOrWhiteSpace(path)) {
                return path;
            }

            Log.Error("DesignID '" + pId + "' not found.");
            return "";
        }

        private static string getIdPathFromDirectory(string pId, string pDirectory) {
            if (Directory.Exists(pDirectory)) {
                string[] filenames = Directory.GetFiles(pDirectory, "*", SearchOption.AllDirectories);
                Log.Info("Loading DesignIDs from '" + pDirectory + "'.");
                foreach (string addfile in filenames) {
                    if (addfile.EndsWith(".jpg") || addfile.EndsWith(".bmp") || addfile.EndsWith(".webp")) {
                        string id = Path.GetFileNameWithoutExtension(addfile);
                        id = id.Split("-").Last();
                        if (id == pId)
                            return addfile;
                    }
                }
            } else {
                Log.Warning("Directory at: '" + pDirectory + "' does not exist.");
            }
            return "";
        }

        private static string getIdPathFromProgramData(string pId) {
            if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
                return null;
            return getIdPathFromDirectory(pId, "C:\\ProgramData\\hps");
        }

        public static string getIdPathFromInstallationDirectly(string pId) {
            string path = Path.Combine(Config.ProgramPath, "Resources", "photofun", "backgrounds");
            return getIdPathFromDirectory(pId, path);
        }

        public static SKBitmap getImageFromID(string pId) {
            if (_imageCache.ContainsKey(pId)) {
                Log.Info("Using cached image for Design ID '" + pId + "'");
                return _imageCache[pId];
            }

            string path = getPath(pId);

            if (String.IsNullOrWhiteSpace(path)) {
                Log.Error("Design ID '" + pId + "' not found.");
                return null;
            }

            if (!File.Exists(path)) {
                Log.Error("DesignID file at: '" + path + "' does not exist.");
                return null;
            } else {
                Log.Info("Loading DesignID: " + Path.GetFileName(path));
            }

            try {
                SKBitmap bm = SKBitmap.Decode(path);
                if (bm == null) {
                    Log.Error("Loading '" + path + "' failed: SKBitmap.Decode returned null.");
                    return null;
                }
                addToImageCache(pId, bm);
                return bm;
            } catch (Exception e) {
                Log.Error("Loading '" + path + "' failed with error: '" + e.Message + "'.");
                return null;
            }
        }

        private static void addToImageCache(string pId, SKBitmap pImg) {
            _imageCache.Add(pId, pImg);
        }
    }
}
