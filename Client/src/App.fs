namespace ImaginaryAlchemy

open Fable.Remoting.Client
open Elmish
open Elmish.React
open Feliz

type Model =
    {
        ConceptInfos : List<ConceptInfo>
        First : ConceptInfo
        Second : ConceptInfo
    }

type Msg = Msg

module Model =

    let init () =
        let model =
            let conceptInfos =
                [
                    "Earth", 0
                    "Fire", 0
                    "Water", 0
                    "Air", 0
                    "Steam", 1
                ] |> List.map (fun (concept, gen) ->
                    {
                        Concept = concept
                        Generation = gen
                    })
            {
                ConceptInfos = conceptInfos
                First = conceptInfos.Head
                Second = conceptInfos.Head
            }
        model, Cmd.none

    let update msg model =
        match msg with
            | Msg -> model, Cmd.none

module View =

    let renderConceptInfo info =
        Html.div [
            prop.className "concept-info"
            prop.children [
                Html.span [
                    prop.className "concept"
                    prop.text info.Concept
                ]
                Html.span [
                    prop.className "generation"
                    prop.innerHtml
                        (String.replicate info.Generation "&bull;")
                ]
            ]
        ]

    let render model dispatch =
        model.ConceptInfos
            |> Seq.map renderConceptInfo
            |> Html.div

module App =

    let alchemyApi =
        Remoting.createApi()
            |> Remoting.buildProxy<IAlchemyApi>

    Program.mkProgram Model.init Model.update View.render
        |> Program.withReactSynchronous "elmish-app"
        |> Program.run
