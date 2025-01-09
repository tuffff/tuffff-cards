namespace tuffCards.Markdown;

public class ImageParser : InlineParser {
	public ImageParser() {
		OpeningCharacters = new[] { '{' };
	}

	public override bool Match(InlineProcessor processor, ref StringSlice slice) {
		var isImage = false;

		slice.NextChar();
		if (slice.CurrentChar == '{') {
			isImage = true;
			slice.NextChar();
		}

		var current = slice.CurrentChar;
		var start = slice.Start;
		var end = start;

		while (current.IsAlphaNumeric() || current == '-' || current == '_')
		{
			end = slice.Start;
			current = slice.NextChar();
		}
		if (slice.CurrentChar != '}') {
			return false;
		}
		slice.NextChar();
		if (isImage) {
			if (slice.CurrentChar != '}') {
				return false;
			}
			slice.NextChar();
		}

		var inlineStart = processor.GetSourcePosition
			(slice.Start, out var line, out var column);
		var result = new StringSlice(slice.Text, start, end).ToString();
		processor.Inline = new Image
		{
			Span =
			{
				Start = inlineStart,
				End = inlineStart + (end - start) + 1
			},
			Line = line,
			Column = column,
			Name = result,
			IsIcon = !isImage
		};

		return true;
	}
}
