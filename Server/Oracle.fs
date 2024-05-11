namespace ImaginaryAlchemy

open System
open System.IO

open Microsoft.Extensions.Configuration

open OpenAI
open OpenAI.Managers
open OpenAI.ObjectModels
open OpenAI.ObjectModels.RequestModels

/// Server-side settings.
type Settings =
    {
        /// OpenAI API key. Don't share this!
        ApiKey : string
    }

/// Inference oracle.
type Oracle private =
    {
        /// GPT service.
        Service : OpenAIService

        /// Set of all possible concepts.
        ConceptSet : Set<Concept>
    }

    interface IDisposable with
        member this.Dispose() =
            this.Service.Dispose()

module Oracle =

    /// GPT prompt template.
    [<Literal>]
    let private promptTemplate =
        """The following is a list of imaginary alchemy experiments. Each experiment combines two concepts into a new concept. The new concept is always a singular noun.
> Fire + Water = Steam
> %s + %s = """

    /// Normlizes the given concept. E.g. "water" -> "Water".
    let private normalize (concept : Concept) : Concept =
        concept[0..0].ToUpper() + concept[1..].ToLower()

    /// Creates an oracle using the list of possible concepts in the
    /// given directory.
    let create dir =

            // connect to GPT service
        let service =
            let settings =
                let path = Path.Combine(dir, "appsettings.json")
                ConfigurationBuilder()
                    .AddJsonFile(path)
                    .Build()
                    .Get<Settings>()
            new OpenAIService(
                OpenAiOptions(ApiKey = settings.ApiKey))

            // load set of all possible concepts
            // https://www.reddit.com/r/learnprogramming/comments/4yoap9/large_word_list_of_english_nouns
        let conceptSet =
            let path = Path.Combine(dir, "nouns.txt")
            File.ReadLines(path)
                |> Seq.map normalize
                |> set

        {
            Service = service
            ConceptSet = conceptSet
        }

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

    /// Tries to find the given concept in the set of possible
    /// concepts, converting it to singular if necessary.
    let private tryFind oracle (concept : Concept) =

        let test suffix () =
            if concept.EndsWith(suffix : string) then
                let concept' : Concept =
                    concept.Substring(
                        0,
                        concept.Length - suffix.Length)
                if oracle.ConceptSet.Contains(concept') then
                    Some concept'
                else None
            else None

        test "" ()
            |> Option.orElseWith (test "es")
            |> Option.orElseWith (test "s")

    /// Combines the given concepts, if possible.
    let combine oracle (first : Concept) (second : Concept) =
        option {

                // check for valid input
            if first < second
                && oracle.ConceptSet.Contains(first)
                && oracle.ConceptSet.Contains(second) then

                    // combine concepts
                let concept =
                    if first = "Fire" && second = "Water" then   // hard-coded example
                        "Steam"
                    else
                        infer oracle first second

                    // accept result?
                let! concept' = tryFind oracle concept
                if concept' <> first && concept' <> second then
                    return concept'
        }
