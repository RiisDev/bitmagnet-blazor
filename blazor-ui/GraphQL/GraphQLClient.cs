using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bitmagnet.WebUI.GraphQL;

public sealed class GraphQlClient
{
	private readonly HttpClient _httpClient;

	// The generated enums' wire values (e.g. "tv_show", "no_info" -- see [EnumMember] in
	// GraphQL.cs) are snake_case, not the PascalCase member names System.Text.Json expects
	// by default, and it doesn't honor [EnumMember] the way Newtonsoft does.
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNameCaseInsensitive = true,
		Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) }
	};

	public GraphQlClient(HttpClient httpClient)
	{
		_httpClient = httpClient;
	}

	public async Task<T> SendAsync<T>(
		string query,
		CancellationToken cancellationToken = default)
	{
		GraphQlRequest request = new()
		{
			Query = query
		};

		HttpResponseMessage response = await _httpClient.PostAsJsonAsync(
			"",
			request,
			cancellationToken);

		response.EnsureSuccessStatusCode();

		GraphQlResponse<T>? result =
			await response.Content.ReadFromJsonAsync<GraphQlResponse<T>>(
				JsonOptions,
				cancellationToken);

		if (result is null)
		{
			throw new InvalidOperationException(
				"GraphQL server returned an empty response.");
		}

		if (result.Errors is { Count: > 0 })
		{
			string errors = string.Join(
				Environment.NewLine,
				result.Errors.Select(x => x.Message));

			throw new InvalidOperationException(
				$"GraphQL request failed:{Environment.NewLine}{errors}");
		}

		return result.Data;
	}

	private sealed class GraphQlRequest
	{
		[JsonPropertyName("query")]
		public required string Query { get; init; }
	}
}