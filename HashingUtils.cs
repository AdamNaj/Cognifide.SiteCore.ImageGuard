using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Resources.Media;
using Sitecore.Text;
using Convert = System.Convert;

namespace Cognifide.ImageGuard
{
    public class HashingUtils
    {
        private static bool initialized;
        private static byte[] saltBytes;
        private static string sharedSecret = "Sitecore is awesome!";

        public static List<string> DANGEROUS_PARAMETERS =
            new List<string>()
            {
                "?w=", "?h=", "?sc=", "?mw=", "?mh=",
                "&w=", "&h=", "&sc=", "&mw=", "&mh="
            };

        public static Dictionary<string, string> MediaItemCache = new Dictionary<string, string>();

        public static void Initialize()
        {
            if (!initialized)
            {
                sharedSecret = Settings.GetSetting("ImageGuard.SharedSecret", string.Empty);
                saltBytes = Encoding.UTF8.GetBytes(sharedSecret);
                initialized = true;
            }
        }

        public static string ComputeHash(string plainText)
        {
            Initialize();
            byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] plainTextWithSaltBytes =
                new byte[plainTextBytes.Length + saltBytes.Length];
            for (int i = 0; i < plainTextBytes.Length; i++)
                plainTextWithSaltBytes[i] = plainTextBytes[i];
            for (int i = 0; i < saltBytes.Length; i++)
                plainTextWithSaltBytes[plainTextBytes.Length + i] = saltBytes[i];
            HashAlgorithm hash = new SHA1Managed();
            byte[] hashBytes = hash.ComputeHash(plainTextWithSaltBytes);

            // Convert result into a base64-encoded string.
            string hashValue = Convert.ToBase64String(hashBytes).Replace("=", "_").Replace("/", "_").Replace("+", "_").Replace("?", "_").Replace("&", "_");
            return hashValue;
        }

        public static bool IsDangerousAssetUrl(string url)
        {
            int paramIndex = url.IndexOf("?", StringComparison.Ordinal);
            if (paramIndex > 0) //contains parameters
            {
                url = url.ToLower();
                foreach (string parameter in DANGEROUS_PARAMETERS)
                {
                    if (url.IndexOf(parameter, paramIndex, StringComparison.Ordinal) > -1)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static Database CurrentDatabase { get { return Sitecore.Context.ContentDatabase ?? Sitecore.Context.Database; } }

        public static string GetAssetUrlHash(string url)
        {
            UrlString imgUrl = new UrlString(url.ToLowerInvariant());
            string path = imgUrl.Path;
            if (!MediaItemCache.ContainsKey(path))
            {
                Item mediaItem = CurrentDatabase.GetItem(GetMediaPath(path));
                if (mediaItem == null)
                {
                    return url;
                }
                MediaItemCache.Add(path, mediaItem.ID.ToString());
            }
            if (MediaItemCache.ContainsKey(path))
            {
                string width = imgUrl.Parameters["w"];
                string height = imgUrl.Parameters["h"];
                string maxWidth = imgUrl.Parameters["mh"];
                string maxHeight = imgUrl.Parameters["mw"];
                string scale = imgUrl.Parameters["sc"];
                return
                    ComputeHash(string.Format("{0}?w={1}&h={2}&sc={3}&mw={4}&mh={5}",
                        new object[] {MediaItemCache[path], width, height, scale, maxHeight, maxWidth}));
            }
            return url;
        }

        public static string ProtectAssetUrl(string url)
        {
            return url + "&amp;hash=" + GetAssetUrlHash(url);
        }

        public static bool IsUrlHashValid(string url)
        {
            var hash = GetAssetUrlHash(url);
            UrlString parsedUrl = new UrlString(url);
            var urlHash = parsedUrl.Parameters["hash"];
            return hash.Equals(urlHash, StringComparison.OrdinalIgnoreCase);
        }

        protected static string GetMediaPath(string localPath)
        {
            int indexA = -1;
            string strB = string.Empty;
            foreach (string str in MediaManager.Provider.Config.MediaPrefixes)
            {
                indexA = localPath.IndexOf(str, StringComparison.InvariantCultureIgnoreCase);
                if (indexA >= 0)
                {
                    strB = str;
                    break;
                }
            }
            if (indexA < 0 ||
                string.Compare(localPath, indexA, strB, 0, strB.Length, true, CultureInfo.InvariantCulture) != 0)
                return string.Empty;
            string id = StringUtil.Divide(StringUtil.Mid(localPath, indexA + strB.Length), '.', true)[0];
            if (id.EndsWith("/"))
                return string.Empty;
            if (ShortID.IsShortID(id))
                return ShortID.Decode(id);
            return ("/sitecore/media library/" + id.TrimStart(new[] {'/'}));
        }

    }

}