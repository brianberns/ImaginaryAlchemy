namespace ImaginaryAlchemy

open Fable.Remoting.Client
open Elmish
open Elmish.React
open Feliz

module Alchemy =

    let api =
        Remoting.createApi()
            |> Remoting.buildProxy<IAlchemyApi>

type Model =
    {
        ConceptMap : Map<Concept, int>
        First : Concept
        Second : Concept
    }

type Msg =
    | SetFirst of Concept
    | SetSecond of Concept
    | Combine
    | Upsert of ConceptInfo
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

    let update msg model =
        match msg with
            | SetFirst concept ->
                let model' = { model with First = concept }
                model', Cmd.none
            | SetSecond concept ->
                let model' = { model with Second = concept }
                model', Cmd.none
            | Combine ->
                let cmd =
                    Cmd.OfAsync.perform
                        (fun () ->
                            Alchemy.api.Combine(
                                model.First,
                                model.Second))
                        ()
                        (function
                            | Some info ->
                                Upsert info
                            | None -> Fail)
                model, cmd
            | Upsert info ->
                let model' =
                    { model with
                        ConceptMap =
                            Map.add
                                info.Concept
                                info.Generation
                                model.ConceptMap
                        First = info.Concept
                        Second = info.Concept }
                model', Cmd.none
            | Fail ->
                model, Cmd.none

module View =

    let renderConceptCard concept (conceptMap : Map<_, _>) dispatchOpt =
        Html.div [
            prop.className "concept-card"
            prop.children [
                Html.span [
                    prop.className "concept"
                    prop.text (concept : Concept)
                ]
                Html.span [
                    prop.className "generation"
                    prop.innerHtml
                        (String.replicate
                            conceptMap[concept]
                            "&bull;")
                ]
            ]
            match dispatchOpt with
                | Some dispatch ->
                    prop.onClick (fun _ ->
                        dispatch concept)
                | None -> ()
        ]

    let renderSelected model dispatch =
        Html.div [
            renderConceptCard model.First model.ConceptMap None
            renderConceptCard model.Second model.ConceptMap None
            Html.button [
                prop.text "Combine"
                prop.onClick (fun _ ->
                    Combine |> dispatch)
            ]
        ]

    let renderConceptCards model dispatch =
        Html.div [
            prop.className "concept-cards"
            model.ConceptMap.Keys
                |> Seq.map (fun concept ->
                    renderConceptCard concept model.ConceptMap dispatch)
                |> prop.children
        ]

    let render model dispatch =
        Html.div [
            renderSelected model dispatch
            renderConceptCards
                model
                (Some (SetFirst >> dispatch))
            renderConceptCards
                model
                (Some (SetSecond >> dispatch))
        ]        

module App =

    Program.mkProgram Model.init Model.update View.render
        |> Program.withReactSynchronous "elmish-app"
        |> Program.run
