using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Net;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Framework.Common;
using Microsoft.TeamFoundation.Framework.Client;
using Roadkill.Core.Database;
using Roadkill.Core.Mvc.ViewModels;
using Roadkill.Core.Plugins;

namespace Roadkill.Plugins.TFS
{
    public class Tfs : TextPlugin
    {
        private Roadkill.Core.Database.IRepository _repository;
        private static Regex _regex = new Regex(@"(?<!{)(?:\{TFS\})(?!})", RegexOptions.Compiled);
        private static Regex _regex2 = new Regex(@"(?<!{)(?:\{(TFS):([a-zA-Z]+)\})(?!})", RegexOptions.Compiled);
        private readonly Uri _tfsUri = new Uri("http://SERVER:8080/tfs");
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

        public Tfs(Roadkill.Core.Database.IRepository repository)
        {
            _repository = repository;
        }

        private void ConnectToTfs()
        {
            _tfsConfigurationServer = new TfsConfigurationServer(_tfsUri, new NetworkCredential("dev", "fator@123", "FSP-SRV-TFS-P01"));

            if (_tfsConfigurationServer == null)
                throw new Exception(string.Format("Unable to connect to TFS Server. ({0}).", _tfsUri.AbsolutePath));
        }

        private void HandleCollections(ref string html)
        {
            try
            {
                var match = _regex.Match(html);
                if (!match.Success) return;

                ConnectToTfs();

                var collectionNodes = _tfsConfigurationServer.CatalogNode.QueryChildren(
                        new[] { CatalogResourceTypes.ProjectCollection },
                        false, CatalogQueryOptions.None).OrderBy(p => p.Resource.DisplayName);

                var pages = _repository.FindPagesContainingTag("tfs");

                UrlHelper helper = new UrlHelper(HttpContext.Current.Request.RequestContext);
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("<ul id=\"tfs\">");

                foreach (var collection in collectionNodes)
                {
                    var teamProjectsCount = collectionNodes.First(p => p.Resource.DisplayName == collection.Resource.DisplayName)
                        .QueryChildren(new[] { CatalogResourceTypes.TeamProject }, false, CatalogQueryOptions.IncludeParents)
                        .Count();

                    var page = pages.FirstOrDefault(p => p.Title == string.Format("TFS {0}", collection.Resource.DisplayName));

                    if (page == null)
                        sb.AppendFormat("<li>{0} {1} ({2} projeto(s))</li>", collection.Resource.DisplayName, collection.Resource.Description, teamProjectsCount);
                    else
                        sb.AppendFormat("<li><a href=\"{0}\">{1}</a> ({2} projeto(s))</li>",
                            helper.Action("Index", "Wiki", new
                            {
                                id = page.Id,
                                title = PageViewModel.EncodePageTitle(page.Title)
                            }),
                            string.Format("{0} {1}", collection.Resource.DisplayName, collection.Resource.Description), teamProjectsCount);
                }
                sb.AppendLine("</ul>");
                html = _regex.Replace(html, sb.ToString());
            }
            catch (Exception e)
            {
                html = _regex.Replace(html, e.Message);
            }
        }

        private void HandleTeamProjects(ref string html)
        {
            try
            {
                var match = _regex2.Match(html);
                if (!match.Success) return;

                var collectionName = match.Groups[2].Value;

                ConnectToTfs();
                
                var collectionNodes = _tfsConfigurationServer.CatalogNode.QueryChildren(
                        new[] { CatalogResourceTypes.ProjectCollection },
                        false, CatalogQueryOptions.None);
                var teamProjects = collectionNodes.First(p => p.Resource.DisplayName == collectionName).QueryChildren(new[] { CatalogResourceTypes.TeamProject }, false, CatalogQueryOptions.IncludeParents).OrderBy(p => p.Resource.DisplayName);

                StringBuilder sb = new StringBuilder();
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
                html = _regex2.Replace(html, sb.ToString());
            }
            catch (Exception e)
            {
                html = _regex.Replace(html, e.Message);
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
