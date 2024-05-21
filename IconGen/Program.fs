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
        "Rounded edges square 3D icon of %s, subtle gradient, on a white background"

    let service =
        let settings = Settings.get @"..\..\..\..\Server\"
        new OpenAIService(
            OpenAiOptions(ApiKey = settings.ApiKey))

    for concept in ["earth"; "air"; "fire"; "water"] do

        let prompt = sprintf promptTemplate concept
        let req =
            ImageCreateRequest(prompt,
                Model = Models.Dall_e_3,
                N = 1,
                Size = "1024x1024",
                ResponseFormat = "b64_json")

        let resp =
            service.Image
                .CreateImage(req)
                .Result

        if resp.Successful then
            let path = $"{prompt}.png"
            let bytes =
                let result = Seq.exactlyOne resp.Results
                Convert.FromBase64String(result.B64)
            File.WriteAllBytes(path, bytes)
        else
            printfn $"{resp.Error.Message}"
