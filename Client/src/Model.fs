namespace ImaginaryAlchemy

open Fable.Remoting.Client
open Elmish

module Alchemy =

    let api =
        Remoting.createApi()
            |> Remoting.buildProxy<IAlchemyApi>

type Model =
    {
        ConceptMap : Map<Concept, (*generation*) int>
        FirstOpt : Option<Concept>
        SecondOpt : Option<Concept>
    }

type Msg =
    | Select of Concept
    | Combine
    | Upsert of Concept * (*generation*) int * (*isNew*) bool
    | Fail

module Model =

    let init () =
        let model =
            let conceptMap =
                Map [
                    "Earth", 0
                    "Fire", 0
                    "Water", 0
                    "Air", 0
                    "Steam", 1
                ]
            {
                ConceptMap = conceptMap
                FirstOpt = None
                SecondOpt = None
            }
        model, Cmd.none

    let private select concept model =
        let model' =
            let firstOpt, secondOpt =
                if model.FirstOpt.IsNone then
                    assert(model.SecondOpt.IsNone)
                    Some concept, None
                elif model.SecondOpt.IsNone then
                    model.FirstOpt, Some concept
                else
                    model.SecondOpt, Some concept
            { model with
                FirstOpt = firstOpt
                SecondOpt = secondOpt }
        model', Cmd.none

    let private combine model =
        let cmd =
            option {
                let! first = model.FirstOpt
                let! second = model.SecondOpt
                let genFirst = model.ConceptMap[first]
                let genSecond = model.ConceptMap[second]
                let gen = (max genFirst genSecond) + 1
                return
                    Cmd.OfAsync.perform
                        (fun () ->
                            Alchemy.api.Combine(
                                first,
                                second))
                        ()
                        (function
                            | Some (concept, isNew) ->
                                Upsert (concept, gen, isNew)
                            | None -> Fail)
            } |> Option.defaultValue Cmd.none
        model, cmd

    let private upsert concept gen isNew model =
        let model' =
            { model with
                ConceptMap =
                    Map.add concept gen
                        model.ConceptMap
                FirstOpt = Some concept
                SecondOpt = None }
        model', Cmd.none

    let private fail model =
        model, Cmd.none

    let update msg model =
        match msg with
            | Select concept ->
                select concept model
            | Combine ->
                combine model
            | Upsert (concept, gen, isNew) ->
                upsert concept gen isNew model
            | Fail ->
                fail model
