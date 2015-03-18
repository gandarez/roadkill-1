using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using System.Net;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Framework.Common;
using Roadkill.Core.Database;
using Roadkill.Core.Mvc.ViewModels;
using Roadkill.Core.Plugins;

namespace Roadkill.Plugins.TFS
{
    public class Tfs : TextPlugin
    {
        private readonly IRepository _repository;
        private static readonly Regex Regex = new Regex(@"(?<!{)(?:\{TFS\})(?!})", RegexOptions.Compiled);
        private static readonly Regex Regex2 = new Regex(@"(?<!{)(?:\{(TFS):([a-zA-Z]+)\})(?!})", RegexOptions.Compiled);
        private TfsConfigurationServer _tfsConfigurationServer;

        public override string Id
        {
            get { return "TFS"; }
        }

        public override string Name
        {
            get { return "TFS"; }
        }

        public override string Description
        {
            get { return "Rturn a list of TFS collections. Usage {TFS}. To get a list of Team Projects please refer to: {TFS:TeamProjectName}."; }
        }

        public override string Version
        {
            get { return "1.0.0"; }
        }

        public Tfs(IRepository repository)
        {
            _repository = repository;
        }

        private void ConnectToTfs()
        {
            var uri = new Uri(Settings.GetValue("TFS-ADDRESS"));
            var user = Settings.GetValue("TFS-USER");
            var pass = Settings.GetValue("TFS-PASSWORD");
            var domain = Settings.GetValue("TFS-CREDENTIAL-DOMAIN");

            _tfsConfigurationServer = new TfsConfigurationServer(uri, new NetworkCredential(user, pass, domain));

            if (_tfsConfigurationServer == null)
                throw new Exception(string.Format("Unable to connect to TFS Server. ({0}).", uri.AbsolutePath));
        }

        public override void OnInitializeSettings(Settings settings)
        {
            settings.SetValue("TFS-ADDRESS", "http://SERVER:8080/tfs");
            settings.SetValue("TFS-USER", "dev");
            settings.SetValue("TFS-PASSWORD", "pass");
            settings.SetValue("TFS-CREDENTIAL-DOMAIN", "domain.com");
        }

        private void HandleCollections(ref string html)
        {
            try
            {
                var match = Regex.Match(html);
                if (!match.Success) return;

                ConnectToTfs();

                var collectionNodes = _tfsConfigurationServer.CatalogNode.QueryChildren(
                        new[] { CatalogResourceTypes.ProjectCollection },
                        false, CatalogQueryOptions.None).OrderBy(p => p.Resource.DisplayName);

                var pages = _repository.FindPagesContainingTag("tfs");

                var helper = new UrlHelper(HttpContext.Current.Request.RequestContext);
                var sb = new StringBuilder();
                sb.AppendLine("<ul id=\"tfs\">");

                foreach (var collection in collectionNodes)
                {
                    var teamProjectsCount = collectionNodes.First(p => p.Resource.DisplayName == collection.Resource.DisplayName)
                        .QueryChildren(new[] { CatalogResourceTypes.TeamProject }, false, CatalogQueryOptions.IncludeParents)
                        .Count();

                    var page = pages.FirstOrDefault(p => p.Title == string.Format("TFS {0}", collection.Resource.DisplayName));

                    if (page == null)
                        sb.AppendFormat("<li>{0} {1} ({2} project(s))</li>", collection.Resource.DisplayName, collection.Resource.Description, teamProjectsCount);
                    else
                        sb.AppendFormat("<li><a href=\"{0}\">{1}</a> ({2} projects(s))</li>",
                            helper.Action("Index", "Wiki", new
                            {
                                id = page.Id,
                                title = PageViewModel.EncodePageTitle(page.Title)
                            }),
                            string.Format("{0} {1}", collection.Resource.DisplayName, collection.Resource.Description), teamProjectsCount);
                }
                sb.AppendLine("</ul>");
                html = Regex.Replace(html, sb.ToString());
            }
            catch (Exception e)
            {
                html = Regex.Replace(html, e.Message);
            }
        }

        private void HandleTeamProjects(ref string html)
        {
            try
            {
                var match = Regex2.Match(html);
                if (!match.Success) return;

                var collectionName = match.Groups[2].Value;

                ConnectToTfs();

                var collectionNodes = _tfsConfigurationServer.CatalogNode.QueryChildren(
                        new[] { CatalogResourceTypes.ProjectCollection },
                        false, CatalogQueryOptions.None);
                var teamProjects =
                    collectionNodes.First(p => p.Resource.DisplayName == collectionName)
                        .QueryChildren(new[] { CatalogResourceTypes.TeamProject }, false,
                            CatalogQueryOptions.IncludeParents)
                        .OrderBy(p => p.Resource.DisplayName);

                var sb = new StringBuilder();
                sb.AppendLine("<ul id=\"tfs\">");
                foreach (var teamProject in teamProjects)
                {
                    var description = teamProject.Resource.Description.StartsWith("-")
                        ? teamProject.Resource.Description
                        : string.IsNullOrEmpty(teamProject.Resource.Description)
                            ? string.Empty
                            : string.Concat("- ", teamProject.Resource.Description);

                    sb.AppendFormat("<li>{0} {1}</li>", teamProject.Resource.DisplayName, description);
                }
                sb.AppendLine("</ul>");
                html = Regex2.Replace(html, sb.ToString());
            }
            catch (Exception e)
            {
                html = Regex.Replace(html, e.Message);
            }
        }

        public override string AfterParse(string html)
        {
            HandleCollections(ref html);
            HandleTeamProjects(ref html);

            return html;
        }
    }
}
