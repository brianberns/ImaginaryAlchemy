namespace ImaginaryAlchemy

open FSharp.Data

open RestSharp
open RestSharp.Authenticators

type NounProject = JsonProvider<"sample.json">

module Program =

    let settings = Settings.get "."
    let options =
        RestClientOptions(
            "https://api.thenounproject.com",
            Authenticator = OAuth1Auth.ForRequestToken(
                settings.ApiKey, settings.Secret))
    let client = new RestClient(options)

    for concept in [ "earth"; "air"; "fire"; "water" ] do

        let request =
            RestRequest("/v2/icon/")
                .AddQueryParameter("query", concept)
                .AddQueryParameter("thumbnail_size", "42")
                .AddQueryParameter("include_svg", "1")
        let response = client.ExecuteGet(request)
        let root = NounProject.Parse(response.Content)
        printfn $"{concept}: {root.Icons[0].ThumbnailUrl}"
