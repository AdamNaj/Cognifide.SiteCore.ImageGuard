using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Sitecore.Data;
using Sitecore.Pipelines.RenderField;
using Sitecore.Text;

namespace Cognifide.SiteCore.ImageGuard
{
    class ProtectedImageLinkRenderer
    {

        public void Process(RenderFieldArgs args)
        {
            /*
            if (Sitecore.Context.PageMode.IsPageEditorEditing)
                return;
            */
            if (args.FieldTypeKey.StartsWith("__"))
                return;
            args.Result.FirstPart = HashImageReferences(args.Result.FirstPart);
            args.Result.LastPart = HashImageReferences(args.Result.LastPart);
        }

        private string HashImageReferences(string renderedText)
        {
            if (!renderedText.Contains("<img "))
            {
                return renderedText;                
            }
            else
            {
                int index = 0;
                StringBuilder buffer = new StringBuilder(renderedText.Length+128);
                while(index < renderedText.Length)
                {
                    var tagIndex = renderedText.IndexOf("<img", index, StringComparison.OrdinalIgnoreCase);
                    if (tagIndex > -1)
                    {
                        var tagCloseIndex = renderedText.IndexOf(">", tagIndex, StringComparison.OrdinalIgnoreCase) + 1;
                        buffer.Append(renderedText.Substring(index, tagIndex - index));
                        string imgTag = renderedText.Substring(tagIndex, tagCloseIndex - tagIndex);
                        buffer.Append(ReplaceReference(imgTag));
                        index = tagCloseIndex;
                    }
                    else
                    {
                        buffer.Append(renderedText.Substring(index, renderedText.Length-index));
                        index = int.MaxValue;
                    }
                }
                return buffer.ToString();
            }
        }

        private char[] quotes = new char[]{'\'', '\"'};


        private string ReplaceReference(string imgTag)
        {
            int urlStartindex = imgTag.IndexOf("src", StringComparison.OrdinalIgnoreCase)+3;
            urlStartindex = imgTag.IndexOfAny(quotes, urlStartindex) + 1;
            int urlEndIndex = imgTag.IndexOfAny(quotes, urlStartindex);
            string url = imgTag.Substring(urlStartindex, urlEndIndex - urlStartindex);
            if(!url.Contains("?"))
            {
                return imgTag; // no parameters, no need to arm the URL;
            }
            url = HashingUtils.ProtectAssetUrl(url);
            return imgTag.Substring(0,urlStartindex) + url+imgTag.Substring(urlEndIndex,imgTag.Length-urlEndIndex);
        }

        private static Database Database { get { return Sitecore.Context.ContentDatabase ?? Sitecore.Context.Database; } }
    }
}
