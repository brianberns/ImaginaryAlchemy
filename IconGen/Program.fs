namespace ImaginaryAlchemy

open System
open System.IO

open OpenAI
open OpenAI.Managers
open OpenAI.ObjectModels
open OpenAI.ObjectModels.RequestModels

module Program =

    [<Literal>]
    let private promptTemplate =
        "An icon of %s in light blue metallic iridescent material, 3D render isometric perspective on dark background"

    let service =
        let settings = Settings.get @"..\..\..\..\Server\"
        new OpenAIService(
            OpenAiOptions(ApiKey = settings.ApiKey))

    let req =
        let prompt = sprintf promptTemplate "water"
        ImageCreateRequest(prompt,
            Model = Models.Dall_e_2,
            N = 1,
            Size = "256x256",
            ResponseFormat = "b64_json")

    let resp =
        service.Image
            .CreateImage(req)
            .Result

    if resp.Successful then
        let result = Seq.exactlyOne resp.Results
        let bytes = Convert.FromBase64String(result.B64)
        let path = $"water {DateTime.Now.Ticks}.png"
        File.WriteAllBytes(path, bytes)
    else
        printfn $"{resp.Error.Message}"
