using System;
using System.Text.Json.Nodes;

namespace TouhouButtonWPF
{
	public static class JsonUtils
	{
		public static void TryGetNode(JsonNode json, string field, Action<JsonNode> callback)
		{
			var value = json[field];
			if (value != null) callback(value);
		}

		public static void TryGetValue<T>(JsonNode json, string field, Action<T> callback) => TryGetNode(json, field, node => callback(node.GetValue<T>()));
		public static void TryGetUri(JsonNode json, string field, Action<Uri> callback) => TryGetValue<string>(json, field, value =>
		{
			Uri uri = new(value.StartsWith("./") ? AppDomain.CurrentDomain.BaseDirectory + value : value, UriKind.Absolute);
			callback(uri);
		});
	}
}