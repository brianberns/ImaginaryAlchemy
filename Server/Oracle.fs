namespace ImaginaryAlchemy

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

    let create () =

        // https://huggingface.co/QuantFactory/Meta-Llama-3-8B-Instruct-GGUF/tree/main
        let executor =
            let modelParams = ModelParams(modelPath, GpuLayerCount = 100)
            let model = LLamaWeights.LoadFromFile(modelParams)
            StatelessExecutor(model, modelParams)
        let inferenceParams =
            InferenceParams(
                Temperature = 0.0f,
                AntiPrompts = [antiPrompt],
                MaxTokens = 10)
        // https://www.reddit.com/r/learnprogramming/comments/4yoap9/large_word_list_of_english_nouns/
        let conceptSet =
            File.ReadLines("nouns.txt")
                |> Seq.map _.ToLower()
                |> set
        {
            Executor = executor
            InferenceParams = inferenceParams
            ConceptSet = conceptSet
        }

    let private normalize (name : string) =
        name[0..0].ToUpper() + name[1..].ToLower()

    let combine (conceptA : string) (conceptB : string) oracle =
        let conceptA = normalize conceptA
        let conceptB = normalize conceptB
        if conceptA = conceptB then
            async { return Some conceptA }
        else
            let conceptA, conceptB =
                min conceptA conceptB,
                max conceptA conceptB
            let prompt =
                (sprintf promptTemplate conceptA conceptB)
                    .Replace("\r", "")
            async {
                let! text =
                    oracle.Executor.InferAsync(
                        prompt,
                        oracle.InferenceParams)
                        |> AsyncSeq.ofAsyncEnum
                        |> AsyncSeq.fold (+) ""
                let concept =
                    let text = text.Trim()
                    if text.EndsWith(antiPrompt) then
                        text.Substring(0, text.Length - antiPrompt.Length)
                    else text
                let concept = normalize concept
                if oracle.ConceptSet.Contains(concept.ToLower())
                    && concept <> conceptA
                    && concept <> conceptB then
                    return Some concept
                else
                    return None
            }
