namespace ImaginaryAlchemy

open System
open System.IO

open FSharp.Control

open LLama
open LLama.Abstractions
open LLama.Common

type Oracle =
    {
        Executor : ILLamaExecutor
        InferenceParams : IInferenceParams
        ConceptSet : Set<Concept>
    }

module Oracle =

    let private modelPath =
        @"C:\Users\brian\source\repos\ImaginaryAlchemy\Server\Meta-Llama-3-8B-Instruct.Q4_K_M.gguf"

    [<Literal>]
    let private promptTemplate =
        """The following is a list of imaginary alchemy experiments. Each experiment combines two concepts into a new concept. The new concept is always a singular noun.
> Fire + Water = Steam
> %s + %s = """

    let private antiPrompt = ">"

    let private normalize (concept : Concept) : Concept =
        concept[0..0].ToUpper() + concept[1..].ToLower()

    let create () =

        // https://huggingface.co/QuantFactory/Meta-Llama-3-8B-Instruct-GGUF/tree/main
        let executor =
            let modelParams = ModelParams(modelPath, GpuLayerCount = 100)
            let model = LLamaWeights.LoadFromFile(modelParams)
            StatelessExecutor(model, modelParams)
        let inferenceParams =
            InferenceParams(
                Temperature = 0.1f,
                AntiPrompts = [antiPrompt],
                MaxTokens = 10)
        // https://www.reddit.com/r/learnprogramming/comments/4yoap9/large_word_list_of_english_nouns/
        let conceptSet =
            File.ReadLines("nouns.txt")
                |> Seq.map normalize
                |> set
        {
            Executor = executor
            InferenceParams = inferenceParams
            ConceptSet = conceptSet
        }

    let private isValid oracle concept =
        concept = normalize concept
            && oracle.ConceptSet.Contains(concept)

    let private useColor color =
        Console.ForegroundColor <- color
        {
            new IDisposable with
                member _.Dispose() =
                    Console.ForegroundColor <- ConsoleColor.White
        }

    /// Infers the combination of two concepts.
    let private infer oracle (first : Concept) (second : Concept) =
        let prompt =
            (sprintf promptTemplate first second)
                .Replace("\r", "")
        let str =
            oracle.Executor.InferAsync(
                prompt,
                oracle.InferenceParams)
                |> AsyncSeq.ofAsyncEnum
                |> AsyncSeq.fold (+) ""
                |> Async.RunSynchronously   // make inference synchronous
        let str =
            let str = str.TrimEnd()
            if str.EndsWith(antiPrompt) then
                str.Substring(0, str.Length - antiPrompt.Length)
            else str
        normalize (str.Trim())

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
            && isValid oracle second
            && first <> second then

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
                        use _ = useColor ConsoleColor.Green
                        printfn $"Accepted: {first} + {second} = {concept}"
                        Some concept
                | _ ->
                    use _ = useColor ConsoleColor.Red
                    printfn $"Rejected: {first} + {second} = {concept}"
                    None
        else
            None
