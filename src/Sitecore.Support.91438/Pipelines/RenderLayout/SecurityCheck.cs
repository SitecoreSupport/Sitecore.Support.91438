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
    public class SecurityCheck : RenderLayoutProcessor
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

        /// <summary>
        /// Gets the login page.
        /// </summary>
        /// <param name="site">The site.</param>
        /// <returns></returns>
        protected virtual string GetLoginPage(SiteContext site)
        {
            if (site == null)
            {
                return string.Empty;
            }
            if (site.DisplayMode == DisplayMode.Normal)
            {
                return site.LoginPage;
            }
            SiteContext site2 = SiteContext.GetSite("shell");
            if (site2 != null)
            {
                return site2.LoginPage;
            }
            return string.Empty;
        }

        /// <summary>
        /// Check credentials.
        /// </summary>
        /// <returns>
        /// 	<c>true</c> if this instance has access; otherwise, <c>false</c>.
        /// </returns>
        protected virtual bool HasAccess()
        {
            Tracer.Info("Checking security for current user \"" + Sitecore.Context.User.Name + "\".");
            SiteContext site = Sitecore.Context.Site;
            if (site != null && site.RequireLogin && !Sitecore.Context.User.IsAuthenticated && !this.IsLoginPageRequest())
            {
                Tracer.Warning("Site \"" + site.Name + "\" requires login and no user is logged in.");
                return false;
            }
            if (site != null && site.DisplayMode != DisplayMode.Normal && !Sitecore.Context.User.IsAuthenticated && PreviewManager.GetShellUser() == string.Empty && !this.IsLoginPageRequest())
            {
                Tracer.Warning("Current display mode is \"" + site.DisplayMode + "\" and no user is logged in.");
                return false;
            }
            if (Sitecore.Context.Item == null)
            {
                Tracer.Info("Access is granted as there is no current item.");
                return true;
            }
            if (Sitecore.Context.Item.Access.CanRead())
            {
                Tracer.Info("Access granted as the current user \"" + Sitecore.Context.GetUserName() + "\" has read access to current item.");
                return true;
            }
            Tracer.Warning(string.Concat(new string[]
            {
                "The current user \"",
                Sitecore.Context.GetUserName(),
                "\" does not have read access to the current item \"",
                Sitecore.Context.Item.Paths.Path,
                "\"."
            }));
            return false;
        }

        /// <summary>
        /// Determines whether current request addresses the login page of the site.
        /// </summary>
        /// <returns>
        /// 	<c>true</c> if current request addresses the login page of the site; otherwise, <c>false</c>.
        /// </returns>
        protected virtual bool IsLoginPageRequest()
        {
            SiteContext site = Sitecore.Context.Site;
            string loginPage = this.GetLoginPage(site);
            return !string.IsNullOrEmpty(loginPage) && HttpContext.Current.Request.RawUrl.StartsWith(loginPage, System.StringComparison.InvariantCultureIgnoreCase);
        }
    }
}