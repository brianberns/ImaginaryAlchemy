namespace ImaginaryAlchemy

open Elmish
open Elmish.React

module App =

    Program.mkProgram Model.init Model.update View.render
        |> Program.withReactSynchronous "elmish-app"
        |> Program.run
