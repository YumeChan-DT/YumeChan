using System.ComponentModel.DataAnnotations;

namespace Nodsoft.YumeChan.NetRunner.Models
{
	public class StatusModel
	{
		[Required(AllowEmptyStrings = true)]
		public string StatusMessage { get; set; }
	}
}
