namespace LlamaSharpTest

open System

open FSharp.Control

open LLama.Common
open LLama

module Model =

    let private modelPath = @"C:\Users\brian\source\repos\ImaginaryAlchemy\ImaginaryAlchemy\llama-2-7b-chat.Q3_K_S.gguf"
    let private parameters = ModelParams(modelPath, GpuLayerCount = 100)
    let private model = LLamaWeights.LoadFromFile(parameters)
    let private executor = StatelessExecutor(model, parameters)
    let private antiPrompt = ">"
    let private inferenceParams =
        InferenceParams(
            Temperature = 0.0f,
            AntiPrompts = [antiPrompt],
            MaxTokens = 10)

    let combine (conceptA : string) (conceptB : string) =
        if conceptA = conceptB then
            Some conceptA
        else
            let (conceptA, conceptB) =
                min conceptA conceptB,
                max conceptA conceptB
            let text =
                $"""The following is a list of imaginary alchemy results. Each one combines two concepts into a new concept. The result is always one word. The result is always a singular noun.
> Fire + Water = Steam
> {conceptA} + {conceptB} = """.Replace("\r", "")
            let concept =
                executor.InferAsync(text, inferenceParams)
                    |> AsyncSeq.ofAsyncEnum
                    |> AsyncSeq.fold (+) ""
                    |> Async.RunSynchronously
            let concept = concept.TrimEnd()
            let concept =
                if concept.EndsWith(antiPrompt) then
                    concept.Substring(0, concept.Length - antiPrompt.Length)
                else concept
            let concept = concept.Trim()
            let concept = concept[0..0].ToUpper() + concept[1..].ToLower()
            if Seq.forall Char.IsLetter concept then
                Some concept
            else
                Console.ForegroundColor <- ConsoleColor.Red
                printfn ""
                printfn $"*** Rejected {concept} = {conceptA} + {conceptB}"
                printfn ""
                Console.ForegroundColor <- ConsoleColor.White
                None
