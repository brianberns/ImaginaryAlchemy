namespace ImaginaryAlchemy

open Fable.Remoting.Client
open Elmish
open Elmish.React
open Feliz

type Model = List<ConceptInfo>

type Msg = Msg

module Model =

    let init () =
        let model =
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
        model, Cmd.none

    let update msg model =
        match msg with
            | Msg -> model, Cmd.none

module View =

    let renderConceptInfo info =
        Html.div [
            Html.span info.Concept
            Html.span info.Generation
        ]

    let render model dispatch =
        model
            |> Seq.map renderConceptInfo
            |> Html.div

module App =

    let alchemyApi =
        Remoting.createApi()
            |> Remoting.buildProxy<IAlchemyApi>

    Program.mkProgram Model.init Model.update View.render
        |> Program.withReactSynchronous "elmish-app"
        |> Program.run
