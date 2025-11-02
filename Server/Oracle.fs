namespace ImaginaryAlchemy

open System
open System.IO

open Betalgo.Ranul.OpenAI
open Betalgo.Ranul.OpenAI.Managers
open Betalgo.Ranul.OpenAI.ObjectModels
open Betalgo.Ranul.OpenAI.ObjectModels.RequestModels

/// Inference oracle.
type Oracle =
    {
        /// Is it valid to attempt to combine the given concepts?
        IsValid : Concept -> Concept -> bool

        /// Combines the given valid concepts, if possible.
        Combine : Concept -> Concept -> Option<Concept>

        /// Disposable resource.
        Resource : IDisposable
    }

    interface IDisposable with
        member this.Dispose() =
            this.Resource.Dispose()

module Oracle =

    /// GPT prompt template.
    [<Literal>]
    let private promptTemplate =
        """The following is a list of imaginary alchemy experiments. Each experiment combines two concepts into a new concept whimsically. The new concept is always a singular noun or two-word noun phrase.
> Fire + Water = Steam
> %s + %s = """

    /// Normlizes the given concept. E.g. "water" -> "Water".
    let private normalize (concept : Concept) : Concept =
        concept[0..0].ToUpper() + concept[1..].ToLower()

    /// Infers the combination of two concepts.
    let private infer
        (service : OpenAIService)
        (first : Concept)
        (second : Concept) =
        let req =
            let prompt =
                (sprintf promptTemplate first second)
                    .Replace("\r", "")
            ChatCompletionCreateRequest(
                Messages =
                    ResizeArray [
                        ChatMessage.FromUser(prompt)
                    ],
                Model = "gpt-4o-mini",
                Seed = 0)
        let resp =
            service
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
    let private tryFind
        (conceptSet : Set<Concept>)
        (concept : Concept) =

        let test suffix () =
            if concept.EndsWith(suffix : string) then
                let concept' : Concept =
                    concept.Substring(
                        0,
                        concept.Length - suffix.Length)
                if conceptSet.Contains(concept') then
                    Some concept'
                else None
            else None

        test "" ()
            |> Option.orElseWith (test "es")
            |> Option.orElseWith (test "s")

    /// Indicates whether the given parent concepts might be
    /// validly combined.
    let private isValid
        (conceptSet : Set<Concept>)
        (first : Concept)
        (second : Concept) =
        first < second
            && conceptSet.Contains(first)
            && conceptSet.Contains(second)

    /// Combines the given concepts, if possible.
    let private combine service conceptSet first second =
        option {
            if isValid conceptSet first second then

                    // combine concepts
                let concept =
                    if first = "Fire" && second = "Water" then   // hard-coded example
                        "Steam"
                    else
                        infer service first second

                    // accept result?
                let! concept' = tryFind conceptSet concept
                if concept' <> first && concept' <> second then
                    return concept'
        }

    /// Creates an oracle using the list of possible concepts in the
    /// given directory.
    let create dir =

            // connect to GPT service
        let service =
            let settings = Settings.get dir
            new OpenAIService(
                OpenAIOptions(ApiKey = settings.ApiKey))

            // load set of all possible concepts
            // https://www.reddit.com/r/learnprogramming/comments/4yoap9/large_word_list_of_english_nouns
        let conceptSet =
            let path = Path.Combine(dir, "nouns.txt")
            File.ReadLines(path)
                |> Seq.map normalize
                |> set

        {
            IsValid = isValid conceptSet
            Combine = combine service conceptSet
            Resource = service
        }
