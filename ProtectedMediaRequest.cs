using System.Web;
using Sitecore.Diagnostics;
using Sitecore.Resources.Media;

namespace Cognifide.ImageGuard
{
    public class ProtectedMediaRequest : MediaRequest
    {
        protected override MediaOptions GetOptions()
        {
            var result = base.GetOptions();

            if (result.Height > 0 || result.Width > 0 || result.Scale > 0)
            {
                if (!HashingUtils.IsUrlHashValid(InnerRequest.RawUrl))
                {
                    // hash invalid remove scaling
                    result.Height = 0;
                    result.Width = 0;
                    result.Scale = 0;
                }
            }
            return result;        
        }

        public override MediaRequest Clone()
        {
            Assert.IsTrue(this.GetType() == typeof(ProtectedMediaRequest), "The Clone() method must be overriden to support prototyping.");
            return (ProtectedMediaRequest)MemberwiseClone();
        }

        protected override string GetMediaPath()
        {
            var result = base.GetMediaPath();
            return result;
        }
        protected override string GetMediaPath(string localPath)
        {
            var result = base.GetMediaPath(localPath);
            return result;
        }

        public override void Initialize(System.Web.HttpRequest request)
        {
            base.Initialize(request);
        }

    public ProtectedMediaRequest() : base()
    {
    }

    public ProtectedMediaRequest(HttpRequest request)
        : base(request)
    {
    }

    }
}
