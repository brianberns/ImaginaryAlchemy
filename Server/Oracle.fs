namespace ImaginaryAlchemy

open System.IO

open FSharp.Control

open LLama
open LLama.Common

module Oracle =

    // https://huggingface.co/QuantFactory/Meta-Llama-3-8B-Instruct-GGUF/tree/main
    let private modelPath = @"C:\Users\brian\source\repos\ImaginaryAlchemy\Server\Meta-Llama-3-8B-Instruct.Q4_K_M.gguf"
    let private parameters = ModelParams(modelPath, GpuLayerCount = 100)
    let private model = LLamaWeights.LoadFromFile(parameters)
    let private executor = StatelessExecutor(model, parameters)
    let private antiPrompt = ">"
    let private inferenceParams =
        InferenceParams(
            Temperature = 0.0f,
            AntiPrompts = [antiPrompt],
            MaxTokens = 10)

    [<Literal>]
    let private promptTemplate =
        """The following is a list of imaginary alchemy experiments. Each experiment combines two concepts into a new concept. The new concept is always a singular noun.
> Fire + Water = Steam
> %s + %s = """

    // https://www.reddit.com/r/learnprogramming/comments/4yoap9/large_word_list_of_english_nouns/
    let private allConcepts =
        File.ReadLines("nouns.txt")
            |> Seq.map _.ToLower()
            |> set

    let private normalize (name : string) =
        name[0..0].ToUpper() + name[1..].ToLower()

    let combine (conceptA : string) (conceptB : string) =
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
                    executor.InferAsync(prompt, inferenceParams)
                        |> AsyncSeq.ofAsyncEnum
                        |> AsyncSeq.fold (+) ""
                let concept =
                    let text = text.Trim()
                    if text.EndsWith(antiPrompt) then
                        text.Substring(0, text.Length - antiPrompt.Length)
                    else text
                let concept = normalize concept
                if allConcepts.Contains(concept.ToLower())
                    && concept <> conceptA
                    && concept <> conceptB then
                    return Some concept
                else
                    return None
            }
