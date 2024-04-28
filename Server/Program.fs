namespace ImaginaryAlchemy

open System.IO

open Suave
open Suave.Logging
open Suave.Operators

open FSharp.Control

open Fable.Remoting.Server
open Fable.Remoting.Suave

open LLama
open LLama.Common

module Model =

    // https://huggingface.co/QuantFactory/Meta-Llama-3-8B-Instruct-GGUF/tree/main
    let private modelPath = @"C:\Users\brian\source\repos\ImaginaryAlchemy\ImaginaryAlchemy\Meta-Llama-3-8B-Instruct.Q4_K_M.gguf"
    let private parameters = ModelParams(modelPath, GpuLayerCount = 100)
    let private model = LLamaWeights.LoadFromFile(parameters)
    let private executor = StatelessExecutor(model, parameters)
    let private antiPrompt = ">"
    let private inferenceParams =
        InferenceParams(
            Temperature = 0.0f,
            AntiPrompts = [antiPrompt],
            MaxTokens = 10)

    // https://www.reddit.com/r/learnprogramming/comments/4yoap9/large_word_list_of_english_nouns/
    let private allConcepts =
        File.ReadLines("nouns.txt")
            |> Seq.map _.ToLower()
            |> set

    let combine (conceptA : string) (conceptB : string) =
        if conceptA = conceptB then
            async { return Some conceptA }
        else
            let (conceptA, conceptB) =
                min conceptA conceptB,
                max conceptA conceptB
            let text =
                $"""The following is a list of imaginary alchemy experiments. Each experiment combines two concepts into a new concept. The new concept is always a singular noun.
> Fire + Water = Steam
> {conceptA} + {conceptB} = """.Replace("\r", "")
            async {
                let! concept =
                    executor.InferAsync(text, inferenceParams)
                        |> AsyncSeq.ofAsyncEnum
                        |> AsyncSeq.fold (+) ""
                let concept = concept.TrimEnd()
                let concept =
                    if concept.EndsWith(antiPrompt) then
                        concept.Substring(0, concept.Length - antiPrompt.Length)
                    else concept
                let concept = concept.Trim().ToLower()
                let concept = concept[0..0].ToUpper() + concept[1..]
                if allConcepts.Contains(concept.ToLower())
                    && concept <> conceptA
                    && concept <> conceptB then
                    return Some concept
                else
                    return None
            }

module Program =

    try

        let alchemyApi : IAlchemyApi =
            {
                Combine = uncurry Model.combine
            }

            // create the web service
        let service : WebPart =
            let logger = Targets.create LogLevel.Info [||]
            (Remoting.createApi()
                |> Remoting.fromValue alchemyApi
                |> Remoting.buildWebPart)
                >=> Filters.logWithLevelStructured
                    LogLevel.Info
                    logger
                    Filters.logFormatStructured

            // start the web server
        let config =
            { defaultConfig with
                bindings = [ HttpBinding.createSimple HTTP "127.0.0.1" 5000 ] }
        startWebServer config service

    with exn -> printfn $"{exn.Message}"
