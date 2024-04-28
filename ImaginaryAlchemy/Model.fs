namespace ImaginaryAlchemy

open System

open FSharp.Control

open LLama.Common
open LLama

// https://www.reddit.com/r/learnprogramming/comments/4yoap9/large_word_list_of_english_nouns/
module Concept =

    let all =
        System.IO.File.ReadLines("nouns.txt")
            |> Seq.map _.ToLower()
            |> set

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

    let wtr =
        let wtr = new System.IO.StreamWriter("output.txt")
        wtr.AutoFlush <- true
        wtr

    let combine (conceptA : string) (conceptB : string) =
        if conceptA = conceptB then
            Some conceptA
        else
            let (conceptA, conceptB) =
                min conceptA conceptB,
                max conceptA conceptB
            let text =
                $"""The following is a list of imaginary alchemy experiments. Each experiment combines two concepts into a new concept. The new concept is always a singular noun.
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
            let concept = concept.Trim().ToLower()
            let concept = concept[0..0].ToUpper() + concept[1..]
            if Concept.all.Contains(concept.ToLower())
                && concept <> conceptA
                && concept <> conceptB then
                Some concept
            else
                fprintfn wtr $"*** Rejected {concept} = {conceptA} + {conceptB}"
                None
