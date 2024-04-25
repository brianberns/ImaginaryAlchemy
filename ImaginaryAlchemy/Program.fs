namespace LlamaSharpTest

open System

type Term = string

type Response =
    {
        Result : Term
        IsNew : bool
    }

type TermMap = Map<Term (*result*), Option<Term (*first*) * Term (*second*)>>

module Program =

    let getMessages exn =

        let rec loop (exn : exn) =
            seq {
                match exn with
                    | :? AggregateException as aggExn ->
                        for innerExn in aggExn.InnerExceptions do
                            yield! loop innerExn
                    | _ ->
                        if not (isNull exn.InnerException) then
                            yield! loop exn.InnerException
                yield exn.Message
            }

        loop exn
            |> Seq.toArray

    let pickOne =
        let rng = Random(0)
        fun (terms : _[]) ->
            let idx = rng.Next(terms.Length)
            terms[idx]

    let pickTwo (asked : Set<_>) (terms : _[]) =

        let rec loop () =
            let first, second =
                let first = pickOne terms
                let second = pickOne terms
                if first <= second then first, second
                else second, first
            if first = second || asked.Contains(first, second) then
                loop ()
            else
                first, second

        if asked.Count < (terms.Length * (terms.Length - 1)) / 2 then
            loop ()
        else
            failwith "No remaining combinations"

    let dump term (termMap : TermMap) =

        let rec loop indent seen term =
            if Set.contains term seen then seen
            else
                match termMap[term] with
                    | Some (first, second) ->
                        printfn $"{String(' ', 3 * indent)}{term} = {first} + {second}"
                        let seen = Set.add term seen
                        let indent = indent + 1
                        let seen = loop indent seen first
                        let seen = loop indent seen second
                        seen
                    | None -> seen

        Console.ForegroundColor <- ConsoleColor.Green
        printfn ""
        loop 0 Set.empty term
            |> ignore
        Console.ForegroundColor <- ConsoleColor.White

    let increment (asked : Set<Term * Term>) (termMap : TermMap) =
        let first, second =
            let terms = Seq.toArray termMap.Keys
            pickTwo asked terms
        let asked' = asked.Add(first, second)
        match Model.combine first second with
            | Some term ->
                let termMap' =
                    Map.add
                        term
                        (Some (first, second))
                        termMap
                dump term termMap'
                asked', termMap'
            | None -> asked', termMap

    let rec loop asked termMap : unit =
        let asked', termMap' = increment asked termMap
        loop asked' termMap'

    try
        Console.OutputEncoding <- System.Text.Encoding.UTF8
        [ "Water"; "Fire"; "Wind"; "Earth" ]
            |> Seq.map (fun term -> term, None)
            |> Map
            |> loop Set.empty
    with exn ->
        printfn $"{exn.Message}"
