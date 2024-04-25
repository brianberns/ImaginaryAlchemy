namespace LlamaSharpTest

open FSharp.Control

open LLama.Abstractions
open LLama.Common
open LLama

module Model =

    let private modelPath = @"C:\Users\brian\Downloads\Meta-Llama-3-8B.Q8_0.gguf"

    let private combineRaw (executor : ILLamaExecutor) =

        let antiPrompt = "User:"
        let inferenceParams =
            InferenceParams(
                Temperature = 0.0f,
                AntiPrompts = [antiPrompt])

        fun (conceptA : string) (conceptB : string) ->
            if conceptA = conceptB then
                Some conceptA
            else
                let (conceptA, conceptB) =
                    min conceptA conceptB,
                    max conceptA conceptB
                let text =
                    $"Transcript of a dialog, where the User interacts with an assistant named Bob. In each interaction, the user provides two concepts, and Bob combines them into new concept. Bob is creative and always responds with a single word, even when it seems like there is no word to represent the combined concept.\nUser: Fire, Water\nBob: Steam\nUser: {conceptA}, {conceptB}\nBob: "
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
                if concept.Contains(' ') then None
                else Some concept

    let combine =
        let parameters = ModelParams(modelPath, GpuLayerCount=100)
        let model = LLamaWeights.LoadFromFile(parameters)
        let executor = StatelessExecutor(model, parameters)
        combineRaw executor
