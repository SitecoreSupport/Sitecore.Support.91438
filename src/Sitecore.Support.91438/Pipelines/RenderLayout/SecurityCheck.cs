using Sitecore.Configuration;
using Sitecore.Diagnostics;
using Sitecore.Pipelines.RenderLayout;
using Sitecore.Publishing;
using Sitecore.Sites;
using Sitecore.Text;
using Sitecore.Web;
using System;
using System.Web;

namespace Sitecore.Support.Pipelines.RenderLayout
{
    public class SecurityCheck : Sitecore.Pipelines.RenderLayout.SecurityCheck
    {
        /// <summary>
        /// Process query string.
        /// </summary>
        /// <param name="args">The arguments.</param>
        public override void Process(RenderLayoutArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            Profiler.StartOperation("Check security access to page.");
            if (!this.HasAccess())
            {
                args.AbortPipeline();
                SiteContext site = Sitecore.Context.Site;
                string loginPage = this.GetLoginPage(site);
                if (loginPage.Length > 0)
                {
                    Tracer.Info("Redirecting to login page \"" + loginPage + "\".");
                    UrlString urlString = new UrlString(loginPage);
                    if (Settings.Authentication.SaveRawUrl)
                    {
                        urlString.Append("returnurl", Sitecore.Context.RawUrl);
                    }
                    WebUtil.Redirect(urlString.ToString(), false);
                }
                else
                {
                    Tracer.Info("Redirecting to error page as no login page was found.");
                    WebUtil.RedirectToErrorPage("Login is required, but no valid login page has been specified for the site (" + Sitecore.Context.Site.Name + ").", false);
                }
            }
            Profiler.EndOperation();
        }        
    }
}