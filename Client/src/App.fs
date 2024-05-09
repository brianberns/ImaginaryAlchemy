namespace ImaginaryAlchemy

open Elmish
open Elmish.React

module App =

        // Elmish go!
    Program.mkProgram Model.init Model.update View.render
        |> Program.withReactSynchronous "elmish-app"
        |> Program.run
