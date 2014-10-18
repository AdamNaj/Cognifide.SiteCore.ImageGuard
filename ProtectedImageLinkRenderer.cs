using System;
using System.Collections.Generic;
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

        private bool CheckReferenceForParams(string renderedText, int tagStart)
        {
            int srcIndex = renderedText.IndexOf("src", tagStart, StringComparison.OrdinalIgnoreCase) + 3;
            srcIndex = renderedText.IndexOfAny(quotes, srcIndex) + 1;
            int quoteIndex = renderedText.IndexOfAny(quotes, srcIndex);
            int srcParamsIndex = renderedText.IndexOf('?', srcIndex, quoteIndex - srcIndex);
            if (srcParamsIndex >= 0)
            {
                foreach (string dangerousParam in HashingUtils.DANGEROUS_PARAMETERS)
                {
                    if (renderedText.IndexOf(dangerousParam, srcParamsIndex, quoteIndex - srcParamsIndex, StringComparison.Ordinal) > -1)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private string HashImageReferences(string renderedText)
        {
            if (!renderedText.Contains("<img "))
            {
                return renderedText;
            }
            int tagIndex;
            int tagCloseIndex;
            int index = 0;
            bool containsDangerousParams = false;

            // check for anyt problematic content - maybe we don't need to do anythign?
            while ((index < renderedText.Length) && !containsDangerousParams)
            {
                tagIndex = renderedText.IndexOf("<img", index, StringComparison.OrdinalIgnoreCase);
                if (tagIndex < 0)
                {
                    break;
                }
                containsDangerousParams = CheckReferenceForParams(renderedText, tagIndex);
                tagCloseIndex = renderedText.IndexOf(">", tagIndex, StringComparison.OrdinalIgnoreCase) + 1;
                index = tagCloseIndex;
            }
            if (!containsDangerousParams)
                return renderedText;

            // ok, problematic content found - let's augment it with hash
            index = 0;
            StringBuilder builder = new StringBuilder(renderedText.Length + 128);
            while (index < renderedText.Length)
            {
                tagIndex = renderedText.IndexOf("<img", index, StringComparison.OrdinalIgnoreCase);
                if (tagIndex > -1)
                {
                    tagCloseIndex = renderedText.IndexOf(">", tagIndex, StringComparison.OrdinalIgnoreCase) + 1;
                    builder.Append(renderedText.Substring(index, tagIndex - index));
                    string imgTag = renderedText.Substring(tagIndex, tagCloseIndex - tagIndex);
                    builder.Append(ReplaceReference(imgTag));
                    index = tagCloseIndex;
                }
                else
                {
                    builder.Append(renderedText.Substring(index, renderedText.Length - index));
                    index = int.MaxValue;
                }
            }
            return builder.ToString();
        }

        private char[] quotes = new char[]{'\'', '\"'};


        private string ReplaceReference(string imgTag)
        {
            int urlStartindex = imgTag.IndexOf("src", StringComparison.OrdinalIgnoreCase) + 3;
            urlStartindex = imgTag.IndexOfAny(quotes, urlStartindex) + 1;
            int urlEndIndex = imgTag.IndexOfAny(quotes, urlStartindex);
            string url = imgTag.Substring(urlStartindex, urlEndIndex - urlStartindex);
            if (!url.Contains("?"))
            {
                return imgTag; // no parameters, no need to arm the URL;
            }
            url = HashingUtils.ProtectAssetUrl(url);
            return imgTag.Substring(0, urlStartindex) + url + imgTag.Substring(urlEndIndex, imgTag.Length - urlEndIndex);
        }

    }
}
