using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text;

namespace SteamToolsBot.Helpers;

internal sealed class HtmlStringBuilderGenerator
{
	private readonly StringBuilder _stringBuilder;

	public HtmlStringBuilderGenerator(StringBuilder stringBuilder) => _stringBuilder = stringBuilder;

	public IDisposable Bold() => new TagAppender(_stringBuilder, "b");

	public IDisposable Link(string link) => new LinkAppender(_stringBuilder, link);

	private class TagAppender : IDisposable
	{
		private readonly StringBuilder _stringBuilder;
		private readonly string _tag;

		public TagAppender(StringBuilder stringBuilder, string tag, IReadOnlyDictionary<string, string>? attributes = null)
		{
			_tag = tag;
			stringBuilder.Append(CultureInfo.InvariantCulture, $"<{tag}");
			if (attributes is {Count: > 0})
			{
				foreach (var (name, value) in attributes)
				{
					stringBuilder.Append(' ');
					stringBuilder.Append(WebUtility.HtmlEncode(name));
					stringBuilder.Append("=\"");
					stringBuilder.Append(WebUtility.HtmlEncode(value));
					stringBuilder.Append('"');
				}
			}

			stringBuilder.Append('>');
			_stringBuilder = stringBuilder;
		}

		public void Dispose()
		{
			_stringBuilder.Append(CultureInfo.InvariantCulture, $"</{_tag}>");
		}
	}

	private sealed class LinkAppender : TagAppender
	{
		public LinkAppender(StringBuilder stringBuilder, string link) : base(
			stringBuilder, "a", new Dictionary<string, string>(1) {["href"] = link})
		{
		}
	}
}
