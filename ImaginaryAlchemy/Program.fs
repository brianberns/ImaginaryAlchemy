namespace ImaginaryAlchemy

open System

type Term = string

type TermMap = Map<Term, Option<Term * Term>>

type Inventory =
    {
        OldTermMap : TermMap
        NewTermMap : TermMap
    }

module Inventory =

    let private uncurry f (a, b) = f a b

    let private keySet map =
        map |> Map.keys |> set

    let create oldTermMap newTermMap =
        assert(
            Set.intersect
                (keySet oldTermMap)
                (keySet newTermMap) = Set.empty)
        assert(
            [
                yield! Map.values oldTermMap |> Seq.choose id
                yield! Map.values newTermMap |> Seq.choose id
            ]
                |> List.unzip
                |> uncurry List.append
                |> Seq.forall (fun term ->
                    oldTermMap.ContainsKey(term)))
        {
            OldTermMap = oldTermMap
            NewTermMap = newTermMap
        }

    let iterate inv =
        let allTerms =
            keySet inv.OldTermMap
                + keySet inv.NewTermMap
        let pairs =
            seq {
                for newTerm in inv.NewTermMap.Keys do
                    for term in allTerms do
                        yield newTerm, term
            }
        let oldTermMap =
            Map [
                yield! Map.toSeq inv.OldTermMap
                yield! Map.toSeq inv.NewTermMap
            ]
        let newTermMap =
            (Map.empty, pairs)
                ||> Seq.fold (fun acc (first, second) ->
                    match Model.combine first second with
                        | Some term when
                            allTerms
                                |> Set.contains term
                                |> not ->
                            acc.Add(term, Some (first, second))
                        | _ -> acc)
        create oldTermMap newTermMap

    let dump inv =

        let entries =
            inv.NewTermMap
                |> Map.toSeq
                |> Seq.choose (fun (term, pairOpt) ->
                    pairOpt
                        |> Option.map (fun pair ->
                            term, pair))

        Console.ForegroundColor <- ConsoleColor.Green
        for term, (first, second) in entries do
            printfn $"{term} = {first} + {second}"
        Console.ForegroundColor <- ConsoleColor.White

    let rec explore inv =
        let inv' = iterate inv
        if not inv'.NewTermMap.IsEmpty then
            printfn ""
            dump inv'
            explore inv'

module Program =

    let inv =
        let oldTermMap =
            Map [
                "Fire", None
                "Water", None
            ]
        let newTermMap =
            Map [
                "Earth", None
                "Wind", None
                "Steam", Some ("Fire", "Water")
            ]
        Inventory.create oldTermMap newTermMap

    Console.OutputEncoding <- System.Text.Encoding.UTF8
    try
        Inventory.explore inv
    with exn ->
        printfn $"{exn.Message}"
