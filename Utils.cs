using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TouhouButtonWPF
{
	internal class Utils
	{
		public static long UnixTimestampNow() => DateTimeOffset.Now.ToUnixTimeSeconds();
		
		public static Uri GetUri(string value)
		{
			Uri uri = new(value.StartsWith("./") ? AppDomain.CurrentDomain.BaseDirectory + value : value, UriKind.Absolute);
			return uri;
		}
	}
}
