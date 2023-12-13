using MinimalRazor.Pages;
using RazorBlade;
using RazorBlade.Support;

namespace MinimalRazor.Models;

internal abstract class ViewTemplate<TModel> : HtmlTemplate
{
	[TemplateConstructor]
	public ViewTemplate(TModel model, HttpContext? context = null)
    {
        Model = model;
        Context = context;
		LayoutMain = context is null ? new LayoutPublic() : new Layout { User = Context?.User };
    }

    public TModel Model { get; set; }
	public HttpContext? Context { get; set; }
    public HtmlLayout? LayoutMain { get; set; }
}