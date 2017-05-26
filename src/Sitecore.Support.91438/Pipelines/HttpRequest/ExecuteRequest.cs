using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Layouts;
using Sitecore.Pipelines.HttpRequest;
using Sitecore.SecurityModel;
using Sitecore.Sites;
using Sitecore.Text;
using Sitecore.Web;
using System.Web;
using Sitecore;

namespace Sitecore.Support.Pipelines.HttpRequest
{
    public class ExecuteRequest : HttpRequestProcessor
    {
        /// <summary>
        /// Runs the processor.
        /// </summary>
        /// <param name="args">The arguments.</param>
        public override void Process(HttpRequestArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            SiteContext site = Sitecore.Context.Site;
            if (site != null && !SiteManager.CanEnter(site.Name, Sitecore.Context.User))
            {
                this.HandleSiteAccessDenied(site, args);
                return;
            }
            PageContext page = Context.Page;
            Assert.IsNotNull(page, "No page context in processor.");
            string filePath = page.FilePath;
            if (filePath.Length > 0)
            {
                if (WebUtil.IsExternalUrl(filePath))
                {
                    args.Context.Response.Redirect(filePath, true);
                    return;
                }
                if (string.Compare(filePath, HttpContext.Current.Request.Url.LocalPath, System.StringComparison.InvariantCultureIgnoreCase) != 0)
                {
                    args.Context.RewritePath(filePath, args.Context.Request.PathInfo, args.Url.QueryString, false);
                    return;
                }
            }
            else
            {
                if (Context.Item == null)
                {
                    this.HandleItemNotFound(args);
                    return;
                }
                this.HandleLayoutNotFound(args);
            }
        }

        /// <summary>
        /// Redirects to login page.
        /// </summary>
        /// <param name="url">The URL.</param>
        protected virtual void RedirectToLoginPage(string url)
        {
            UrlString urlString = new UrlString(url);
            if (string.IsNullOrEmpty(urlString["returnUrl"]))
            {
                urlString["returnUrl"] = WebUtil.GetRawUrl();
                urlString.Parameters.Remove("item");
                urlString.Parameters.Remove("user");
                urlString.Parameters.Remove("site");
            }
            WebUtil.Redirect(urlString.ToString(), false);
        }

        /// <summary>
        /// Preforms redirect on 'item not found' condition.
        /// </summary>
        /// <param name="url">The URL.</param>
        protected virtual void RedirectOnItemNotFound(string url)
        {
            this.PerformRedirect(url);
        }

        /// <summary>
        /// Redirects on 'no access' condition.
        /// </summary>
        /// <param name="url">The URL.</param>
        protected virtual void RedirectOnNoAccess(string url)
        {
            this.PerformRedirect(url);
        }

        /// <summary>
        /// Redirects the on 'site access denied' condition.
        /// </summary>
        /// <param name="url">The URL.</param>
        protected virtual void RedirectOnSiteAccessDenied(string url)
        {
            this.PerformRedirect(url);
        }

        /// <summary>
        /// Redirects on 'layout not found' condition.
        /// </summary>
        /// <param name="url">The URL.</param>
        protected virtual void RedirectOnLayoutNotFound(string url)
        {
            this.PerformRedirect(url);
        }

        /// <summary>
        /// Redirects request to the specified URL.
        /// </summary>
        /// <param name="url">The URL.</param>
        protected virtual void PerformRedirect(string url)
        {
            if (Settings.RequestErrors.UseServerSideRedirect)
            {
                HttpContext.Current.Server.Transfer(url);
                return;
            }
            WebUtil.Redirect(url, false);
        }

        /// <summary>
        /// Gets the no access URL.
        /// </summary>
        /// <returns>The no access URL.</returns>
        private string GetNoAccessUrl(out bool loginPage)
        {
            SiteContext site = Context.Site;
            loginPage = false;
            if (site == null || site.LoginPage.Length <= 0)
            {
                Tracer.Warning("Redirecting to \"No Access\" page as no login page was found.");
                return Settings.NoAccessUrl;
            }
            if (SiteManager.CanEnter(site.Name, Context.User))
            {
                Tracer.Info("Redirecting to login page \"" + site.LoginPage + "\".");
                loginPage = true;
                return site.LoginPage;
            }
            Tracer.Info(string.Concat(new string[]
            {
                "Redirecting to the 'No Access' page as the current user '",
                Context.User.Name,
                "' does not have sufficient rights to enter the '",
                site.Name,
                "' site."
            }));
            return Settings.NoAccessUrl;
        }

        /// <summary>
        /// Handles the item not found.
        /// </summary>
        /// <param name="args">The arguments.</param>
        private void HandleItemNotFound(HttpRequestArgs args)
        {
            string localPath = args.LocalPath;
            string name = Context.User.Name;
            bool flag = false;
            bool flag2 = false;
            string url = Settings.ItemNotFoundUrl;
            if (args.PermissionDenied)
            {
                flag = true;
                url = this.GetNoAccessUrl(out flag2);
            }
            SiteContext site = Context.Site;
            string text = (site != null) ? site.Name : string.Empty;
            System.Collections.Generic.List<string> list = new System.Collections.Generic.List<string>(new string[]
            {
                "item",
                localPath,
                "user",
                name,
                "site",
                text
            });
            if (Settings.Authentication.SaveRawUrl)
            {
                list.AddRange(new string[]
                {
                    "returnurl",
                    Context.RawUrl
                });
            }
            url = WebUtil.AddQueryString(url, list.ToArray());
            if (!flag)
            {
                Log.Warn(string.Format("Request is redirected to document not found page. Requested url: {0}, User: {1}, Website: {2}", Context.RawUrl, name, text), this);
                this.RedirectOnItemNotFound(url);
                return;
            }
            if (flag2)
            {
                Log.Warn(string.Format("Request is redirected to login page. Requested url: {0}, User: {1}, Website: {2}", Context.RawUrl, name, text), this);
                this.RedirectToLoginPage(url);
            }
            Log.Warn(string.Format("Request is redirected to access denied page. Requested url: {0}, User: {1}, Website: {2}", Context.RawUrl, name, text), this);
            this.RedirectOnNoAccess(url);
        }

        /// <summary>
        /// Handles the layout not found.
        /// </summary>
        /// <param name="args">The arguments.</param>
        private void HandleLayoutNotFound(HttpRequestArgs args)
        {
            string text = string.Empty;
            string text2 = string.Empty;
            string text3 = string.Empty;
            string text4 = "Request is redirected to no layout page.";
            DeviceItem device = Context.Device;
            if (device != null)
            {
                text2 = device.Name;
            }
            Item item = Context.Item;
            if (item != null)
            {
                text4 = text4 + " Item: " + item.Uri;
                if (device != null)
                {
                    text4 += string.Format(" Device: {0} ({1})", device.ID, device.InnerItem.Paths.Path);
                    text = item.Visualization.GetLayoutID(device).ToString();
                    if (text.Length > 0)
                    {
                        Database database = Context.Database;
                        Assert.IsNotNull(database, "No database on processor.");
                        Item item2 = ItemManager.GetItem(text, Language.Current, Sitecore.Data.Version.Latest, database, SecurityCheck.Disable);
                        if (item2 != null && !item2.Access.CanRead())
                        {
                            SiteContext site = Context.Site;
                            string text5 = (site != null) ? site.Name : string.Empty;
                            text3 = WebUtil.AddQueryString(Settings.NoAccessUrl, new string[]
                            {
                                "item",
                                string.Concat(new string[]
                                {
                                    "Layout: ",
                                    text,
                                    " (item: ",
                                    args.LocalPath,
                                    ")"
                                }),
                                "user",
                                Context.GetUserName(),
                                "site",
                                text5,
                                "device",
                                text2
                            });
                        }
                    }
                }
            }
            if (text3.Length == 0)
            {
                text3 = WebUtil.AddQueryString(Settings.LayoutNotFoundUrl, new string[]
                {
                    "item",
                    args.LocalPath,
                    "layout",
                    text,
                    "device",
                    text2
                });
            }
            Log.Warn(text4, this);
            this.RedirectOnLayoutNotFound(text3);
        }

        /// <summary>
        /// Handles 'site access denied'.
        /// </summary>
        private void HandleSiteAccessDenied(SiteContext site, HttpRequestArgs args)
        {
            string url = Settings.NoAccessUrl;
            url = WebUtil.AddQueryString(url, new string[]
            {
                "item",
                args.LocalPath,
                "user",
                Context.GetUserName(),
                "site",
                site.Name,
                "right",
                "site:enter"
            });
            Log.Warn(string.Format("Request is redirected to access denied page. Requested url: {0}, User: {1}, Website: {2}", Context.RawUrl, Context.GetUserName(), site.Name), this);
            this.RedirectOnSiteAccessDenied(url);
        }
    }
}