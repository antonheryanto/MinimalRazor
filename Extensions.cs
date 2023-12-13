using RazorBlade;

namespace MinimalRazor;

internal static class Extensions
{
	public static IResult ToHtml(this HtmlTemplate view) => Results.Content(view.Render(), "text/html");
}

