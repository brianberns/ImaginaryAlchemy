namespace ImaginaryAlchemy

open System
open System.IO

type ConceptMap = Map<Concept, Option<Concept * Concept>>

type Inventory =
    {
        OldConceptMap : ConceptMap
        NewConceptMap : ConceptMap
    }

module Inventory =

    let private keySet map =
        map |> Map.keys |> set

    let create oldConceptMap newConceptMap =
        assert(
            Set.intersect
                (keySet oldConceptMap)
                (keySet newConceptMap) = Set.empty)
        assert(
            [
                yield! Map.values oldConceptMap |> Seq.choose id
                yield! Map.values newConceptMap |> Seq.choose id
            ]
                |> List.unzip
                |> uncurry List.append
                |> Seq.forall (fun concept ->
                    oldConceptMap.ContainsKey(concept)))
        {
            OldConceptMap = oldConceptMap
            NewConceptMap = newConceptMap
        }

    let iterate oracle inv =

        let allConcepts =
            keySet inv.OldConceptMap
                + keySet inv.NewConceptMap

        let pairs =
            let newConcepts = Seq.toArray inv.NewConceptMap.Keys
            seq {
                for i = 0 to newConcepts.Length - 1 do
                    let newConcept = newConcepts[i]
                    for concept in inv.OldConceptMap.Keys do
                        yield newConcept, concept
                    for j = i+1 to newConcepts.Length - 1 do
                        yield newConcept, newConcepts[j]
            }
                |> Seq.sortBy (fun _ -> Guid.NewGuid())
                |> Seq.truncate 200

        let oldConceptMap =
            Map [
                yield! Map.toSeq inv.OldConceptMap
                yield! Map.toSeq inv.NewConceptMap
            ]

        let newConceptMap =
            (Map.empty, pairs)
                ||> Seq.fold (fun acc (first, second) ->
                    match Oracle.combine oracle first second with
                        | Some concept when allConcepts.Contains(concept) |> not ->
                            acc.Add(concept, Some (first, second))
                        | _ -> acc)

        create oldConceptMap newConceptMap

    let dump inv =

        let entries =
            inv.NewConceptMap
                |> Map.toSeq
                |> Seq.choose (fun (concept, pairOpt) ->
                    pairOpt
                        |> Option.map (fun pair ->
                            concept, pair))

        for concept, (first, second) in entries do
            printfn $"{concept} = {first} + {second}"

    let rec explore oracle inv =
        let inv' = iterate oracle inv
        if not inv'.NewConceptMap.IsEmpty then
            printfn ""
            dump inv'
            explore oracle inv'

module Program =

    let oracle = Oracle.create "."

    let inv =
        let oldConceptMap =
            Map [
                "Fire", None
                "Water", None
            ]
        let newConceptMap =
            Map [
                "Earth", None
                "Air", None
                "Steam", Some ("Fire", "Water")
            ]
        Inventory.create oldConceptMap newConceptMap

    try
        Inventory.explore oracle inv
    with exn ->
        printfn $"{exn.Message}"
