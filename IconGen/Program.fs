namespace ImaginaryAlchemy

open RestSharp
open RestSharp.Authenticators

module Program =

    let settings = Settings.get "."
    let options =
        RestClientOptions(
            "https://api.thenounproject.com",
            Authenticator = OAuth1Auth.ForRequestToken(
                settings.ApiKey, settings.Secret))
    let client = new RestClient(options)
    let request =
        RestRequest("/v2/icon/")
            .AddQueryParameter("query", "water")
            .AddQueryParameter("thumbnail_size", "42")
            .AddQueryParameter("include_svg", "1")
    let response = client.ExecuteGet(request)
    printfn "%A" response.Content
