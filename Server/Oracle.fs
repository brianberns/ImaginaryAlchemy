namespace ImaginaryAlchemy

open System
open System.IO

open Microsoft.Extensions.Configuration

open OpenAI.GPT3
open OpenAI.GPT3.Managers
open OpenAI.GPT3.ObjectModels
open OpenAI.GPT3.ObjectModels.RequestModels

type Settings =
    {
        ApiKey : string
    }

type Oracle =
    {
        Service : OpenAIService
        ConceptSet : Set<Concept>
    }

module Oracle =

    [<Literal>]
    let private promptTemplate =
        """The following is a list of imaginary alchemy experiments. Each experiment combines two concepts into a new concept. The new concept is always a singular noun.
> Fire + Water = Steam
> %s + %s = """

    let private normalize (concept : Concept) : Concept =
        concept[0..0].ToUpper() + concept[1..].ToLower()

    let create dir =

        let settings =
            let path = Path.Combine(dir, "appsettings.json")
            ConfigurationBuilder()
                .AddJsonFile(path)
                .Build()
                .Get<Settings>()

        let service =
            OpenAiOptions(ApiKey = settings.ApiKey)
                |> OpenAIService

        // https://www.reddit.com/r/learnprogramming/comments/4yoap9/large_word_list_of_english_nouns/
        let conceptSet =
            let path = Path.Combine(dir, "nouns.txt")
            File.ReadLines(path)
                |> Seq.map normalize
                |> set

        {
            Service = service
            ConceptSet = conceptSet
        }

    let private isValid oracle concept =
        concept = normalize concept
            && oracle.ConceptSet.Contains(concept)

    /// Infers the combination of two concepts.
    let private infer oracle (first : Concept) (second : Concept) =
        let req =
            let prompt =
                (sprintf promptTemplate first second)
                    .Replace("\r", "")
            ChatCompletionCreateRequest(
                Messages =
                    ResizeArray [
                        ChatMessage.FromUser(prompt)
                    ],
                Model = Models.Gpt_4,
                Temperature = 0.0f)
        let resp =
            oracle.Service
                .ChatCompletion
                .CreateCompletion(req)
                .Result
        if resp.Successful then
            resp.Choices[0]
                .Message
                .Content
                .Trim()
                |> normalize
        else
            failwith resp.Error.Message

    let private trySingular oracle (concept : Concept) =

        let test suffix =
            if concept.EndsWith(suffix : string) then
                let concept' =
                    concept.Substring(
                        0,
                        concept.Length - suffix.Length)
                if oracle.ConceptSet.Contains(concept') then
                    Some concept'
                else None
            else None

        test ""
            |> Option.orElseWith (fun () ->
                test "es")
            |> Option.orElseWith (fun () ->
                test "s")

    /// Combines the given concepts.
    let combine oracle (first : Concept) (second : Concept) =

            // check for valid input
        if isValid oracle first
            && isValid oracle second then

            if first = second then
                Error first
            else

                    // normalize concept order
                let first, second =
                    min first second,
                    max first second

                    // combine concepts
                let concept =
                    if first = "Fire" && second = "Water" then   // hard-coded example
                        "Steam"
                    else
                        infer oracle first second

                    // accept result?
                match trySingular oracle concept with
                    | Some concept when
                        concept <> first
                            && concept <> second ->
                            Ok concept
                    | _ ->
                        Error concept

        else
            Error "Invalid"
