#pragma warning disable 1591
// ------------------------------------------------------------------------------
//  <autogenerated>
//      This code was generated by a tool.
//      Mono Runtime Version: 4.0.30319.17020
// 
//      Changes to this file may cause incorrect behavior and will be lost if 
//      the code is regenerated.
//  </autogenerated>
// ------------------------------------------------------------------------------

namespace staccato
{
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#line 1 "/home/sircmpwn/sources/staccato/staccato/Views/Index/IrcHelp.cshtml"
using staccato;

#line default
#line hidden

#line 2 "/home/sircmpwn/sources/staccato/staccato/Views/Index/IrcHelp.cshtml"
using ChatSharp;

#line default
#line hidden


[System.CodeDom.Compiler.GeneratedCodeAttribute("RazorTemplatePreprocessor", "2.6.0.0")]
public partial class IrcHelp : IrcHelpBase
{

#line hidden

public override void Execute()
{
WriteLiteral("<h1>IRC Help</h1>\n");


#line 4 "/home/sircmpwn/sources/staccato/staccato/Views/Index/IrcHelp.cshtml"
 if (!Program.Configuration.Irc.Enabled) {


#line default
#line hidden
WriteLiteral("    <p>IRC is not enabled for this server.</p>\n");


#line 6 "/home/sircmpwn/sources/staccato/staccato/Views/Index/IrcHelp.cshtml"
} else {


#line default
#line hidden
WriteLiteral("    <p>Staccato is currently running a bot in these channels:</p>\n");

WriteLiteral("    <ul>\n");


#line 9 "/home/sircmpwn/sources/staccato/staccato/Views/Index/IrcHelp.cshtml"
        

#line default
#line hidden

#line 9 "/home/sircmpwn/sources/staccato/staccato/Views/Index/IrcHelp.cshtml"
         foreach (var channel in Program.IrcBot.Client.Channels)
        {


#line default
#line hidden
WriteLiteral("            <li>");


#line 11 "/home/sircmpwn/sources/staccato/staccato/Views/Index/IrcHelp.cshtml"
           Write(channel.Name);


#line default
#line hidden
WriteLiteral("</li>\n");


#line 12 "/home/sircmpwn/sources/staccato/staccato/Views/Index/IrcHelp.cshtml"
        }


#line default
#line hidden
WriteLiteral("    </ul>\n");


#line 14 "/home/sircmpwn/sources/staccato/staccato/Views/Index/IrcHelp.cshtml"
    if (Program.Configuration.Irc.AnnounceNowPlaying) {


#line default
#line hidden
WriteLiteral("        <p>The bot\'s name is ");


#line 15 "/home/sircmpwn/sources/staccato/staccato/Views/Index/IrcHelp.cshtml"
                        Write(Program.Configuration.Irc.Nick);


#line default
#line hidden
WriteLiteral(", and will announce new songs as they start playing.</p>\n");


#line 16 "/home/sircmpwn/sources/staccato/staccato/Views/Index/IrcHelp.cshtml"
    } else {


#line default
#line hidden
WriteLiteral("        <p>The bot\'s name is ");


#line 17 "/home/sircmpwn/sources/staccato/staccato/Views/Index/IrcHelp.cshtml"
                        Write(Program.Configuration.Irc.Nick);


#line default
#line hidden
WriteLiteral(".</p>\n");


#line 18 "/home/sircmpwn/sources/staccato/staccato/Views/Index/IrcHelp.cshtml"
    }


#line default
#line hidden
WriteLiteral("    <p>You can use various commands to interact with the bot. Simply say these co" +
"mmands in the channel:</p>\n");

WriteLiteral(@"    <dl>
        <dt>~help</dt>
        <dd>Links to this page.</dd>
        <dt>~np</dt>
        <dd>Displays the current song.</dd>
        <dt>~q</dt>
        <dd>Shows a few items from the top of the queue.</dd>
        <dt>~r [song/id]</dt>
        <dd>Requests a song. Use the song name, or the number shown in search results.</dd>
        <dt>~s [terms]</dt>
        <dd>Searches for [terms] and shows a few results.</dd>
        <dt>~sr [terms]</dt>
        <dd>Searches for [terms] and requests the first result.</dd>
        <dt>~skip</dt>
        <dd>Requests to skip the current song.</dd>
    </dl>
");


#line 36 "/home/sircmpwn/sources/staccato/staccato/Views/Index/IrcHelp.cshtml"
}

#line default
#line hidden
}
}

// NOTE: this is the default generated helper class. You may choose to extract it to a separate file 
// in order to customize it or share it between multiple templates, and specify the template's base 
// class via the @inherits directive.
public abstract class IrcHelpBase
{

		// This field is OPTIONAL, but used by the default implementation of Generate, Write and WriteLiteral
		//
		System.IO.TextWriter __razor_writer;

		// This method is OPTIONAL
		//
		///<summary>Executes the template and returns the output as a string.</summary>
		public string GenerateString ()
		{
			using (var sw = new System.IO.StringWriter ()) {
				Generate (sw);
				return sw.ToString();
			}
		}

		// This method is OPTIONAL, you may choose to implement Write and WriteLiteral without use of __razor_writer
		// and provide another means of invoking Execute.
		//
		///<summary>Executes the template, writing to the provided text writer.</summary>
		public void Generate (System.IO.TextWriter writer)
		{
			__razor_writer = writer;
			Execute ();
			__razor_writer = null;
		}

		// This method is REQUIRED, but you may choose to implement it differently
		//
		///<summary>Writes literal values to the template output without HTML escaping them.</summary>
		protected void WriteLiteral (string value)
		{
			__razor_writer.Write (value);
		}

		// This method is REQUIRED, but you may choose to implement it differently
		//
		///<summary>Writes values to the template output, HTML escaping them if necessary.</summary>
		protected void Write (object value)
		{
			WriteTo (__razor_writer, value);
		}

		// This method is REQUIRED if the template uses any Razor helpers, but you may choose to implement it differently
		//
		///<summary>Invokes the action to write directly to the template output.</summary>
		///<remarks>This is used for Razor helpers, which already perform any necessary HTML escaping.</remarks>
		protected void Write (Action<System.IO.TextWriter> write)
		{
			write (__razor_writer);
		}

		// This method is REQUIRED if the template has any Razor helpers, but you may choose to implement it differently
		//
		///<remarks>Used by Razor helpers to HTML escape values.</remarks>
		protected static void WriteTo (System.IO.TextWriter writer, object value)
		{
			if (value != null) {
				writer.Write (System.Web.HttpUtility.HtmlEncode (value.ToString ()));
				// NOTE: better version for .NET 4+, handles pre-escape HTML (IHtmlString)
				// writer.Write (System.Web.HttpUtility.HtmlEncode (value));
			}
		}

		// This method is REQUIRED. The generated Razor subclass will override it with the generated code.
		//
		///<summary>Executes the template, writing output to the Write and WriteLiteral methods.</summary>.
		///<remarks>Not intended to be called directly. Call the Generate method instead.</remarks>
		public abstract void Execute ();

}
}
#pragma warning restore 1591