using System.Text;

namespace Nodsoft.YumeChan.NetRunner.Controls.Assets
{
	public class ButtonBuilder
	{
		// Properties
		public ButtonColor Color { get; set; }
		public ButtonSize Size { get; set; }

		public bool IsOutline { get; set; }


		// Contructors
		public ButtonBuilder() { } // No params ctor, used in case Button properties are set in Fields or via Fluent
		public ButtonBuilder(ButtonColor color) => Color = color; //Default ctor, requiring the most basic info

		// Fluent Access Methods
		public void SetColor(ButtonColor color) => Color = color;

		public void SetSize(ButtonSize size) => Size = size;

		public void SetOutline() => IsOutline = true;
		public void SetOutline(bool outline) => IsOutline = outline;

		// Functional Methods
		public string BuildClass()
		{
			string color = Color.ToString();
			string size = " btn-";

			switch (Size)
			{
				case ButtonSize.Large:
					size += "lg";
					break;
				case ButtonSize.Small:
					size += "sm";
					break;
				case ButtonSize.Block:
					size += "lg btn-block";
					break;
				default:
					size = null;
					break;
			}

			StringBuilder builder = new StringBuilder("btn btn-");

			if (IsOutline)
			{
				builder.Append("outline-");
			}

			builder.Append(color);
			builder.Append(size);


			return builder.ToString();
		}
	}

	public enum ButtonColor
	{
		Primary, Secondary, Success, Danger, Warning, Info, Light, Dark
	}

	public enum ButtonSize
	{
		Normal, Large, Small, Block
	}

}
