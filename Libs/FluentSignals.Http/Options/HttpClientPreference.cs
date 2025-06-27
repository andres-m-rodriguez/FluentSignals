namespace FluentSignals.Http.Options;

public class HttpClientPreference
{
    public enum Preference
    {
        HttpClient,
        HttpFactory,
    }

    public Preference Value { get; set; }
    public bool IsHttpClient => this.Value == Preference.HttpClient;
    public bool IsHttpFactory => this.Value == Preference.HttpFactory;

    public bool ThrowExceptionIfPreferedClientNotFound = false;
    public static readonly HttpClientPreference Default = new() { Value = Preference.HttpFactory };
}
