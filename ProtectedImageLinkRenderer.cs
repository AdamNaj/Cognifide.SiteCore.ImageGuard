using System;
using System.Text;
using Sitecore.Pipelines.RenderField;

namespace Cognifide.ImageGuard
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
            int tagIndex = 0;
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
            
            // copy the non-problematic part of the content first
            StringBuilder builder = new StringBuilder(renderedText, 0, tagIndex, renderedText.Length + 128);
            
            //start with the problematic content
            index = tagIndex;
            while (index < renderedText.Length)
            {
                tagIndex = renderedText.IndexOf("<img", index, StringComparison.OrdinalIgnoreCase);
                if (tagIndex > -1)
                {
                    builder.Append(renderedText.Substring(index, tagIndex - index));
                    ReplaceReference(renderedText, builder, tagIndex, ref index);
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


        private void ReplaceReference(string renderedText, StringBuilder builder, int tagIndex, ref int index)
        {
            int urlStartIndex = renderedText.IndexOf("src", tagIndex, StringComparison.OrdinalIgnoreCase) + 3;
            urlStartIndex = renderedText.IndexOfAny(quotes, urlStartIndex) + 1;
            int urlEndIndex = renderedText.IndexOfAny(quotes, urlStartIndex);
            index = urlEndIndex;
            if (renderedText.IndexOf("?", urlStartIndex, urlEndIndex - urlStartIndex, StringComparison.Ordinal) < 0)
            {
                builder.Append(renderedText.Substring(tagIndex, urlEndIndex - tagIndex));
                return; // no parameters, no need to arm the URL;
            }
            string url = renderedText.Substring(urlStartIndex, urlEndIndex - urlStartIndex);
            url = HashingUtils.ProtectAssetUrl(url);
            builder.Append(renderedText.Substring(tagIndex, urlStartIndex - tagIndex));
            builder.Append(url);
            // we don't need to copy the rest of the tag as it will be supplemented in the next runs
        }

    }
}
