namespace ImaginaryAlchemy

open Fable.Remoting.Client
open Elmish
open Elmish.React
open Feliz

type Model = Model

type Msg = Msg

module Model =

    let init () = Model, Cmd.none

    let update msg Model =
        match msg with
            | Msg -> Model, Cmd.none

module View =

    let render model dispatch =
        Html.div []

module App =

    let alchemyApi =
        Remoting.createApi()
            |> Remoting.buildProxy<IAlchemyApi>

    Program.mkProgram Model.init Model.update View.render
        |> Program.withReactSynchronous "elmish-app"
        |> Program.run
