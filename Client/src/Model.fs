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
        First : Concept
        Second : Concept
    }

type Msg =
    | SetFirst of Concept
    | SetSecond of Concept
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
                First = "Earth"
                Second = "Water"
            }
        model, Cmd.none

    let private setFirst concept model =
        let model' = { model with First = concept }
        model', Cmd.none

    let private setSecond concept model =
        let model' = { model with Second = concept }
        model', Cmd.none

    let private combine model =
        let gen =
            let genFirst = model.ConceptMap[model.First]
            let genSecond = model.ConceptMap[model.Second]
            (max genFirst genSecond) + 1
        let cmd =
            Cmd.OfAsync.perform
                (fun () ->
                    Alchemy.api.Combine(
                        model.First,
                        model.Second))
                ()
                (function
                    | Some (concept, isNew) ->
                        Upsert (concept, gen, isNew)
                    | None -> Fail)
        model, cmd

    let private upsert concept gen isNew model =
        let model' =
            { model with
                ConceptMap =
                    Map.add concept gen
                        model.ConceptMap
                First = concept
                Second = concept }
        model', Cmd.none

    let private fail model =
        model, Cmd.none

    let update msg model =
        match msg with
            | SetFirst concept ->
                setFirst concept model
            | SetSecond concept ->
                setFirst concept model
            | Combine ->
                combine model
            | Upsert (concept, gen, isNew) ->
                upsert concept gen isNew model
            | Fail ->
                fail model
