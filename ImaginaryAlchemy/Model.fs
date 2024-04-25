namespace LlamaSharpTest

open FSharp.Control

open LLama.Common
open LLama

module Model =

    let private modelPath = @"C:\Users\brian\Downloads\Meta-Llama-3-8B.Q8_0.gguf"
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
                $"The following is a list of imaginary alchemy experiments. Each experiment combines two concepts into a new concept creatively. The results is always exactly one word, even if it seems like there is no single word that represents the combined concept.\n> Fire + Water = Steam\n> {conceptA} + {conceptB} = "
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
