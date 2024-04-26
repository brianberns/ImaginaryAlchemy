namespace ImaginaryAlchemy

open System

type Term = string

type TermMap = Map<Term (*result*), Option<Term (*first*) * Term (*second*)>>

module Program =

    let rng = Random(0)

    let pickOne terms =
        terms
            |> Array.length
            |> rng.Next
            |> Array.get terms

    let pickTwo (asked : Set<_>) (terms : _[]) =

        let rec loop () =
            let first, second =
                let first = pickOne terms
                let second = pickOne terms
                min first second, max first second
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
            | Some term when not (termMap.ContainsKey(term)) ->
                let termMap' =
                    Map.add
                        term
                        (Some (first, second))
                        termMap
                dump term termMap'
                asked', termMap'
            | _ -> asked', termMap

    let rec loop asked termMap : unit =
        let asked', termMap' = increment asked termMap
        loop asked' termMap'

    try
        Console.OutputEncoding <- System.Text.Encoding.UTF8
        let asked =
            set [ "Fire", "Water" ]
        let termMap =
            Map [
                "Fire", None
                "Water", None
                "Stone", None
                "Wood", None
                "Steam", Some ("Fire", "Water")
            ]
        loop asked termMap
    with exn ->
        printfn $"{exn.Message}"
