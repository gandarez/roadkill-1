#region

using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using Roadkill.Core.Database;
using Roadkill.Core.Mvc.ViewModels;
using Roadkill.Core.Plugins;

#endregion

namespace Roadkill.Plugins.Top10
{
	public class Top10 : TextPlugin
	{
		private readonly IRepository _repository;
		private static readonly Regex Regex = new Regex(@"(?<!{)(?:\{Top10\})(?!})", RegexOptions.Compiled);

		public override string Id
		{
			get { return "Top10"; }
		}

		public override string Name
		{
			get { return "Top Ten Pages"; }
		}

		public override string Description
		{
			get { return "Displays the top new/modified pages."; }
		}

		public override string Version
		{
			get { return "1.0.0"; }
		}

		public Top10(IRepository repository)
		{
			_repository = repository;
		}

        public override void OnInitializeSettings(Settings settings)
        {
            settings.SetValue("Quantity", "10");
        }

		public override string AfterParse(string html)
		{
            var match = Regex.Match(html);
            if (!match.Success) return html;

            var qty = 10;
            int.TryParse(Settings.GetValue("Quantity"), out qty);

			var topPages = _repository.AllPages().OrderByDescending(p => p.ModifiedOn).Take(10).ToList();
			if (topPages.Count == 0) return Regex.Replace(html, string.Empty);

			var helper = new UrlHelper(HttpContext.Current.Request.RequestContext);
			var sb = new StringBuilder();

			sb.AppendLine("<ol class=\"clear\">");
			foreach (var page in topPages) {
				sb.AppendFormat("<li><a href=\"{0}\">{1}</a> <span class=\"smaller\">{2} {3}</span></li>", helper.Action("Index", "Wiki", new {
				    id = page.Id,
				    title = PageViewModel.EncodePageTitle(page.Title)
				}), page.Title, page.ModifiedOn.ToLongDateString(), page.ModifiedOn.ToShortTimeString());
			}
			sb.AppendLine("</ol>");
			return Regex.Replace(html, sb.ToString());
		}
	}
}